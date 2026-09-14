using UnityEngine;

// Owns which level is active, persists progress, and pushes the difficulty config into the
// gameplay systems. Drag the ThrowManager in the inspector; GameManager/WindManager are found via
// their singletons.
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private const string PrefKey = "current_level";

    [SerializeField] private ThrowManager _throwManager;

    public int CurrentLevel { get; private set; } = 1;
    public LevelConfig Config { get; private set; }
    public bool IsFinalLevel => CurrentLevel >= LevelProgression.TotalLevels;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        CurrentLevel = Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 1), 1, LevelProgression.TotalLevels);
    }

    // Don't auto-start a match on boot; GameManager starts after mode/difficulty selection.

    public void LoadLevel(int level)
    {
        CurrentLevel = Mathf.Clamp(level, 1, LevelProgression.TotalLevels);
        Config = LevelProgression.Generate(CurrentLevel);

        if (GameManager.Instance != null) GameManager.Instance.ConfigureFromLevel(Config);
        if (_throwManager != null) _throwManager.ConfigureFromLevel(Config);
        if (WindManager.Instance != null) WindManager.Instance.maxForce = Config.windMax;

        Analytics.LevelStart(CurrentLevel);
    }

    // Called on a win: unlock the next level so returning players resume where they left off.
    // Floor 20 stays at 20 — campaign complete is a UI state, not a level 21.
    public void OnLevelWon()
    {
        if (IsFinalLevel)
        {
            PlayerPrefs.SetInt(PrefKey, LevelProgression.TotalLevels);
            PlayerPrefs.Save();
            return;
        }

        int next = CurrentLevel + 1;
        PlayerPrefs.SetInt(PrefKey, next);
        PlayerPrefs.Save();
    }

    public void NextLevel()
    {
        if (IsFinalLevel) return;
        LoadLevel(CurrentLevel + 1);
    }

    public void RetryLevel()
    {
        Analytics.Retry(CurrentLevel);
        LoadLevel(CurrentLevel);
    }

    public void RestartCampaign()
    {
        PlayerPrefs.SetInt(PrefKey, 1);
        PlayerPrefs.Save();
        LoadLevel(1);
    }
}
