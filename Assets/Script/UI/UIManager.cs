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
    [SerializeField] private List<Canvas> _allCanvases; // drag evey canvas to this too

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // activate only canvas want
    public void ShowOnly(Canvas canvasToShow)
    {
        foreach (var c in _allCanvases)
            c.gameObject.SetActive(c == canvasToShow);
    }

    public void ShowLogin() => ShowOnly(_loginCanvas);
    public void ShowMainMenu() => ShowOnly(_mainMenuCanvas);
    public void ShowHowToPlay() => ShowOnly(_howToPlayCanvas);
    public void ShowSelectMode() => ShowOnly(_selectModeCanvas);
    public void ShowSelectDifficulty() => ShowOnly(_selectDifficultyCanvas);
    public void ShowGameplay() => ShowOnly(_gameplayCanvas);
    public void ShowResult() => ShowOnly(_resultCanvas);
}