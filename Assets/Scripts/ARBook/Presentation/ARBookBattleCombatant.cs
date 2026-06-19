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
    public bool audioEnabled = true;
    public AudioSource audioSource;
    public AudioClip attackClip;
    public AudioClip hitClip;
    public AudioClip defeatClip;
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
    }

    public void PlayAttack()
    {
        PlayClip(attackClip);
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
            PlayClip(defeatClip);
            actor?.PlayDefeat();
            onDefeated?.Invoke();
        }
        else
        {
            PlayClip(hitClip);
            actor?.PlayHit();
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
    }

    public void PlayVictory()
    {
        PlayClip(victoryClip);
        actor?.PlayVictory();
    }

    public void PlayCaptureSuccess()
    {
        PlayClip(captureSuccessClip);
        actor?.PlayCaptureSuccess();
    }

    private void PlayClip(AudioClip clip)
    {
        if (!audioEnabled || clip == null)
        {
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.spatialBlend = 0f;
        audioSource.PlayOneShot(clip);
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
