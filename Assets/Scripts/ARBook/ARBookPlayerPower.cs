using TMPro;
using UnityEngine;

public class ARBookPlayerPower : MonoBehaviour
{
    public static ARBookPlayerPower Instance { get; private set; }

    public const string AttackBonusKey = "PlayerAttackPowerBonus";

    [Header("Battle")]
    [Min(1)] public int baseAttackPower = 20;
    [Min(1)] public int playerMaxHP = 100;

    [Header("UI")]
    public TMP_Text powerText;

    public int AttackBonus => PlayerPrefs.GetInt(AttackBonusKey, 0);
    public int TotalAttackPower => Mathf.Max(1, baseAttackPower + AttackBonus);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("场景中存在多个 ARBookPlayerPower，将使用最新启用的实例。", this);
        }

        Instance = this;
        RefreshUI();
    }

    public static ARBookPlayerPower Resolve()
    {
        if (Instance != null)
        {
            return Instance;
        }

        ARBookPlayerPower existing = FindObjectOfType<ARBookPlayerPower>(true);
        if (existing != null)
        {
            return existing;
        }

        GameObject powerObject = new GameObject("ARBookPlayerPower");
        return powerObject.AddComponent<ARBookPlayerPower>();
    }

    public void GrantAttackPower(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        PlayerPrefs.SetInt(AttackBonusKey, AttackBonus + amount);
        PlayerPrefs.Save();
        RefreshUI();
    }

    public void ApplyToCombatant(ARBookBattleCombatant combatant)
    {
        if (combatant == null)
        {
            return;
        }

        combatant.maxHP = playerMaxHP;
        combatant.attackPower = TotalAttackPower;
    }

    public void ResetPower()
    {
        PlayerPrefs.DeleteKey(AttackBonusKey);
        PlayerPrefs.Save();
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (powerText != null)
        {
            powerText.text = GetPowerText();
        }
    }

    public string GetPowerText()
    {
        return $"攻击力：{TotalAttackPower}（强化 +{AttackBonus}）";
    }
}
