using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ---------- data returned by the server ----------

[Serializable]
public class PlayerInfo
{
    public string player_id;
    public string display_name;
    public bool ready;
    public bool finished;
    public int duration_ms;
    public int scanned_count;
}

[Serializable]
public class SessionInfo
{
    public string error;
    public string session_id;
    public string state;
    public long server_now_ms;
    public long start_at_ms;
    public int min_players;
    public int required_items;
    public bool member;
    public PlayerInfo[] players;
}

[Serializable]
public class JoinResult
{
    public string session_id;
    public string state;
}

[Serializable]
public class FinishResult
{
    public bool ok;
    public string error;
    public int duration_ms;
    public int rank;
    public bool new_best;
}

[Serializable]
public class OkResult
{
    public bool ok;
    public string error;
}

[Serializable]
public class LeaderboardRow
{
    public string player_id;
    public string display_name;
    public int duration_ms;
}

[Serializable]
public class LeaderboardInfo
{
    public LeaderboardRow[] top;
    public int personal_best_ms;
    public int personal_rank;
}

public enum SessionState { None, Lobby, Countdown, Racing, Ended }

/// <summary>Shared switches that tell the rest of the game which mode a run is in.</summary>
public static class MultiplayerGate
{
    /// <summary>Player chose to skip the online race.</summary>
    public static bool SoloChosen;

    /// <summary>Player is in an online session.</summary>
    public static bool Online;

    /// <summary>False in the lobby and countdown, so early scans do not count.</summary>
    public static bool ScanningAllowed = true;

    public static bool LobbyRequired
    {
        get { return YuccaBackendConfig.IsConfigured && !SoloChosen; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        Reset();
    }

    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Reset();
    }

    public static void Reset()
    {
        SoloChosen = false;
        Online = false;
        ScanningAllowed = true;
    }
}

/// <summary>
/// Joins a race session, keeps the lobby in sync by asking the server every second or two,
/// keeps a shared clock, reports scans and the finish.
/// </summary>
public class SessionController : MonoBehaviour
{
    public static SessionController Instance { get; private set; }

    public static SessionController EnsureExists()
    {
        if (Instance != null) return Instance;
        return new GameObject("SessionController").AddComponent<SessionController>();
    }

    public SessionState State { get; private set; }
    public SessionInfo Info { get; private set; }
    public string SessionId { get; private set; }
    public string LastError { get; private set; }
    public FinishResult LastResult { get; private set; }

    public bool Joined { get { return !string.IsNullOrEmpty(SessionId); } }
    public bool HasResult { get { return LastResult != null && LastResult.ok; } }

    public event Action OnChanged;
    public event Action<string> OnError;
    public event Action OnResult;

    long offsetMs;
    long bestRtt = long.MaxValue;
    bool haveOffset;
    bool polling;
    bool inFlight;
    string myName = "Player";

    QuestManager qm;
    readonly Queue<string> scanQueue = new Queue<string>();
    readonly HashSet<string> reported = new HashSet<string>();
    bool pumping;
    bool finishPending;

    /// <summary>Server time in milliseconds since 1970, estimated from this phone's clock.</summary>
    public long ServerNowMs { get { return LocalNowMs() + offsetMs; } }

    public float SecondsUntilStart
    {
        get { return Info == null ? 0f : (Info.start_at_ms - ServerNowMs) / 1000f; }
    }

