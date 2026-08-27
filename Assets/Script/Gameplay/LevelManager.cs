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
    public void OnLevelWon()
    {
        int next = Mathf.Min(CurrentLevel + 1, LevelProgression.TotalLevels);
        PlayerPrefs.SetInt(PrefKey, next);
        PlayerPrefs.Save();
    }

    public void NextLevel() => LoadLevel(Mathf.Min(CurrentLevel + 1, LevelProgression.TotalLevels));

    public void RetryLevel()
    {
        Analytics.Retry(CurrentLevel);
        LoadLevel(CurrentLevel);
    }
}
