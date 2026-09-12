using System.Collections.Generic;
using UnityEngine;

// One level's tunable difficulty. Kept to values controllable in code so 20 levels can exist
// without hand-authoring scenes or assets.
public struct LevelConfig
{
    public int levelNumber;
    public int playerHp;
    public int enemyHp;
    public float windMax;       // 0..1, scales the random wind ceiling
    public float minThrowPower;
    public float maxThrowPower;
    public float chargeSpeed;   // faster = tighter timing window
}

// ponytail: levels are generated from one difficulty curve instead of 20 authored assets.
// Upgrade path: replace Generate() with a lookup into hand-tuned LevelConfig data when a designer
// wants bespoke layouts.
public static class LevelProgression
{
    public const int TotalLevels = 20;

    public static LevelConfig Generate(int levelNumber)
    {
        int n = Mathf.Clamp(levelNumber, 1, TotalLevels);
        float t = (n - 1) / (float)(TotalLevels - 1); // 0 at level 1, 1 at level 20

        return new LevelConfig
        {
            levelNumber = n,
            // ~7 body hits (4 dmg) or ~5 head hits (6 dmg) at level 1.
            playerHp = 28,
            enemyHp = 28 + Mathf.RoundToInt(t * 12f),          // 28 -> 40
            windMax = Mathf.Lerp(0.15f, 0.9f, t),              // calmer early, gustier late
            minThrowPower = 2.4f,
            maxThrowPower = Mathf.Lerp(4.6f, 4.2f, t),
            chargeSpeed = Mathf.Lerp(12f, 18f, t),
        };
    }

    public static IEnumerable<LevelConfig> All()
    {
        for (int i = 1; i <= TotalLevels; i++)
            yield return Generate(i);
    }
}
