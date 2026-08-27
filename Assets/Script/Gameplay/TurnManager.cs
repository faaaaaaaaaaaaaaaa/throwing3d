using System.Collections;
using TMPro;
using UnityEngine;

// Turn loop ported from the original 2D throwing game, adapted for Adventurer vs Skeleton 3D.
public class TurnManager : MonoBehaviour
{
    public enum Turn { Player, Enemy }

    public static TurnManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private ThrowManager _throwManager;
    [SerializeField] private PowerBarUI _powerBarUI;
    [SerializeField] private ItemManager _itemManager;
    [SerializeField] private TMP_Text _turnText;

    [Header("AI")]
    [SerializeField] private float _windLowThreshold = 0.25f;
    [SerializeField] private float _windHighThreshold = 0.65f;
    [SerializeField] private float _aiThinkDelay = 1f;

    public Turn CurrentTurn { get; private set; } = Turn.Player;
    public int NumPlayers { get; private set; } = 1;
    public int Difficulty { get; private set; } = 0;
    public bool IsAiTurn { get; private set; }
    public bool IsWaitingForHit { get; private set; }

    private bool _powerThrowPlayer;
    private bool _powerThrowEnemy;
    private bool _doubleAttackPlayer;
    private bool _doubleAttackEnemy;
    private Coroutine _timerRoutine;
    private Coroutine _aiRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void InitMatch(int numPlayers, int difficulty)
    {
        NumPlayers = Mathf.Clamp(numPlayers, 1, 2);
        Difficulty = Mathf.Clamp(difficulty, 0, 2);
        _powerThrowPlayer = _powerThrowEnemy = false;
        _doubleAttackPlayer = _doubleAttackEnemy = false;
        _itemManager?.ResetAllItems();
        SetTurn(Turn.Player);
    }

    public void SetTurn(Turn turn)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        CurrentTurn = turn;
        IsWaitingForHit = false;
        IsAiTurn = false;
        StopTimer();
        if (_aiRoutine != null) { StopCoroutine(_aiRoutine); _aiRoutine = null; }

        UpdateTurnText();
        WindManager.Instance?.RandomWind();
        _powerBarUI?.HideBothBars();
        _powerBarUI?.HideTimeWarnings();

        bool playerTurn = CurrentTurn == Turn.Player;
        _itemManager?.ShowItemsForTurn(playerTurn, !playerTurn && NumPlayers == 2);

