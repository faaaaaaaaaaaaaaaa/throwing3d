using UnityEngine;

// ponytail: analytics is a Debug.Log stub so the funnel is instrumented from day one without a
// heavy SDK. Upgrade path: forward each event to Unity Analytics / Firebase / GameAnalytics.
public static class Analytics
{
    public static void LevelStart(int level) => Log($"level_start level={level}");
    public static void LevelEnd(int level, bool won) => Log($"level_end level={level} won={won}");
    public static void Retry(int level) => Log($"retry level={level}");
    public static void AdOfferShown(string placement) => Log($"ad_offer_shown placement={placement}");
    public static void AdStarted(string placement) => Log($"ad_started placement={placement}");
    public static void AdCompleted(string placement) => Log($"ad_completed placement={placement}");
    public static void AdRewardGranted(string placement) => Log($"ad_reward_granted placement={placement}");

    private static void Log(string evt) => Debug.Log($"[Analytics] {evt}");
}

// ponytail: coins persist via PlayerPrefs. Fine for a single local profile; upgrade path is a
// server-backed wallet if cross-device sync or anti-cheat ever matters.
public static class CurrencyWallet
{
    private const string Key = "coins";

    public static int Coins => PlayerPrefs.GetInt(Key, 0);

    public static void Add(int amount)
    {
        PlayerPrefs.SetInt(Key, Mathf.Max(0, Coins + amount));
        PlayerPrefs.Save();
    }

    public static bool TrySpend(int amount)
    {
        if (amount <= 0 || Coins < amount) return false;
        PlayerPrefs.SetInt(Key, Coins - amount);
        PlayerPrefs.Save();
        return true;
    }
}
