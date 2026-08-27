using UnityEngine;
using UnityEngine.UI;

public class PowerBarUI : MonoBehaviour
{
    [SerializeField] private GameObject _humanPowerBar;
    [SerializeField] private GameObject _zombiePowerBar;
    [SerializeField] private GameObject _humanTimeWarning;
    [SerializeField] private GameObject _zombieTimeWarning;

    [SerializeField] private Image _humanPowerBarImage;
    [SerializeField] private Image _zombiePowerBarImage;
    [SerializeField] private Image _humanTimeWarningImage;
    [SerializeField] private Image _zombieTimeWarningImage;

    private static readonly Color LowPower = new Color(0.35f, 0.95f, 0.45f, 1f);
    private static readonly Color MidPower = new Color(1f, 0.82f, 0.18f, 1f);
    private static readonly Color HighPower = new Color(1f, 0.35f, 0.22f, 1f);

    private void Awake()
    {
        EnsureFillImage(ref _humanPowerBarImage, _humanPowerBar, LowPower);
        EnsureFillImage(ref _zombiePowerBarImage, _zombiePowerBar, new Color(0.55f, 0.75f, 1f, 1f));
    }

    private static void EnsureFillImage(ref Image fill, GameObject barRoot, Color fallbackColor)
    {
        if (fill != null || barRoot == null) return;

        foreach (var image in barRoot.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject == barRoot) continue;
            fill = image;
            break;
        }

        if (fill != null) return;

        var fillGo = new GameObject("PowerFill");
        fillGo.transform.SetParent(barRoot.transform, false);
        var rect = fillGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        fill = fillGo.AddComponent<Image>();
        fill.color = fallbackColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
    }

    public void ShowHumanPowerBar(bool show) => _humanPowerBar?.SetActive(show);
    public void ShowZombiePowerBar(bool show) => _zombiePowerBar?.SetActive(show);

    public void HideBothBars()
    {
        ShowHumanPowerBar(false);
        ShowZombiePowerBar(false);
    }

    public void ShowHumanTimeWarning(bool show) => _humanTimeWarning?.SetActive(show);
    public void ShowZombieTimeWarning(bool show) => _zombieTimeWarning?.SetActive(show);

    public void ShowTimeWarning(bool isPlayer, bool show)
    {
        if (isPlayer) ShowHumanTimeWarning(show);
        else ShowZombieTimeWarning(show);
    }

    public void HideTimeWarnings()
    {
        ShowHumanTimeWarning(false);
        ShowZombieTimeWarning(false);
    }

    public void SetHumanPower(float t)
    {
        t = Mathf.Clamp01(t);
        if (_humanPowerBarImage)
        {
            _humanPowerBarImage.fillAmount = t;
            _humanPowerBarImage.color = PowerColor(t);
        }
    }

    public void SetZombiePower(float t)
    {
        t = Mathf.Clamp01(t);
        if (_zombiePowerBarImage)
        {
            _zombiePowerBarImage.fillAmount = t;
            _zombiePowerBarImage.color = Color.Lerp(new Color(0.45f, 0.7f, 1f), HighPower, t);
        }
    }

    public void SetTimeWarning(bool isPlayer, float t)
    {
        if (isPlayer)
        {
            if (_humanTimeWarningImage) _humanTimeWarningImage.fillAmount = t;
        }
        else
        {
            if (_zombieTimeWarningImage) _zombieTimeWarningImage.fillAmount = t;
        }
    }

    private static Color PowerColor(float t)
    {
        if (t < 0.5f) return Color.Lerp(LowPower, MidPower, t * 2f);
        return Color.Lerp(MidPower, HighPower, (t - 0.5f) * 2f);
    }
}