        if (CurrentTurn == Turn.Player)
        {
            IsAiTurn = false;
            _timerRoutine = StartCoroutine(PlayerTurnTimer(true));
        }
        else
        {
            IsAiTurn = NumPlayers == 1;
            if (IsAiTurn)
                _aiRoutine = StartCoroutine(AiThrowRoutine());
            else
                _timerRoutine = StartCoroutine(PlayerTurnTimer(false));
        }
    }

    public void SetPowerThrowThisTurn(bool isPlayer)
    {
        if (isPlayer) _powerThrowPlayer = true;
        else _powerThrowEnemy = true;
    }

    public void SetDoubleAttackThisTurn(bool isPlayer)
    {
        if (isPlayer) _doubleAttackPlayer = true;
        else _doubleAttackEnemy = true;
    }

    public bool IsPowerThrowThisTurn(bool isPlayer) => isPlayer ? _powerThrowPlayer : _powerThrowEnemy;
    public bool IsDoubleAttackThisTurn(bool isPlayer) => isPlayer ? _doubleAttackPlayer : _doubleAttackEnemy;

    public void OnPowerConfirmed(float power01, bool isPlayer)
    {
        if (IsWaitingForHit || GameManager.Instance == null || GameManager.Instance.IsGameOver) return;
        if ((isPlayer && CurrentTurn != Turn.Player) || (!isPlayer && CurrentTurn != Turn.Enemy)) return;
        if (_throwManager == null) return;

        IsWaitingForHit = true;
        StopTimer();
        _powerBarUI?.HideTimeWarnings();

        bool isDouble = IsDoubleAttackThisTurn(isPlayer);
        bool isPower = IsPowerThrowThisTurn(isPlayer);
        _powerThrowPlayer = _powerThrowEnemy = false;
        _doubleAttackPlayer = _doubleAttackEnemy = false;

        if (isDouble)
            StartCoroutine(DoubleAttackSequence(isPlayer, power01));
        else
            _throwManager.ThrowCharged(isPlayer, power01, isPower ? "PowerThrow" : null, OnItemResolved);
    }

    private IEnumerator DoubleAttackSequence(bool isPlayer, float power01)
    {
        bool done = false;
        _throwManager.ThrowCharged(isPlayer, power01, "DoubleAttack", () => done = true);
        yield return new WaitUntil(() => done);
        yield return new WaitForSeconds(0.45f);

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;

        done = false;
        _throwManager.ThrowCharged(isPlayer, power01, "DoubleAttack", () => done = true);
        yield return new WaitUntil(() => done);
        OnItemResolved();
    }

    private void OnItemResolved()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        IsWaitingForHit = false;
        SwitchTurn();
    }

    public void SwitchTurn() => SetTurn(CurrentTurn == Turn.Player ? Turn.Enemy : Turn.Player);

    public void StopTimer()
    {
        if (_timerRoutine != null) StopCoroutine(_timerRoutine);
        _timerRoutine = null;
    }

    private IEnumerator PlayerTurnTimer(bool isPlayer)
    {
        var cfg = GameManager.Instance?.Config;
        float think = cfg != null ? cfg.timeToThink : 30f;
        float warn = cfg != null ? cfg.timeToWarning : 10f;

        float t = 0f;
        while (t < think)
        {
            if (!IsSameTurn(isPlayer) || GameManager.Instance.IsGameOver) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        _powerBarUI?.ShowTimeWarning(isPlayer, true);
        t = 0f;
        while (t < warn)
        {
            if (!IsSameTurn(isPlayer) || GameManager.Instance.IsGameOver)
            {
                _powerBarUI?.HideTimeWarnings();
                yield break;
            }
            _powerBarUI?.SetTimeWarning(isPlayer, Mathf.Lerp(1f, 0f, t / warn));
            t += Time.deltaTime;
            yield return null;
        }

        _powerBarUI?.HideTimeWarnings();
        SwitchTurn();
    }

    private IEnumerator AiThrowRoutine()
    {
        yield return new WaitForSeconds(_aiThinkDelay);
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) yield break;

        float wind = WindManager.Instance ? WindManager.Instance.windForce : 0f;
        int missChance = GameManager.Instance.GetEnemyMissChance();
        bool willMiss = Random.Range(0, 100) < missChance;

        float aiPower;
        string special = null;
        if (willMiss)
        {
            aiPower = Random.Range(0.08f, 0.17f);
        }
        else
        {
            aiPower = Random.Range(0.6f, 0.92f);
            if (Difficulty == 2 && wind > _windHighThreshold) special = "PowerThrow";
            else if (Difficulty == 1 && wind < _windLowThreshold) special = "DoubleAttack";
        }

        if (_powerBarUI != null && _throwManager != null)
        {
            _powerBarUI.ShowZombiePowerBar(true);
            float shown = 0f;
            const float chargeDuration = 0.8f;
            while (shown < aiPower)
            {
                shown += Time.deltaTime / chargeDuration;
                _powerBarUI.SetZombiePower(Mathf.Clamp01(shown));
                yield return null;
            }
            yield return new WaitForSeconds(0.15f);
            _powerBarUI.ShowZombiePowerBar(false);
        }

        IsWaitingForHit = true;
        if (special == "DoubleAttack")
            yield return StartCoroutine(DoubleAttackSequence(false, aiPower));
        else
        {
            bool done = false;
            _throwManager.ThrowCharged(false, aiPower, special, () => done = true);
            yield return new WaitUntil(() => done);
            OnItemResolved();
        }
    }

    private bool IsSameTurn(bool isPlayer) =>
        (isPlayer && CurrentTurn == Turn.Player) || (!isPlayer && CurrentTurn == Turn.Enemy);

    private void UpdateTurnText()
    {
        if (_turnText == null) return;
        if (NumPlayers == 2)
            _turnText.text = CurrentTurn == Turn.Player
                ? "Player 1 — pull back & release"
                : "Player 2 — pull back & release";
        else
            _turnText.text = CurrentTurn == Turn.Player
                ? "Your turn — pull back & release"
                : "Skeleton's turn…";
    }

    public void HideTurnText()
    {
        if (_turnText) _turnText.text = "";
    }
}
