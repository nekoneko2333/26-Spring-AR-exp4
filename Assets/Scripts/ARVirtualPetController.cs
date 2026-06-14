using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ARVirtualPetController : MonoBehaviour
{
    [Header("Pet State")]
    [Range(0, 100)] public float mood = 70f;
    [Range(0, 100)] public float hunger = 35f;
    public bool isSleeping;
    public string petId = "DefaultPet";
    public bool saveState = true;
    public float offlineHungerIncreasePerHour = 8f;
    public float offlineMoodDecreasePerHour = 5f;

    [Header("State Change")]
    public float hungerIncreasePerSecond = 1.2f;
    public float moodDecreasePerSecond = 0.8f;
    public float sleepingMoodRecoveryPerSecond = 2.5f;

    [Header("Optional References")]
    public Animator animator;
    public ParticleSystem happyEffect;
    public ParticleSystem feedEffect;
    public AudioSource audioSource;
    public AudioClip feedClip;
    public AudioClip petClip;
    public AudioClip playClip;
    public AudioClip sleepClip;

    [Header("UI")]
    public TMP_Text statusText;
    public TMP_Text moodText;
    public TMP_Text hungerText;
    public Slider moodSlider;
    public Slider hungerSlider;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        LoadState();
        RefreshUI();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveState();
        }
    }

    private void OnApplicationQuit()
    {
        SaveState();
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        if (isSleeping)
        {
            mood = Mathf.Clamp(mood + sleepingMoodRecoveryPerSecond * delta, 0f, 100f);
            hunger = Mathf.Clamp(hunger + hungerIncreasePerSecond * 0.55f * delta, 0f, 100f);
        }
        else
        {
            hunger = Mathf.Clamp(hunger + hungerIncreasePerSecond * delta, 0f, 100f);

            float moodLoss = hunger > 70f ? moodDecreasePerSecond * 2.2f : moodDecreasePerSecond;
            mood = Mathf.Clamp(mood - moodLoss * delta, 0f, 100f);
        }

        RefreshUI();
    }

    public void Feed()
    {
        isSleeping = false;
        hunger = Mathf.Clamp(hunger - 28f, 0f, 100f);
        mood = Mathf.Clamp(mood + 10f, 0f, 100f);
        TriggerAnimation("Feed");
        PlayEffect(feedEffect);
        PlayClip(feedClip);
        SetStatus("吃饱啦");
        SaveState();
    }

    public void Pet()
    {
        isSleeping = false;
        mood = Mathf.Clamp(mood + 18f, 0f, 100f);
        TriggerAnimation("Happy");
        PlayEffect(happyEffect);
        PlayClip(petClip);
        SetStatus("被摸摸很开心");
        SaveState();
    }

    public void Play()
    {
        isSleeping = false;
        mood = Mathf.Clamp(mood + 24f, 0f, 100f);
        hunger = Mathf.Clamp(hunger + 14f, 0f, 100f);
        TriggerAnimation("Play");
        PlayEffect(happyEffect);
        PlayClip(playClip);
        SetStatus("正在玩耍");
        SaveState();
    }

    public void ToggleSleep()
    {
        isSleeping = !isSleeping;
        TriggerAnimation(isSleeping ? "Sleep" : "Wake");
        PlayClip(sleepClip);
        SetStatus(isSleeping ? "睡觉恢复心情" : "醒来了");
        SaveState();
    }

    public void WakeUp()
    {
        if (!isSleeping)
        {
            return;
        }

        isSleeping = false;
        TriggerAnimation("Wake");
        SetStatus("醒来了");
        SaveState();
    }

    private void OnMouseDown()
    {
        Pet();
    }

    private void RefreshUI()
    {
        if (moodSlider != null)
        {
            moodSlider.value = mood / 100f;
        }

        if (hungerSlider != null)
        {
            hungerSlider.value = hunger / 100f;
        }

        if (moodText != null)
        {
            moodText.text = $"心情 {Mathf.RoundToInt(mood)}";
        }

        if (hungerText != null)
        {
            hungerText.text = $"饥饿 {Mathf.RoundToInt(hunger)}";
        }

        if (statusText != null && string.IsNullOrWhiteSpace(statusText.text))
        {
            statusText.text = GetDefaultStatus();
        }
    }

    private string GetDefaultStatus()
    {
        if (isSleeping)
        {
            return "睡觉中";
        }

        if (hunger > 78f)
        {
            return "有点饿了";
        }

        if (mood > 75f)
        {
            return "状态很好";
        }

        if (mood < 35f)
        {
            return "想要陪伴";
        }

        return "等待互动";
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void TriggerAnimation(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }

    private void PlayEffect(ParticleSystem effect)
    {
        if (effect != null)
        {
            effect.Play();
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void SaveState()
    {
        if (!saveState)
        {
            return;
        }

        PlayerPrefs.SetFloat(GetKey("Mood"), mood);
        PlayerPrefs.SetFloat(GetKey("Hunger"), hunger);
        PlayerPrefs.SetInt(GetKey("Sleeping"), isSleeping ? 1 : 0);
        PlayerPrefs.SetString(GetKey("LastSaveUtc"), DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        if (!saveState)
        {
            return;
        }

        mood = PlayerPrefs.GetFloat(GetKey("Mood"), mood);
        hunger = PlayerPrefs.GetFloat(GetKey("Hunger"), hunger);
        isSleeping = PlayerPrefs.GetInt(GetKey("Sleeping"), isSleeping ? 1 : 0) == 1;

        string ticksText = PlayerPrefs.GetString(GetKey("LastSaveUtc"), string.Empty);
        if (long.TryParse(ticksText, out long ticks))
        {
            TimeSpan elapsed = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);
            float elapsedHours = Mathf.Max(0f, (float)elapsed.TotalHours);
            hunger = Mathf.Clamp(hunger + offlineHungerIncreasePerHour * elapsedHours, 0f, 100f);
            mood = Mathf.Clamp(mood - offlineMoodDecreasePerHour * elapsedHours, 0f, 100f);
        }
    }

    private string GetKey(string suffix)
    {
        return $"Pet_{petId}_{suffix}";
    }
}
