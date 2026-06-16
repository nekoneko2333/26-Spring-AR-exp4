using UnityEngine;

public class ARBookBattleStats : MonoBehaviour
{
    [Header("Enemy Stats")]
    [Min(1)] public int enemyMaxHP = 120;
    [Min(1)] public int enemyAttackPower = 18;

    [Header("Optional Hint")]
    [TextArea(2, 4)] public string lowPowerHint =
        "现在的攻击力还不够。先去收集地图上的能量道具，再回来挑战。";

    public void ApplyToEnemy(ARBookBattleCombatant enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.maxHP = enemyMaxHP;
        enemy.attackPower = enemyAttackPower;
    }
}
