using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class ARBookFinaleController : MonoBehaviour
{
    public int[] requiredChapterIds = { 1, 2, 3, 4 };
    public ARBookChapterProgress chapterProgress;
    public DialogueManager dialogueManager;
    public string speakerName = "裂隙核心";
    [TextArea(2, 4)] public string successDialogue =
        "四枚记忆碎片已经共鸣。裂隙被修复，Adventure Complete.";
    [TextArea(2, 4)] public string missingDialoguePrefix = "还缺少以下章节记忆：";
    public GameObject finaleEffectRoot;
    public ParticleSystem finaleEffect;
    public UnityEvent onFinaleCompleted;

    private const string FinaleCompletedKey = "FinaleCompleted";

    private void Start()
    {
        ResolveReferences();
    }

    public bool CanCompleteFinale()
    {
        ResolveReferences();

        if (chapterProgress == null)
        {
            return false;
        }

        for (int i = 0; i < requiredChapterIds.Length; i++)
        {
            if (!chapterProgress.IsChapterCompleted(requiredChapterIds[i]))
            {
                return false;
            }
        }

        return true;
    }

    public void TryCompleteFinale()
    {
        ResolveReferences();

        if (!CanCompleteFinale())
        {
            ShowMissingChapters();
            return;
        }

        PlayerPrefs.SetInt(FinaleCompletedKey, 1);
        PlayerPrefs.Save();
        PlayFinaleEffect();
        onFinaleCompleted?.Invoke();

        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogue(speakerName, successDialogue);
        }
    }

    public bool IsFinaleCompleted()
    {
        return PlayerPrefs.GetInt(FinaleCompletedKey, 0) == 1;
    }

    private void ShowMissingChapters()
    {
        if (dialogueManager == null || chapterProgress == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(missingDialoguePrefix);

        for (int i = 0; i < requiredChapterIds.Length; i++)
        {
            int chapterId = requiredChapterIds[i];
            if (!chapterProgress.IsChapterCompleted(chapterId))
            {
                builder.AppendLine($"Chapter {chapterId}");
            }
        }

        dialogueManager.ShowDialogue(speakerName, builder.ToString().TrimEnd());
    }

    private void PlayFinaleEffect()
    {
        if (finaleEffectRoot != null)
        {
            finaleEffectRoot.SetActive(true);
            ParticleSystem[] particleSystems =
                finaleEffectRoot.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Play();
            }
            return;
        }

        if (finaleEffect != null)
        {
            finaleEffect.gameObject.SetActive(true);
            finaleEffect.Play();
        }
    }

    private void ResolveReferences()
    {
        if (chapterProgress == null)
        {
            chapterProgress = FindObjectOfType<ARBookChapterProgress>();
        }

        if (dialogueManager == null)
        {
            dialogueManager = DialogueManager.Instance != null
                ? DialogueManager.Instance
                : FindObjectOfType<DialogueManager>();
        }
    }
}
