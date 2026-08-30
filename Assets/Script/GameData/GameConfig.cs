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
    public int normalAttack = 6;   // head
    public int smallAttack = 4;    // body
    public int powerThrow = 8;
    public int doubleAttackAmount = 2;
    public int doubleAttackDamage = 3;

    [Header("Heal / Timing")]
    public int healHP = 20;
    public int timeToThink = 30;
    public int timeToWarning = 10;
}
