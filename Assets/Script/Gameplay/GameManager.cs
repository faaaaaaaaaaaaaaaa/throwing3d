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
    [SerializeField] private int _playerStartHp = 50;
    [FormerlySerializedAs("_zombieStartHp")]
    [SerializeField] private int _enemyStartHp = 50;
    [SerializeField] private int _headshotDamage = 6;
    [SerializeField] private int _bodyshotDamage = 4;
    [SerializeField] private int _powerThrowDamage = 8;
    [SerializeField] private int _doubleAttackDamage = 3;
    [SerializeField] private int _healAmount = 20;
    [SerializeField] private int _coinsPerWin = 50;

    [Header("UI HP")]
    [FormerlySerializedAs("_humanHpSlider")]
    [SerializeField] private Slider _playerHpSlider;
    [FormerlySerializedAs("_zombieHpSlider")]
    [SerializeField] private Slider _enemyHpSlider;

    [Header("Result UI (optional)")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TMP_Text _resultText;

    [Header("Refs")]
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private ItemManager _itemManager;

    private int _playerHp;
    private int _enemyHp;
    private bool _isGameOver;
    private bool _playerWon;
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
        ApplyConfigValues();
    }

    private void Start() => SetupHp();

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
        string result = _playerWon ? "Victory!" : "Defeated...";

        _turnManager?.HideTurnText();
        _turnManager?.StopTimer();

        if (_resultText) _resultText.text = result;
        if (_resultPanel) _resultPanel.SetActive(true);
        if (UIManager.Instance) UIManager.Instance.ShowResult();

        int level = LevelManager.Instance ? LevelManager.Instance.CurrentLevel : 0;
        if (_playerWon)
        {
            CurrencyWallet.Add(_coinsPerWin);
            LevelManager.Instance?.OnLevelWon();
        }
        Analytics.LevelEnd(level, _playerWon);
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
            _enemyStartHp = baseEnemy + Mathf.RoundToInt(t * 20f);
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
        AdManager.Instance?.TryShowInterstitial("retry");
        if (_resultPanel) _resultPanel.SetActive(false);
        if (UIManager.Instance) UIManager.Instance.ShowGameplay();

        if (LevelManager.Instance != null) LevelManager.Instance.RetryLevel();
        else ResetGame();

        _turnManager?.InitMatch(_selectedNumPlayers, _selectedDifficulty);
    }

    public void OnNextPressed()
    {
        AdManager.Instance?.TryShowInterstitial("next_level");
        if (_resultPanel) _resultPanel.SetActive(false);
        if (UIManager.Instance) UIManager.Instance.ShowGameplay();

        if (LevelManager.Instance != null) LevelManager.Instance.NextLevel();
        else ResetGame();

        _turnManager?.InitMatch(_selectedNumPlayers, _selectedDifficulty);
    }

    public void FullResetAndGoToMainMenu()
    {
        _selectedNumPlayers = 1;
        _selectedDifficulty = 0;
        _isGameOver = false;
        SetupHp();
        if (_resultPanel) _resultPanel.SetActive(false);
        _turnManager?.StopTimer();
        _turnManager?.HideTurnText();
        UIManager.Instance?.ShowMainMenu();
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

    public void ResetGame()
    {
        SetupHp();
        if (_resultPanel) _resultPanel.SetActive(false);
    }
}
