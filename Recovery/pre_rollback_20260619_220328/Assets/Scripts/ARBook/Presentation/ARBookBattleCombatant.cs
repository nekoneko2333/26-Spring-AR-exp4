using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ARBookBattleCombatant : MonoBehaviour
{
    public string displayName = "战斗单位";
    [Min(1)] public int maxHP = 100;
    [Min(1)] public int attackPower = 20;
    public ARBookPresentationActor actor;
    public Slider hpSlider;
    public TMP_Text hpText;
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip entryClip;
    public AudioClip attackClip;
    public AudioClip hitClip;
    public AudioClip defeatClip;
    public AudioClip healClip;
    public AudioClip victoryClip;
    public AudioClip captureSuccessClip;
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
        PlayClip(entryClip);
    }

    public void PlayAttack()
    {
        actor?.PlayAttack();
        PlayClip(attackClip);
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
            PlayClip(defeatClip != null ? defeatClip : hitClip);
            onDefeated?.Invoke();
        }
        else
        {
            actor?.PlayHit();
            PlayClip(hitClip);
        }
    }

    public void Heal(int amount)
    {
        if (IsDefeated)
        {
            return;
        }

        CurrentHP = Mathf.Clamp(CurrentHP + Mathf.Max(0, amount), 0, maxHP);
        RefreshUI();
        actor?.PlayVictory();
        PlayClip(healClip);
    }

    public void PlayVictory()
    {
        actor?.PlayVictory();
        PlayClip(victoryClip);
    }

    public void PlayCaptureSuccess()
    {
        actor?.PlayCaptureSuccess();
        PlayClip(captureSuccessClip);
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

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
