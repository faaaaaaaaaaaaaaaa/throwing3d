using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Config")]
    [SerializeField] private GameConfig _config;

    [FormerlySerializedAs("_humanStartHp")]
    [SerializeField] private int _playerStartHp = 28;
    [FormerlySerializedAs("_zombieStartHp")]
    [SerializeField] private int _enemyStartHp = 28;
    [SerializeField] private int _headshotDamage = 6;
    [SerializeField] private int _bodyshotDamage = 4;
    [SerializeField] private int _powerThrowDamage = 8;
    [SerializeField] private int _doubleAttackDamage = 3;
    [SerializeField] private int _healAmount = 10;
    [SerializeField] private int _coinsPerWin = 50;

    [Header("UI HP")]
    [FormerlySerializedAs("_humanHpSlider")]
    [SerializeField] private Slider _playerHpSlider;
    [FormerlySerializedAs("_zombieHpSlider")]
    [SerializeField] private Slider _enemyHpSlider;

    [Header("Result UI (optional)")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private GameObject _nextButton;
    [SerializeField] private GameObject _retryButton;
    [SerializeField] private GameObject _reviveButton;
    [SerializeField] private GameObject _doubleCoinsButton;

    [Header("Refs")]
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private ItemManager _itemManager;

    private int _playerHp;
    private int _enemyHp;
    private bool _isGameOver;
    private bool _playerWon;
    private bool _campaignComplete;
    private ResultScreenLayout _resultLayout;
    private int _selectedNumPlayers = 1;
    private int _selectedDifficulty = 0;

    public GameConfig Config => _config;
    public int SelectedNumPlayers => _selectedNumPlayers;
    public int SelectedDifficulty => _selectedDifficulty;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (_turnManager == null) _turnManager = FindAnyObjectByType<TurnManager>();
        if (_itemManager == null) _itemManager = FindAnyObjectByType<ItemManager>();
        ResolveResultButtons();
        ApplyConfigValues();
    }

    private void Start() => SetupHp();

    // ponytail: scenes already have named buttons; resolve if inspector refs are empty.
    private void ResolveResultButtons()
    {
        Transform root = _resultPanel != null ? _resultPanel.transform : null;
        if (_nextButton == null) _nextButton = FindResultChild(root, "NextButton");
        if (_retryButton == null) _retryButton = FindResultChild(root, "RetryButton");
        if (_reviveButton == null) _reviveButton = FindResultChild(root, "ReviveAdButton");
        if (_doubleCoinsButton == null) _doubleCoinsButton = FindResultChild(root, "DoubleCoinsAdButton");
    }

    private static GameObject FindResultChild(Transform root, string name)
    {
        if (root == null) return GameObject.Find(name);
        Transform t = root.Find(name);
        return t != null ? t.gameObject : GameObject.Find(name);
    }

    private void ApplyConfigValues()
    {
        if (_config == null) return;
        _playerStartHp = _config.playerHP;
        _enemyStartHp = GetEnemyHpForDifficulty(_selectedDifficulty);
        _headshotDamage = _config.normalAttack;
        _bodyshotDamage = _config.smallAttack;
        _powerThrowDamage = _config.powerThrow;
        _doubleAttackDamage = _config.doubleAttackDamage;
        _healAmount = _config.healHP;
    }

    public void SetupHp()
    {
        _isGameOver = false;
        _campaignComplete = false;
        ApplyConfigValues();
        _playerHp = _playerStartHp;
        _enemyHp = _enemyStartHp;
        SetSliderHp();
        UpdateHpUi();
        _itemManager?.ResetAllItems();
    }

    private void SetSliderHp()
    {
        if (_playerHpSlider) _playerHpSlider.maxValue = _playerStartHp;
        if (_enemyHpSlider) _enemyHpSlider.maxValue = _enemyStartHp;
    }

    public void UpdateHpUi()
    {
        if (_playerHpSlider) _playerHpSlider.value = _playerHp;
        if (_enemyHpSlider) _enemyHpSlider.value = _enemyHp;
    }

    public void HitPlayer(int damage)
    {
        if (_isGameOver) return;
        _playerHp = Mathf.Max(0, _playerHp - damage);
        UpdateHpUi();
        if (_playerHp <= 0) ShowResult();
    }

    public void HitEnemy(int damage)
    {
        if (_isGameOver) return;
        _enemyHp = Mathf.Max(0, _enemyHp - damage);
        UpdateHpUi();
        if (_enemyHp <= 0) ShowResult();
    }

    public void HealPlayer(int amount)
    {
        _playerHp = Mathf.Clamp(_playerHp + amount, 0, _playerStartHp);
        UpdateHpUi();
    }

    public void HealEnemy(int amount)
    {
        _enemyHp = Mathf.Clamp(_enemyHp + amount, 0, _enemyStartHp);
        UpdateHpUi();
    }

    private void ShowResult()
    {
        _isGameOver = true;
        _playerWon = _enemyHp <= 0;
        int level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : 0;
        _campaignComplete = _playerWon && LevelManager.Instance != null && LevelManager.Instance.IsFinalLevel;
        _resultLayout = ResultScreenLayout.For(_playerWon, _campaignComplete, _coinsPerWin);

        _turnManager?.HideTurnText();
        _turnManager?.StopTimer();

        ResolveResultButtons();
        if (_resultText != null) _resultText.text = _resultLayout.Title ?? string.Empty;
        ApplyResultButtons(_resultLayout);
        if (_resultPanel != null) _resultPanel.SetActive(true);
        UIManager.Instance?.ShowResult();

        if (_playerWon)
        {
            CurrencyWallet.Add(_coinsPerWin);
            LevelManager.Instance?.OnLevelWon();
        }
        Analytics.LevelEnd(level, _playerWon);
    }

    private void ApplyResultButtons(ResultScreenLayout layout)
    {
        // Optional UI: missing buttons just stay unresolved — never throw.
        SetActive(_nextButton, layout.ShowNext);
        SetActive(_retryButton, layout.ShowRetry);
        SetActive(_reviveButton, layout.ShowRevive);
        SetActive(_doubleCoinsButton, layout.ShowDoubleCoins);
        SetButtonLabel(_nextButton, layout.NextLabel);
        SetButtonLabel(_retryButton, layout.RetryLabel);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }

    private static void SetButtonLabel(GameObject button, string label)
    {
        if (button == null || string.IsNullOrEmpty(label)) return;
        var tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = label;
    }

    // Mode select: 1 = vs AI, 2 = local duel.
    public void OnNumberOfPlayersSelected(int num)
    {
        _selectedNumPlayers = Mathf.Clamp(num, 1, 2);
        if (_selectedNumPlayers == 2)
        {
            // 2P skips difficulty and starts immediately on easy baseline HP.
            OnDifficultySelected(0);
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSelectDifficulty();
        }
    }

    public void OnDifficultySelected(int difficulty)
    {
        _selectedDifficulty = Mathf.Clamp(difficulty, 0, 2);
        ApplyConfigValues();
        SetupHp();

        if (UIManager.Instance != null) UIManager.Instance.ShowGameplay();
        LevelManager.Instance?.LoadLevel(LevelManager.Instance.CurrentLevel);

        if (_turnManager != null)
            _turnManager.InitMatch(_selectedNumPlayers, _selectedDifficulty);
        else
            // Fallback if TurnManager is not wired yet.
            Debug.LogWarning("GameManager: TurnManager missing; match started without turn system.");
    }

    public void ConfigureFromLevel(LevelConfig cfg)
    {
        // Level curve still scales enemy toughness on top of difficulty base.
        if (_config != null)
        {
            _playerStartHp = _config.playerHP;
            int baseEnemy = GetEnemyHpForDifficulty(_selectedDifficulty);
            float t = (cfg.levelNumber - 1) / 19f;
            _enemyStartHp = baseEnemy + Mathf.RoundToInt(t * 12f);
        }
        else
        {
            _playerStartHp = cfg.playerHp;
            _enemyStartHp = cfg.enemyHp;
        }
        SetupHp();
    }

    public int GetEnemyMissChance()
    {
        if (_config == null) return 30;
        return _selectedDifficulty switch
        {
            2 => _config.missedChanceHard,
            1 => _config.missedChanceNormal,
            _ => _config.missedChanceEasy
        };
    }

    private int GetEnemyHpForDifficulty(int diff)
    {
        if (_config == null) return _enemyStartHp;
        return diff switch
        {
            2 => _config.enemyHPHard,
            1 => _config.enemyHPMedium,
            _ => _config.enemyHPEasy
        };
    }

    public void ReviveWithAd()
    {
        if (!_isGameOver || _playerWon) return;
        if (AdManager.Instance == null) return;

        AdManager.Instance.ShowRewarded("revive", () =>
        {
            _isGameOver = false;
            _campaignComplete = false;
            _playerHp = Mathf.Max(1, _playerStartHp / 2);
            UpdateHpUi();
            if (_resultPanel) _resultPanel.SetActive(false);
            if (UIManager.Instance) UIManager.Instance.ShowGameplay();
            _turnManager?.InitMatch(_selectedNumPlayers, _selectedDifficulty);
        });
    }

    public void DoubleCoinsWithAd()
    {
        if (!_isGameOver || !_playerWon) return;
        if (AdManager.Instance == null) return;
        AdManager.Instance.ShowRewarded("double_coins", () => CurrencyWallet.Add(_coinsPerWin));
    }

    public void OnRetryPressed()
    {
        EnsureResultLayout();
        AdManager.Instance?.TryShowInterstitial("retry");
        if (_resultPanel != null) _resultPanel.SetActive(false);
        UIManager.Instance?.ShowGameplay();

        if (_resultLayout.RetryIsCampaignRestart && LevelManager.Instance != null)
            LevelManager.Instance.RestartCampaign();
        else if (LevelManager.Instance != null)
            LevelManager.Instance.RetryLevel();
        else
            ResetGame();

        _campaignComplete = false;
        _isGameOver = false;
        _turnManager?.InitMatch(_selectedNumPlayers, _selectedDifficulty);
    }

    public void OnNextPressed()
    {
        EnsureResultLayout();
        if (_resultLayout.NextIsMenu)
        {
            FullResetAndGoToMainMenu();
            return;
        }

        AdManager.Instance?.TryShowInterstitial("next_level");
        if (_resultPanel != null) _resultPanel.SetActive(false);
        UIManager.Instance?.ShowGameplay();

        if (LevelManager.Instance != null) LevelManager.Instance.NextLevel();
        else ResetGame();

        _campaignComplete = false;
        _isGameOver = false;
        _turnManager?.InitMatch(_selectedNumPlayers, _selectedDifficulty);
    }

    // Rebuild layout from current end-state if a button fires before ShowResult (or after domain reload).
    private void EnsureResultLayout()
    {
        if (!string.IsNullOrEmpty(_resultLayout.Title)) return;
        _resultLayout = ResultScreenLayout.For(_playerWon, _campaignComplete, _coinsPerWin);
    }

    public void FullResetAndGoToMainMenu()
    {
        _selectedNumPlayers = 1;
        _selectedDifficulty = 0;
        _isGameOver = false;
        _campaignComplete = false;
        SetupHp();
        if (_resultPanel != null) _resultPanel.SetActive(false);
        _turnManager?.StopTimer();
        _turnManager?.HideTurnText();
        UIManager.Instance?.ShowMainMenu();
    }

    // Editor / SelfCheck: log gaps without crashing Play Mode.
    public void LogMissingCriticalRefs()
    {
        ResolveResultButtons();
        if (_resultPanel == null) Debug.LogWarning("GameManager: ResultPanel missing", this);
        if (_resultText == null) Debug.LogWarning("GameManager: ResultText missing", this);
        if (_nextButton == null) Debug.LogWarning("GameManager: NextButton missing", this);
        if (_retryButton == null) Debug.LogWarning("GameManager: RetryButton missing", this);
        if (_reviveButton == null) Debug.LogWarning("GameManager: ReviveAdButton missing", this);
        if (_doubleCoinsButton == null) Debug.LogWarning("GameManager: DoubleCoinsAdButton missing", this);
    }

    public int PlayerHp => _playerHp;
    public int EnemyHp => _enemyHp;
    public int HeadshotDamage => _headshotDamage;
    public int BodyshotDamage => _bodyshotDamage;
    public int PowerThrowDamage => _powerThrowDamage;
    public int DoubleAttackDamage => _doubleAttackDamage;
    public int HealAmount => _healAmount;
    public bool IsGameOver => _isGameOver;
    public bool PlayerWon => _playerWon;
    public bool CampaignComplete => _campaignComplete;
    public int CoinsPerWin => _coinsPerWin;

    public void ResetGame()
    {
        SetupHp();
        if (_resultPanel) _resultPanel.SetActive(false);
    }
}
