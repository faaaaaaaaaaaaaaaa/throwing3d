using UnityEngine;

public class PowerBarUI : MonoBehaviour
{
    [SerializeField] private GameObject _humanPowerBar;
    [SerializeField] private GameObject _zombiePowerBar;
    [SerializeField] private GameObject _humanTimeWarning;
    [SerializeField] private GameObject _zombieTimeWarning;

    public void ShowHumanPowerBar(bool show) => _humanPowerBar?.SetActive(show);
    public void ShowZombiePowerBar(bool show) => _zombiePowerBar?.SetActive(show);

    public void ShowHumanTimeWarning(bool show) => _humanTimeWarning?.SetActive(show);
    public void ShowZombieTimeWarning(bool show) => _zombieTimeWarning?.SetActive(show);

}