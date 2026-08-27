#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameConfigImporter
{
    [MenuItem("Tools/Throwing3D/Import GameConfig JSON")]
    public static void Import()
    {
        string path = EditorUtility.OpenFilePanel("Select ThrowingGameData.json", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        // Unity's JsonUtility needs a wrapper for arrays.
        var rows = JsonUtility.FromJson<Wrapper>("{\"items\":" + json + "}");
        if (rows == null || rows.items == null || rows.items.Length == 0)
        {
            Debug.LogError("GameConfigImporter: failed to parse JSON.");
            return;
        }

        var config = ScriptableObject.CreateInstance<GameConfig>();
        foreach (var row in rows.items)
        {
            switch (row.name)
            {
                case "Player HP": config.playerHP = row.hp; break;
                case "Enemy HP(easy)": config.enemyHPEasy = row.hp; config.missedChanceEasy = row.missedChance; break;
                case "Enemy HP(normal)": config.enemyHPMedium = row.hp; config.missedChanceNormal = row.missedChance; break;
                case "Enemy HP(hard)": config.enemyHPHard = row.hp; config.missedChanceHard = row.missedChance; break;
                case "Normal Attack": config.normalAttack = row.damage; break;
                case "Small Attack": config.smallAttack = row.damage; break;
                case "Power throw": config.powerThrow = row.damage; break;
                case "Double Attack": config.doubleAttackAmount = row.amount; config.doubleAttackDamage = row.damage; break;
                case "Heal": config.healHP = row.hp; break;
                case "Time to think": config.timeToThink = row.sec; break;
                case "Time to Warning": config.timeToWarning = row.sec; break;
            }
        }

        const string assetPath = "Assets/Script/GameData/GameConfig.asset";
        AssetDatabase.CreateAsset(config, assetPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = config;
        Debug.Log($"GameConfigImporter: wrote {assetPath}");
    }

    [System.Serializable]
    private class Wrapper { public GameConfigJsonRow[] items; }
}
#endif
