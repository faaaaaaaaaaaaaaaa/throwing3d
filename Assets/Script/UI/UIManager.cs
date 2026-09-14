using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas References (Drag from Inspector)")]
    [SerializeField] private Canvas _loginCanvas;
    [SerializeField] private Canvas _mainMenuCanvas;
    [SerializeField] private Canvas _howToPlayCanvas;
    [SerializeField] private Canvas _selectModeCanvas;
    [SerializeField] private Canvas _selectDifficultyCanvas;
    [SerializeField] private Canvas _gameplayCanvas;
    [SerializeField] private Canvas _resultCanvas;
    [SerializeField] private List<Canvas> _allCanvases;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (_mainMenuCanvas != null) ShowMainMenu();
        else if (_loginCanvas != null) ShowLogin();
        else if (_gameplayCanvas != null) ShowGameplay();
    }

    public bool IsGameplayActive => _gameplayCanvas == null || _gameplayCanvas.gameObject.activeSelf;
    public Canvas ResultCanvas => _resultCanvas;

    public void ShowOnly(Canvas canvasToShow)
    {
        if (_allCanvases == null) return;
        // Null canvasToShow hides every known canvas — safe no-op for missing optional refs.
        foreach (var c in _allCanvases)
        {
            if (c == null) continue;
            bool show = canvasToShow != null && c == canvasToShow;
            if (c.gameObject != null && c.gameObject.activeSelf != show)
                c.gameObject.SetActive(show);
        }
    }

    // Legacy quick play (skips mode/difficulty). Prefer OnClickOnePlayer / OnClickTwoPlayers.
    public void PlayCurrentLevel()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDifficultySelected(GameManager.Instance.SelectedDifficulty);
        else
        {
            ShowGameplay();
            LevelManager.Instance?.LoadLevel(LevelManager.Instance.CurrentLevel);
        }
    }

    // Wire these to mode-select buttons.
    public void OnClickOnePlayer() => GameManager.Instance?.OnNumberOfPlayersSelected(1);
    public void OnClickTwoPlayers() => GameManager.Instance?.OnNumberOfPlayersSelected(2);

    // Wire these to difficulty buttons (0 easy / 1 normal / 2 hard).
    public void OnClickDifficultyEasy() => GameManager.Instance?.OnDifficultySelected(0);
    public void OnClickDifficultyNormal() => GameManager.Instance?.OnDifficultySelected(1);
    public void OnClickDifficultyHard() => GameManager.Instance?.OnDifficultySelected(2);

    public void OnClickReturnToMainMenu() => GameManager.Instance?.FullResetAndGoToMainMenu();

    public void ShowLogin() => ShowOnly(_loginCanvas);
    public void ShowMainMenu()
    {
        if (_mainMenuCanvas == null)
        {
            Debug.LogWarning("UIManager: MainMenuCanvas missing", this);
            return;
        }
        ShowOnly(_mainMenuCanvas);
        // If mode canvas is separate, callers can still open it from a Play button.
    }
    public void ShowHowToPlay() => ShowOnly(_howToPlayCanvas);
    public void ShowSelectMode() => ShowOnly(_selectModeCanvas != null ? _selectModeCanvas : _mainMenuCanvas);
    public void ShowSelectDifficulty() => ShowOnly(_selectDifficultyCanvas);
    public void ShowGameplay()
    {
        if (_gameplayCanvas == null)
        {
            Debug.LogWarning("UIManager: GameplayCanvas missing", this);
            return;
        }
        ShowOnly(_gameplayCanvas);
    }
    public void ShowResult()
    {
        if (_resultCanvas == null)
        {
            Debug.LogWarning("UIManager: ResultCanvas missing", this);
            return;
        }
        ShowOnly(_resultCanvas);
    }

    public void LogMissingCriticalRefs()
    {
        if (_mainMenuCanvas == null) Debug.LogWarning("UIManager: MainMenuCanvas missing", this);
        if (_gameplayCanvas == null) Debug.LogWarning("UIManager: GameplayCanvas missing", this);
        if (_resultCanvas == null) Debug.LogWarning("UIManager: ResultCanvas missing", this);
        if (_allCanvases == null || _allCanvases.Count == 0)
            Debug.LogWarning("UIManager: _allCanvases empty", this);
    }
}
