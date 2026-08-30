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

        // Hold-to-charge edges: press fires only on rising edge, release only on falling edge.
        ThrowManager.ChargeEdges(false, true, out bool b1, out bool e1);
        ThrowManager.ChargeEdges(true, true, out bool b2, out bool e2);
        ThrowManager.ChargeEdges(true, false, out bool b3, out bool e3);
        Debug.Assert(b1 && !e1, "SelfCheck: charge should begin on press");
        Debug.Assert(!b2 && !e2, "SelfCheck: held input should not re-trigger begin");
        Debug.Assert(!b3 && e3, "SelfCheck: charge should end on release");

        Debug.Log("[SelfCheck] passed");
    }
#endif
}
