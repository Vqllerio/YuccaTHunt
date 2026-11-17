using System;
using UnityEngine;

/// <summary>
/// Times a run. Starts when the player taps Begin on the intro panel and stops on the last scan.
/// Creates itself if the scene does not have one.
/// </summary>
public class RaceTimer : MonoBehaviour
{
    public static RaceTimer Instance { get; private set; }

    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }
    public float Elapsed { get; private set; }

    /// <summary>True once a complete run has been timed.</summary>
    public bool HasResult { get { return IsFinished && Elapsed > 0f; } }
    public int ElapsedMs { get { return Mathf.RoundToInt(Elapsed * 1000f); } }

    public event Action OnStarted;
    public event Action<float> OnFinished;

    EntranceTrigger entrance;
    QuestManager qm;
    bool waitingForFirstScan;
    bool serverMode;
    long serverStartMs;

    public static RaceTimer EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("RaceTimer");
        return go.AddComponent<RaceTimer>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        qm = QuestManager.Instance;
        if (qm != null) qm.OnQuestCompleted += Finish;

        entrance = FindAnyObjectByType<EntranceTrigger>();
        if (entrance != null)
        {
            if (entrance.HasIntroBeenClosed) HandleIntroClosed();
            else entrance.OnIntroClosed += HandleIntroClosed;
        }
        else if (qm != null)
        {
            // No entrance poster in this scene: start the clock on the first scan.
            waitingForFirstScan = true;
            qm.OnItemScanned += FirstScan;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (qm != null)
        {
            qm.OnQuestCompleted -= Finish;
            qm.OnItemScanned -= FirstScan;
        }
        if (entrance != null) entrance.OnIntroClosed -= HandleIntroClosed;
    }

    void HandleIntroClosed()
    {
        // With an online backend, the race starts from the lobby instead.
        if (MultiplayerGate.LobbyRequired) return;
        StartRun();
    }

    void FirstScan(QuestItem item)
    {
        if (!waitingForFirstScan) return;
        waitingForFirstScan = false;
        StartRun();
    }

    public void StartRun()
    {
        if (IsRunning || IsFinished) return;
        Elapsed = 0f;
        IsRunning = true;
        OnStarted?.Invoke();
    }

    /// <summary>Starts the clock from a server-issued start time (milliseconds since 1970).</summary>
    public void StartRunAtServer(long startMs)
    {
        if (IsRunning || IsFinished) return;
        serverMode = true;
        serverStartMs = startMs;
        Elapsed = 0f;
        IsRunning = true;
        OnStarted?.Invoke();
    }

    /// <summary>Replaces the phone's estimate with the time the server measured.</summary>
    public void ApplyServerResult(int milliseconds)
    {
        Elapsed = milliseconds / 1000f;
    }

    void Finish()
    {
        if (!IsRunning) return;
        IsRunning = false;
        IsFinished = true;
        OnFinished?.Invoke(Elapsed);
    }

    void Update()
    {
        if (!IsRunning) return;
        if (serverMode)
        {
            var sc = SessionController.Instance;
            if (sc != null) Elapsed = Mathf.Max(0f, (sc.ServerNowMs - serverStartMs) / 1000f);
        }
        else
        {
            Elapsed += Time.unscaledDeltaTime;
        }
    }

    /// <summary>mm:ss</summary>
    public static string Format(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    /// <summary>mm:ss.cc</summary>
    public static string FormatPrecise(int milliseconds)
    {
        milliseconds = Mathf.Max(0, milliseconds);
        int total = milliseconds / 1000;
        int cs = (milliseconds % 1000) / 10;
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00") + "." + cs.ToString("00");
    }
}
