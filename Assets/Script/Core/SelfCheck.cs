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
        ThrowManager.ComputeDragAim(new Vector2(-100f, 0f), 200f, 18f, 70f, out float lowPower, out float lowAngle);
        ThrowManager.ComputeDragAim(new Vector2(-100f, 100f), 200f, 18f, 70f, out float highPower, out float highAngle);
        Debug.Assert(highPower > lowPower, "SelfCheck: a wider pull must produce more power");
        Debug.Assert(highAngle > lowAngle, "SelfCheck: pulling downward must produce a higher arc");

        Debug.Log("[SelfCheck] passed");
    }
#endif
}
