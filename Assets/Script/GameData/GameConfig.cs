using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "GameData/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("HP / Enemy Miss")]
    public int playerHP = 50;
    public int enemyHPEasy = 50;
    public int enemyHPMedium = 60;
    public int enemyHPHard = 70;

    public int missedChanceEasy = 50;
    public int missedChanceNormal = 30;
    public int missedChanceHard = 15;

    [Header("Damage")]
    public int normalAttack = 8;   // head
    public int smallAttack = 5;    // body
    public int powerThrow = 10;
    public int doubleAttackAmount = 2;
    public int doubleAttackDamage = 5;

    [Header("Heal / Timing")]
    public int healHP = 20;
    public int timeToThink = 30;
    public int timeToWarning = 10;
}
