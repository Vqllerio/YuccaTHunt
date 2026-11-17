/// <summary>
/// Connection details for the online race. Fill in both values from Supabase:
/// Project Settings > API > Project URL and the "anon public" key.
/// While either is empty the game runs in solo mode with a local leaderboard.
/// </summary>
public static class YuccaBackendConfig
{
    public static readonly string Url = "https://cpflgqetenzabonsvgef.supabase.co";

    public static readonly string AnonKey = "sb_publishable_4Rr93fURFRVRY2a_b_Z6Tw_RNL_Yq0K";

    public static bool IsConfigured
    {
        get { return !string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(AnonKey); }
    }
}
