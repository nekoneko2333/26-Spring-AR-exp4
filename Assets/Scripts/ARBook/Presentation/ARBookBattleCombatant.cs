using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ARBookBattleCombatant : MonoBehaviour
{
    public string displayName = "Combatant";
    [Min(1)] public int maxHP = 100;
    [Min(1)] public int attackPower = 20;
    public ARBookPresentationActor actor;
    public Slider hpSlider;
    public TMP_Text hpText;
    public UnityEvent onDefeated;

    public int CurrentHP { get; private set; }
    public bool IsDefeated => CurrentHP <= 0;

    private void Awake()
    {
        ResetCombatant();
    }

    public void ResetCombatant()
    {
        CurrentHP = Mathf.Max(1, maxHP);
        actor?.PlayIdle();
        RefreshUI();
    }

    public void PlayEntry()
    {
        actor?.PlayEntry();
    }

    public void PlayAttack()
    {
        actor?.PlayAttack();
    }

    public void TakeDamage(int damage)
    {
        if (IsDefeated)
        {
            return;
        }

        CurrentHP = Mathf.Max(0, CurrentHP - Mathf.Max(0, damage));
        RefreshUI();

        if (IsDefeated)
        {
            actor?.PlayDefeat();
            onDefeated?.Invoke();
        }
        else
        {
            actor?.PlayHit();
        }
    }

    public void PlayVictory()
    {
        actor?.PlayVictory();
    }

    public void PlayCaptureSuccess()
    {
        actor?.PlayCaptureSuccess();
    }

    private void RefreshUI()
    {
        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = Mathf.Max(1, maxHP);
            hpSlider.value = CurrentHP;
        }

        if (hpText != null)
        {
            hpText.text = $"{displayName}  {CurrentHP} / {maxHP}";
        }
    }
}
