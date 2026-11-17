using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LeaderboardEntry
{
    public string playerId;
    public string displayName;
    public int durationMs;
    public long finishedAtTicks;
}

/// <summary>
/// Everything the game needs from a leaderboard. The screens only talk to this interface,
/// so a Supabase version can replace the local one without touching any UI.
/// </summary>
public interface ILeaderboardBackend
{
    /// <summary>Saves a finished run. Keeps only each player's fastest. Returns the rank and whether it is a new best.</summary>
    void Submit(LeaderboardEntry entry, Action<int, bool> onDone);

    void GetTop(int count, Action<List<LeaderboardEntry>> onDone);

    void GetBest(string playerId, Action<LeaderboardEntry> onDone);

    /// <summary>True when the server records finished runs itself, so the player has nothing to save.</summary>
    bool SubmitsAutomatically { get; }

    /// <summary>Heading shown above the top 10.</summary>
    string BoardTitle { get; }
}

public static class Leaderboard
{
    static readonly ILeaderboardBackend local = new LocalLeaderboardBackend();
    static ILeaderboardBackend online;

    /// <summary>The online board while the player is in a server race, otherwise the one on this phone.</summary>
    public static ILeaderboardBackend Backend
    {
        get
        {
            if (MultiplayerGate.Online)
            {
                if (online == null) online = new SupabaseLeaderboardBackend();
                return online;
            }
            return local;
        }
    }
}

public static class PlayerProfile
{
    const string IdKey = "yucca.playerId";
    const string NameKey = "yucca.playerName";

    public static string Id
    {
        get
        {
            string id = PlayerPrefs.GetString(IdKey, "");
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(IdKey, id);
                PlayerPrefs.Save();
            }
            return id;
        }
    }

    public static string DisplayName
    {
        get { return PlayerPrefs.GetString(NameKey, ""); }
        set { PlayerPrefs.SetString(NameKey, value ?? ""); PlayerPrefs.Save(); }
    }
}

/// <summary>Stores the leaderboard on this phone only.</summary>
public class LocalLeaderboardBackend : ILeaderboardBackend
{
    const string Key = "yucca.leaderboard.v1";
    const int MaxEntries = 100;

    [Serializable]
    class Store
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    public bool SubmitsAutomatically { get { return false; } }
    public string BoardTitle { get { return "TOP 10 ON THIS PHONE"; } }

    Store Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return new Store();
        try
        {
            var s = JsonUtility.FromJson<Store>(json);
            return s ?? new Store();
        }
        catch { return new Store(); }
    }

    void Save(Store s)
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(s));
        PlayerPrefs.Save();
    }

    static int Compare(LeaderboardEntry a, LeaderboardEntry b)
    {
        int c = a.durationMs.CompareTo(b.durationMs);
        return c != 0 ? c : a.finishedAtTicks.CompareTo(b.finishedAtTicks);
    }

    public void Submit(LeaderboardEntry entry, Action<int, bool> onDone)
    {
        var store = Load();
        bool newBest = true;

        var existing = store.entries.Find(e => e.playerId == entry.playerId);
        if (existing != null)
        {
            if (existing.durationMs <= entry.durationMs)
            {
                newBest = false;
                existing.displayName = entry.displayName;
            }
            else
            {
                store.entries.Remove(existing);
                store.entries.Add(entry);
            }
        }
        else
        {
            store.entries.Add(entry);
        }

        store.entries.Sort(Compare);
        if (store.entries.Count > MaxEntries)
            store.entries.RemoveRange(MaxEntries, store.entries.Count - MaxEntries);
        Save(store);

        int rank = store.entries.FindIndex(e => e.playerId == entry.playerId) + 1;
        onDone?.Invoke(rank, newBest);
    }

    public void GetTop(int count, Action<List<LeaderboardEntry>> onDone)
    {
        var store = Load();
        store.entries.Sort(Compare);
        if (store.entries.Count > count)
            store.entries.RemoveRange(count, store.entries.Count - count);
        onDone?.Invoke(store.entries);
    }

    public void GetBest(string playerId, Action<LeaderboardEntry> onDone)
    {
        onDone?.Invoke(Load().entries.Find(e => e.playerId == playerId));
    }
}
