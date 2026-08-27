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

    [Header("Config")]
    public WindDirection windDirection;
    public float windForce; // 0–1
    [Range(0f, 1f)] public float maxForce = 1f; // per-level ceiling for random wind

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    public void SetWind(WindDirection dir, float force)
    {
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
        }
        else
        {
            if (fillLeft) fillLeft.SetActive(true);
            if (arrowLeft) arrowLeft.SetActive(true);
            if (fillImageLeft) fillImageLeft.fillAmount = windForce;
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
}