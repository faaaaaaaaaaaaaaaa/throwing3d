using UnityEngine;

public class PowerBarUI : MonoBehaviour
{
    [SerializeField] private Canvas _humanPowerBar;
    [SerializeField] private Canvas _zombiePowerBar;

    public void ShowHumanPowerBar(bool show)
    {
        if (_humanPowerBar) _humanPowerBar.enabled = show;
        else gameObject.SetActive(show); 
    } 
    public void ShowZombiePowerBar(bool show)
    {
        if (_zombiePowerBar) _zombiePowerBar.enabled = show;
        else gameObject.SetActive(show); 
    } 

}