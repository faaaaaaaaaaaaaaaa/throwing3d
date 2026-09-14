using UnityEngine;

// Assert-based self-check for the non-trivial pure logic (difficulty curve + ad pacing).
// Runs automatically when entering Play mode in the editor; it fails loudly if the rules break.
// Editor-only so it never ships in a player build.
public static class SelfCheck
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Run()
    {
        // Difficulty must ramp and clamp.
        var l1 = LevelProgression.Generate(1);
        var l20 = LevelProgression.Generate(20);
        Debug.Assert(l1.enemyHp < l20.enemyHp, "SelfCheck: enemy HP should ramp with difficulty");
        Debug.Assert(l1.windMax < l20.windMax, "SelfCheck: wind ceiling should ramp with difficulty");
        Debug.Assert(l1.minThrowPower < l1.maxThrowPower, "SelfCheck: throw power range must be playable");
        Debug.Assert(LevelProgression.Generate(999).levelNumber == 20, "SelfCheck: level index must clamp to max");
        Debug.Assert(LevelProgression.Generate(0).levelNumber == 1, "SelfCheck: level index must clamp to min");

        // Interstitial pacing: block the opening window, block back-to-back, allow after the gap.
        var cap = new AdFrequencyCap(minSecondsBetween: 300f, blockFirstSeconds: 90f);
        Debug.Assert(!cap.CanShow(10f), "SelfCheck: interstitial must be blocked in the first 90s");
        Debug.Assert(cap.CanShow(120f), "SelfCheck: interstitial must be allowed after warmup");
        cap.MarkShown(120f);
        Debug.Assert(!cap.CanShow(200f), "SelfCheck: interstitial must be capped back-to-back");
        Debug.Assert(cap.CanShow(500f), "SelfCheck: interstitial must be allowed after the gap");

        // GameConfig defaults must stay in a playable range (mirrors original JSON).
        var cfg = ScriptableObject.CreateInstance<GameConfig>();
        Debug.Assert(cfg.playerHP > 0 && cfg.enemyHPHard >= cfg.enemyHPEasy, "SelfCheck: GameConfig HP defaults invalid");
        Debug.Assert(cfg.normalAttack >= cfg.smallAttack, "SelfCheck: head damage should be >= body damage");
        Debug.Assert(cfg.timeToThink > 0 && cfg.timeToWarning > 0, "SelfCheck: turn timers must be positive");
        Object.DestroyImmediate(cfg);

        // Lob throws must go up-toward the target, not into the floor.
        var lob = ThrowManager.ComputeLobDirection(new Vector3(5f, 1f, 0f), new Vector3(-5f, 1f, 0f), 38f);
        Debug.Assert(lob.y > 0.25f && lob.x < 0f, "SelfCheck: player lob should arc toward enemy");
        Debug.Assert(Mathf.Approximately(ThrowManager.ApplyWindToThrowForce(4f, 0.5f, 1f, 1f), 4.5f),
            "SelfCheck: tailwind should add wind force to throw power");
        Debug.Assert(Mathf.Approximately(ThrowManager.ApplyWindToThrowForce(4f, 0.5f, -1f, 1f), 3.5f),
            "SelfCheck: headwind should subtract wind force from throw power");

        // Hold-to-charge edges: press fires only on rising edge, release only on falling edge.
        ThrowManager.ChargeEdges(false, true, out bool b1, out bool e1);
        ThrowManager.ChargeEdges(true, true, out bool b2, out bool e2);
        ThrowManager.ChargeEdges(true, false, out bool b3, out bool e3);
        Debug.Assert(b1 && !e1, "SelfCheck: charge should begin on press");
        Debug.Assert(!b2 && !e2, "SelfCheck: held input should not re-trigger begin");
        Debug.Assert(!b3 && e3, "SelfCheck: charge should end on release");

        // Result screen: win/lose/campaign-complete button sets and coin reward copy.
        var win = ResultScreenLayout.For(won: true, campaignComplete: false, coinsPerWin: 50);
        Debug.Assert(win.ShowNext && win.ShowDoubleCoins && !win.ShowRetry && !win.ShowRevive,
            "SelfCheck: mid-campaign win should show Next + DoubleCoins only");
        Debug.Assert(win.Title.Contains("50") && win.Title.Contains("Victory"),
            "SelfCheck: win result must surface the coin reward");

        var lose = ResultScreenLayout.For(won: false, campaignComplete: false, coinsPerWin: 50);
        Debug.Assert(lose.ShowRetry && lose.ShowRevive && !lose.ShowNext && !lose.ShowDoubleCoins,
            "SelfCheck: lose should show Retry + Revive only");

        var cleared = ResultScreenLayout.For(won: true, campaignComplete: true, coinsPerWin: 50);
        Debug.Assert(cleared.NextIsMenu && cleared.RetryIsCampaignRestart && !cleared.ShowRevive,
            "SelfCheck: floor 20 win must end campaign (Menu + Retry Campaign)");
        Debug.Assert(cleared.Title.Contains("Campaign Complete"),
            "SelfCheck: floor 20 win must show campaign-complete copy");

        // Visibility matrix for the three end states (flags only — scene apply checked AfterSceneLoad).
        Debug.Assert(ResultScreenLayout.MatchesVisibility(win, true, false, false, true),
            "SelfCheck: mid-win visibility must be Next+DoubleCoins");
        Debug.Assert(ResultScreenLayout.MatchesVisibility(lose, false, true, true, false),
            "SelfCheck: lose visibility must be Retry+Revive");
        Debug.Assert(ResultScreenLayout.MatchesVisibility(cleared, true, true, false, true),
            "SelfCheck: floor-20 visibility must be Menu+RetryCampaign+DoubleCoins");

        // Broken LFS leaves null prefab slots — picker must skip them, never return null when a real one exists.
        var dummy = new GameObject("SelfCheckThrowable");
        try
        {
            var picked = ThrowManager.PickPrefab(new[] { null, dummy, null });
            Debug.Assert(picked == dummy, "SelfCheck: PickPrefab must skip null LFS slots");
            Debug.Assert(ThrowManager.PickPrefab(new GameObject[] { null, null }) == null,
                "SelfCheck: PickPrefab must return null when all slots are missing");
        }
        finally
        {
            Object.DestroyImmediate(dummy);
        }

        Debug.Log("[SelfCheck] passed");
    }

    // Soft validation after the scene is up — logs only, never throws / asserts.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ValidateSceneRefs()
    {
        var gm = Object.FindAnyObjectByType<GameManager>();
        gm?.LogMissingCriticalRefs();

        var ui = Object.FindAnyObjectByType<UIManager>();
        ui?.LogMissingCriticalRefs();

        if (gm == null) return;
        bool ok = gm.ValidateAllResultLayoutsSilent();
        if (ok) Debug.Log("[SelfCheck] result visibility OK (win/lose/floor-20)");
        else Debug.LogWarning("[SelfCheck] result visibility FAILED — check ResultPanel button refs");
    }
#endif
}
