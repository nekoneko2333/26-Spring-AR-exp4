using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARSequenceTapChallenge : ARBookChallenge
{
    public string speakerName = "挑战";
    [TextArea(2, 4)] public string startDialogue = "按正确顺序触碰三个闪电碎片。";
    [TextArea(2, 4)] public string successDialogue = "闪电符号被点亮了。";
    [TextArea(2, 4)] public string failureDialogue = "顺序不对，闪电碎片重新暗了下去。";
    public DialogueManager dialogueManager;

    [Header("Progress UI")]
    public TMP_Text progressTMPText;
    public Text progressText;
    public string progressFormat = "闪电顺序：{0} / {1}";
    [Min(1)] public int requiredSteps = 3;

    [Header("Feedback")]
    public ParticleSystem successEffect;
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip failureClip;
    public bool vibrateOnSuccess = true;

    private int currentIndex;

    protected override void Start()
    {
        base.Start();
        ResolveDialogueManager();
        RefreshProgress();
    }

    public void BeginChallenge()
    {
        if (IsCompleted)
        {
            return;
        }

        ResolveDialogueManager();
        if (dialogueManager != null && !string.IsNullOrWhiteSpace(startDialogue))
        {
            dialogueManager.ShowDialogue(speakerName, startDialogue);
        }

        RefreshProgress();
    }

    public void TapStep(int stepIndex)
    {
        if (IsCompleted)
        {
            return;
        }

        if (stepIndex != currentIndex)
        {
            ResetSequence();
            return;
        }

        currentIndex++;
        RefreshProgress();

        if (currentIndex >= requiredSteps)
        {
            CompleteChallenge();
        }
    }

    public void ResetSequence()
    {
        currentIndex = 0;
        RefreshProgress();

        ResolveDialogueManager();
        if (dialogueManager != null && !string.IsNullOrWhiteSpace(failureDialogue))
        {
            dialogueManager.ShowDialogue(speakerName, failureDialogue);
        }

        if (audioSource != null && failureClip != null)
        {
            audioSource.PlayOneShot(failureClip);
        }
    }

    protected override void OnCompleted()
    {
        RefreshProgress();

        if (successEffect != null)
        {
            successEffect.Play();
        }

        if (audioSource != null && successClip != null)
        {
            audioSource.PlayOneShot(successClip);
        }

        if (vibrateOnSuccess)
        {
            Handheld.Vibrate();
        }

        ResolveDialogueManager();
        if (dialogueManager != null && !string.IsNullOrWhiteSpace(successDialogue))
        {
            dialogueManager.ShowDialogue(speakerName, successDialogue);
        }
    }

    private void RefreshProgress()
    {
        string text = IsCompleted
            ? "闪电顺序：已完成"
            : string.Format(progressFormat, currentIndex, requiredSteps);

        if (progressTMPText != null)
        {
            progressTMPText.text = text;
        }

        if (progressText != null)
        {
            progressText.text = text;
        }
    }

    private void ResolveDialogueManager()
    {
        if (dialogueManager != null)
        {
            return;
        }

        dialogueManager = DialogueManager.Instance != null
            ? DialogueManager.Instance
            : FindObjectOfType<DialogueManager>();
    }
}