    static long LocalNowMs() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        qm = QuestManager.Instance;
        if (qm != null)
        {
            qm.OnItemScanned += HandleScan;
            qm.OnQuestCompleted += HandleComplete;
        }
    }

    void OnDestroy()
    {
        if (qm != null)
        {
            qm.OnItemScanned -= HandleScan;
            qm.OnQuestCompleted -= HandleComplete;
        }
        if (Instance == this)
        {
            Instance = null;
            // Leaving the scene while still in the lobby: free the seat.
            if (Joined && (State == SessionState.Lobby || State == SessionState.Countdown))
                SendLeave(SessionId);
        }
    }

    // ---------- joining and the lobby ----------

    public void Join(string name)
    {
        myName = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim();
        LastError = null;

        string body = "{\"p_player_id\":\"" + NetworkClient.Esc(PlayerProfile.Id) +
                      "\",\"p_name\":\"" + NetworkClient.Esc(myName) + "\"}";

        NetworkClient.Instance.Rpc("join_session", body,
            json =>
            {
                JoinResult r = null;
                try { r = JsonUtility.FromJson<JoinResult>(json); } catch { }
                if (r == null || string.IsNullOrEmpty(r.session_id)) { Fail("Could not join the race."); return; }

                SessionId = r.session_id;
                MultiplayerGate.Online = true;
                StartPolling();
                PollNow();
            },
            err => Fail(err));
    }

    public void SetReady(bool ready)
    {
        if (!Joined) return;
        string body = "{\"p_session_id\":\"" + SessionId + "\",\"p_player_id\":\"" +
                      NetworkClient.Esc(PlayerProfile.Id) + "\",\"p_ready\":" + (ready ? "true" : "false") + "}";
        NetworkClient.Instance.Rpc("set_ready", body, json => PollNow(), err => Fail(err));
    }

    public void Leave()
    {
        if (Joined) SendLeave(SessionId);
        SessionId = null;
        Info = null;
        State = SessionState.None;
        polling = false;
        MultiplayerGate.Online = false;
    }

    static void SendLeave(string sessionId)
    {
        string body = "{\"p_session_id\":\"" + sessionId + "\",\"p_player_id\":\"" +
                      NetworkClient.Esc(PlayerProfile.Id) + "\"}";
        NetworkClient.Instance.Rpc("leave_session", body, null, null);
    }

    void StartPolling()
    {
        if (polling) return;
        polling = true;
        StartCoroutine(PollLoop());
    }

    IEnumerator PollLoop()
    {
        while (polling && Joined)
        {
            PollNow();
            yield return new WaitForSecondsRealtime(State == SessionState.Racing ? 2f : 1f);
        }
        polling = false;
    }

    void PollNow()
    {
        if (!Joined || inFlight) return;
        inFlight = true;
        long t0 = LocalNowMs();

        string body = "{\"p_session_id\":\"" + SessionId + "\",\"p_player_id\":\"" +
                      NetworkClient.Esc(PlayerProfile.Id) + "\"}";

        NetworkClient.Instance.Rpc("get_session", body,
            json => { inFlight = false; ApplySession(json, t0, LocalNowMs()); },
            err => { inFlight = false; Fail(err); });
    }

    void ApplySession(string json, long t0, long t1)
    {
        if (!Joined) return;

        SessionInfo info = null;
        try { info = JsonUtility.FromJson<SessionInfo>(json); } catch { }
        if (info == null || !string.IsNullOrEmpty(info.error)) { Fail("The race session was lost."); return; }

        // Use the reply that travelled fastest to line up this phone's clock with the server's.
        long rtt = t1 - t0;
        if (!haveOffset || rtt <= bestRtt + 40)
        {
            offsetMs = info.server_now_ms - (t0 + t1) / 2;
            haveOffset = true;
            if (rtt < bestRtt) bestRtt = rtt;
        }

        Info = info;
        LastError = null;
        State = ParseState(info.state);

        // The server drops players who go quiet in the lobby. Quietly take the seat again.
        if (!info.member && State == SessionState.Lobby) Join(myName);

        if (OnChanged != null) OnChanged();
    }

    static SessionState ParseState(string s)
    {
        switch (s)
        {
            case "lobby": return SessionState.Lobby;
            case "countdown": return SessionState.Countdown;
            case "racing": return SessionState.Racing;
            case "ended": return SessionState.Ended;
            default: return SessionState.None;
        }
    }

    void Fail(string message)
    {
        LastError = message;
        Debug.LogWarning("[SessionController] " + message);
        if (OnError != null) OnError(message);
    }

    // ---------- reporting scans and the finish ----------

    bool RaceIsOn()
    {
        if (!MultiplayerGate.Online || !Joined) return false;
        if (State == SessionState.Racing) return true;
        return State == SessionState.Countdown && SecondsUntilStart <= 0f;
    }

    void HandleScan(QuestItem item)
    {
        if (item == null || !RaceIsOn()) return;
        if (reported.Add(item.itemId))
        {
            scanQueue.Enqueue(item.itemId);
            Pump();
        }
    }

    void HandleComplete()
    {
        if (!RaceIsOn()) return;
        finishPending = true;
        Pump();
    }

    void Pump()
    {
        if (pumping) return;
        pumping = true;
        StartCoroutine(PumpRoutine());
    }

    IEnumerator PumpRoutine()
    {
        int failures = 0;
        while (scanQueue.Count > 0)
        {
            string id = scanQueue.Peek();
            bool done = false, ok = false;

            string body = "{\"p_session_id\":\"" + SessionId + "\",\"p_player_id\":\"" +
                          NetworkClient.Esc(PlayerProfile.Id) + "\",\"p_item_id\":\"" +
                          NetworkClient.Esc(id) + "\"}";
            NetworkClient.Instance.Rpc("report_scan", body,
                json => { done = true; ok = true; },
                err => { done = true; });

            while (!done) yield return null;

            if (ok) { scanQueue.Dequeue(); failures = 0; }
            else
            {
                failures++;
                if (failures >= 5) { scanQueue.Dequeue(); failures = 0; }
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        if (finishPending)
        {
            finishPending = false;
            yield return FinishRoutine();
        }
        pumping = false;
    }

    IEnumerator FinishRoutine()
    {
        string body = "{\"p_session_id\":\"" + SessionId + "\",\"p_player_id\":\"" +
                      NetworkClient.Esc(PlayerProfile.Id) + "\"}";

        for (int attempt = 0; attempt < 6; attempt++)
        {
            bool done = false;
            FinishResult result = null;

            NetworkClient.Instance.Rpc("finish_run", body,
                json =>
                {
                    try { result = JsonUtility.FromJson<FinishResult>(json); } catch { }
                    done = true;
                },
                err => { done = true; });

            while (!done) yield return null;

            if (result != null)
            {
                LastResult = result;
                if (result.ok && RaceTimer.Instance != null) RaceTimer.Instance.ApplyServerResult(result.duration_ms);
                if (!result.ok) Debug.LogWarning("[SessionController] Finish rejected: " + result.error);
                if (OnResult != null) OnResult();
                yield break;
            }
            yield return new WaitForSecondsRealtime(1.5f);
        }

        Fail("Could not send your finish time. It was not saved to the online leaderboard.");
    }
}
