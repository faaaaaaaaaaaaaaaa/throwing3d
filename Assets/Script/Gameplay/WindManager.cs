using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum WindDirection { Left, Right }

public class WindManager : MonoBehaviour
{
    public static WindManager Instance;

    [Header("UI")]
    public GameObject fillLeft, fillRight;   // parent objects
    public Image fillImageLeft, fillImageRight;
    public GameObject arrowLeft, arrowRight;
    public TMP_Text textWind;
    public Slider sliderLeft, sliderRight;

    [Header("Config")]
    public WindDirection windDirection;
    public float windForce; // 0–1
    [Range(0f, 1f)] public float maxForce = 1f; // per-level ceiling for random wind

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ResolveUiReferences();
        SetWind(windDirection, windForce);
    }

    public void SetWind(WindDirection dir, float force)
    {
        ResolveUiReferences();

        windDirection = dir;
        windForce = Mathf.Clamp01(force);

        // hide all (null-safe so scene wiring can be incomplete during setup)
        if (fillLeft) fillLeft.SetActive(false);
        if (fillRight) fillRight.SetActive(false);
        if (arrowLeft) arrowLeft.SetActive(false);
        if (arrowRight) arrowRight.SetActive(false);

        if (dir == WindDirection.Right)
        {
            if (fillRight) fillRight.SetActive(true);
            if (arrowRight) arrowRight.SetActive(true);
            if (fillImageRight) fillImageRight.fillAmount = windForce;
            if (sliderRight) sliderRight.value = windForce;
        }
        else
        {
            if (fillLeft) fillLeft.SetActive(true);
            if (arrowLeft) arrowLeft.SetActive(true);
            if (fillImageLeft) fillImageLeft.fillAmount = windForce;
            if (sliderLeft) sliderLeft.value = windForce;
        }

        if (sliderLeft)
        {
            sliderLeft.minValue = 0f;
            sliderLeft.maxValue = 1f;
            sliderLeft.value = dir == WindDirection.Left ? windForce : 0f;
        }

        if (sliderRight)
        {
            sliderRight.minValue = 0f;
            sliderRight.maxValue = 1f;
            sliderRight.value = dir == WindDirection.Right ? windForce : 0f;
        }

        if (textWind) textWind.text = "WIND";
    }

    // random wind
    public void RandomWind()
    {
        var dir = (UnityEngine.Random.value > 0.5f) ? WindDirection.Right : WindDirection.Left;
        var force = UnityEngine.Random.Range(0f, Mathf.Clamp01(maxForce));
        SetWind(dir, force);
    }

    private void ResolveUiReferences()
    {
        if (fillImageLeft != null && fillImageRight != null && textWind != null)
            return;

        Transform root = FindTransformByName("WindImage") ?? FindTransformByName("WindPanel");
        if (root == null) return;

        if (fillLeft == null)
            fillLeft = FindChildGameObject(root, "LeftBGSlideImage") ?? FindChildGameObject(root, "LeftWindFillRoot");
        if (fillRight == null)
            fillRight = FindChildGameObject(root, "RightFillImage") ?? FindChildGameObject(root, "RightWindFillRoot");
        if (fillImageLeft == null)
            fillImageLeft = FindNamedImage(root, "LeftFillImage", requireFilled: true);
        if (fillImageRight == null)
            fillImageRight = FindNamedImage(root, "RightFillImage", requireFilled: true);
        if (arrowLeft == null)
            arrowLeft = FindChildGameObject(root, "LeftImage") ?? FindChildGameObject(root, "ArrowLeft");
        if (arrowRight == null)
            arrowRight = FindChildGameObject(root, "RightImage") ?? FindChildGameObject(root, "ArrowRight");
        if (textWind == null)
            textWind = FindNamedText(root, "WINDText (TMP)") ?? FindNamedText(root, "WindLabel");
        if (sliderLeft == null)
            sliderLeft = FindNamedSlider(root, "LeftWindSlider");
        if (sliderRight == null)
            sliderRight = FindNamedSlider(root, "RightWindSlider");

        ConfigureFill(fillImageLeft, Image.OriginHorizontal.Right);
        ConfigureFill(fillImageRight, Image.OriginHorizontal.Left);
    }

    private static void ConfigureFill(Image image, Image.OriginHorizontal origin)
    {
        if (image == null) return;

        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)origin;
    }

    private static Transform FindTransformByName(string objectName)
    {
        foreach (Transform t in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == objectName)
                return t;
        }

        return null;
    }

    private static GameObject FindChildGameObject(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child.gameObject;
        }

        return null;
    }

    private static Image FindNamedImage(Transform root, string objectName, bool requireFilled)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.name == objectName && (!requireFilled || image.type == Image.Type.Filled))
                return image;
        }

        return null;
    }

    private static TMP_Text FindNamedText(Transform root, string objectName)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == objectName)
                return text;
        }

        return null;
    }

    private static Slider FindNamedSlider(Transform root, string objectName)
    {
        foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
        {
            if (slider.name == objectName)
                return slider;
        }

        return null;
    }
}