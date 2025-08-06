using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private int _humanStartHp = 5;
    [SerializeField] private int _zombieStartHp = 5;
    [SerializeField] private int _headshotDamage = 2;
    [SerializeField] private int _bodyshotDamage = 1;

    [Header("UI HP")]
    [SerializeField] private Slider _humanHpSlider;
    [SerializeField] private Slider _zombieHpSlider;

    private int _humanHp;
    private int _zombieHp;
    private bool _isGameOver;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        SetupHp();
    }

    public void SetupHp()
    {
        _isGameOver = false;
        _humanHp = _humanStartHp;
        _zombieHp = _zombieStartHp;
        SetSliderHp();
        UpdateHpUi();
    }

    private void SetSliderHp()
    {
        if (_humanHpSlider) _humanHpSlider.maxValue = _humanStartHp;
        if (_zombieHpSlider) _zombieHpSlider.maxValue = _zombieStartHp;
    }

    public void UpdateHpUi()
    {
        if (_humanHpSlider) _humanHpSlider.value = _humanHp;
        if (_zombieHpSlider) _zombieHpSlider.value = _zombieHp;
    }

    public void HitHuman(int damage)
    {
        if (_isGameOver) return;
        _humanHp -= damage;
        if (_humanHp < 0) _humanHp = 0;
        UpdateHpUi();
        if (_humanHp <= 0) ShowResult();
    }

    public void HitZombie(int damage)
    {
        if (_isGameOver) return;
        _zombieHp -= damage;
        if (_zombieHp < 0) _zombieHp = 0;
        UpdateHpUi();
        if (_zombieHp <= 0) ShowResult();
    }

    private void ShowResult()
    {
        _isGameOver = true;
        string result = _humanHp <= 0 ? "Zombie Wins!" : "Human Wins!";
        Debug.Log(result);
        // TODO: โชว์ UI/อนิเมะจบเกม
    }

    // ตัวอย่าง Getter/Setter
    public int HumanHp => _humanHp;
    public int ZombieHp => _zombieHp;
    public int HeadshotDamage => _headshotDamage;
    public int BodyshotDamage => _bodyshotDamage;
    public bool IsGameOver => _isGameOver;

    public void ResetGame()
    {
        SetupHp();
        // เพิ่มเติม: รีเซ็ตสถานะอื่น ๆ ถ้ามี
    }
}