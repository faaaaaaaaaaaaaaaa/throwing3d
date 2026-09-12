using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "GameData/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("HP / Enemy Miss")]
    public int playerHP = 28;
    public int enemyHPEasy = 28;
    public int enemyHPMedium = 32;
    public int enemyHPHard = 36;

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
    public int healHP = 10;
    public int timeToThink = 30;
    public int timeToWarning = 10;
}
