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

        // hide all
        fillLeft.SetActive(false); fillRight.SetActive(false);
        arrowLeft.SetActive(false); arrowRight.SetActive(false);

        if (dir == WindDirection.Right)
        {
            fillRight.SetActive(true);
            arrowRight.SetActive(true);
            fillImageRight.fillAmount = windForce;
        }
        else
        {
            fillLeft.SetActive(true);
            arrowLeft.SetActive(true);
            fillImageLeft.fillAmount = windForce;
        }

        if (textWind) textWind.text = "WIND";
    }

    // random wind
    public void RandomWind()
    {
        var dir = (UnityEngine.Random.value > 0.5f) ? WindDirection.Right : WindDirection.Left;
        var force = UnityEngine.Random.Range(0f, 1f);
        SetWind(dir, force);
    }
}