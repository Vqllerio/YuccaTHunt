using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The shared, all-time leaderboard on the server. The server records each finished race
/// itself, so there is nothing for the player to submit.
/// </summary>
public class SupabaseLeaderboardBackend : ILeaderboardBackend
{
    public bool SubmitsAutomatically { get { return true; } }
    public string BoardTitle { get { return "ALL-TIME TOP 10"; } }

    public void Submit(LeaderboardEntry entry, Action<int, bool> onDone)
    {
        if (onDone != null) onDone(0, false);
    }

    public void GetTop(int count, Action<List<LeaderboardEntry>> onDone)
    {
        Fetch(count, info =>
        {
            var list = new List<LeaderboardEntry>();
            if (info.top != null)
            {
                foreach (var row in info.top)
                    list.Add(new LeaderboardEntry
                    {
                        playerId = row.player_id,
                        displayName = row.display_name,
                        durationMs = row.duration_ms
                    });
            }
            if (onDone != null) onDone(list);
        });
    }

    public void GetBest(string playerId, Action<LeaderboardEntry> onDone)
    {
        Fetch(1, info =>
        {
            LeaderboardEntry e = null;
            if (info.personal_best_ms > 0)
                e = new LeaderboardEntry
                {
                    playerId = playerId,
                    displayName = PlayerProfile.DisplayName,
                    durationMs = info.personal_best_ms
                };
            if (onDone != null) onDone(e);
        });
    }

    void Fetch(int count, Action<LeaderboardInfo> done)
    {
        string body = "{\"p_limit\":" + count + ",\"p_player_id\":\"" + NetworkClient.Esc(PlayerProfile.Id) + "\"}";
        NetworkClient.Instance.Rpc("get_leaderboard", body,
            json =>
            {
                LeaderboardInfo info = null;
                try { info = JsonUtility.FromJson<LeaderboardInfo>(json); } catch { }
                done(info ?? new LeaderboardInfo());
            },
            err =>
            {
                Debug.LogWarning("[Leaderboard] " + err);
                done(new LeaderboardInfo());
            });
    }
}
