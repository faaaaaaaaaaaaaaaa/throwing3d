using UnityEngine;
using UnityEngine.UI;

// One-shot special items per match, same rules as the original 2D throwing game.
public class ItemManager : MonoBehaviour
{
    [Header("Player Item Buttons")]
    [SerializeField] private Button _btnPowerThrowPlayer;
    [SerializeField] private Button _btnDoubleAttackPlayer;
    [SerializeField] private Button _btnHealPlayer;

    [Header("Enemy Item Buttons (2P only)")]
    [SerializeField] private Button _btnPowerThrowEnemy;
    [SerializeField] private Button _btnDoubleAttackEnemy;
    [SerializeField] private Button _btnHealEnemy;

    private bool _usedPowerPlayer, _usedDoublePlayer, _usedHealPlayer;
    private bool _usedPowerEnemy, _usedDoubleEnemy, _usedHealEnemy;

    private void Start()
    {
        if (_btnPowerThrowPlayer) _btnPowerThrowPlayer.onClick.AddListener(() => UsePowerThrow(true));
        if (_btnDoubleAttackPlayer) _btnDoubleAttackPlayer.onClick.AddListener(() => UseDoubleAttack(true));
        if (_btnHealPlayer) _btnHealPlayer.onClick.AddListener(() => UseHeal(true));

        if (_btnPowerThrowEnemy) _btnPowerThrowEnemy.onClick.AddListener(() => UsePowerThrow(false));
        if (_btnDoubleAttackEnemy) _btnDoubleAttackEnemy.onClick.AddListener(() => UseDoubleAttack(false));
        if (_btnHealEnemy) _btnHealEnemy.onClick.AddListener(() => UseHeal(false));
    }

    public void ResetAllItems()
    {
        _usedPowerPlayer = _usedDoublePlayer = _usedHealPlayer = false;
        _usedPowerEnemy = _usedDoubleEnemy = _usedHealEnemy = false;
    }

    public void ShowItemsForTurn(bool playerTurn, bool enemyTurn)
    {
        bool twoPlayer = TurnManager.Instance != null && TurnManager.Instance.NumPlayers == 2;

        SetButton(_btnPowerThrowPlayer, !_usedPowerPlayer, playerTurn && !_usedPowerPlayer);
        SetButton(_btnDoubleAttackPlayer, !_usedDoublePlayer, playerTurn && !_usedDoublePlayer);
        SetButton(_btnHealPlayer, !_usedHealPlayer, playerTurn && !_usedHealPlayer);

        SetButton(_btnPowerThrowEnemy, twoPlayer && !_usedPowerEnemy, enemyTurn && !_usedPowerEnemy);
        SetButton(_btnDoubleAttackEnemy, twoPlayer && !_usedDoubleEnemy, enemyTurn && !_usedDoubleEnemy);
        SetButton(_btnHealEnemy, twoPlayer && !_usedHealEnemy, enemyTurn && !_usedHealEnemy);
    }

    public void UsePowerThrow(bool isPlayer)
    {
        if (!IsMyTurn(isPlayer) || TurnManager.Instance == null) return;
        if (isPlayer) { _usedPowerPlayer = true; _btnPowerThrowPlayer?.gameObject.SetActive(false); }
        else { _usedPowerEnemy = true; _btnPowerThrowEnemy?.gameObject.SetActive(false); }
        TurnManager.Instance.SetPowerThrowThisTurn(isPlayer);
    }

    public void UseDoubleAttack(bool isPlayer)
    {
        if (!IsMyTurn(isPlayer) || TurnManager.Instance == null) return;
        if (isPlayer) { _usedDoublePlayer = true; _btnDoubleAttackPlayer?.gameObject.SetActive(false); }
        else { _usedDoubleEnemy = true; _btnDoubleAttackEnemy?.gameObject.SetActive(false); }
        TurnManager.Instance.SetDoubleAttackThisTurn(isPlayer);
    }

    public void UseHeal(bool isPlayer)
    {
        if (!IsMyTurn(isPlayer) || GameManager.Instance == null) return;
        int heal = GameManager.Instance.HealAmount;
        if (isPlayer)
        {
            _usedHealPlayer = true;
            _btnHealPlayer?.gameObject.SetActive(false);
            GameManager.Instance.HealPlayer(heal);
        }
        else
        {
            _usedHealEnemy = true;
            _btnHealEnemy?.gameObject.SetActive(false);
            GameManager.Instance.HealEnemy(heal);
        }
    }

    private bool IsMyTurn(bool isPlayer)
    {
        if (TurnManager.Instance == null) return false;
        return (isPlayer && TurnManager.Instance.CurrentTurn == TurnManager.Turn.Player)
            || (!isPlayer && TurnManager.Instance.CurrentTurn == TurnManager.Turn.Enemy);
    }

    private static void SetButton(Button btn, bool active, bool interactable)
    {
        if (!btn) return;
        btn.gameObject.SetActive(active);
        btn.interactable = interactable;
    }
}
