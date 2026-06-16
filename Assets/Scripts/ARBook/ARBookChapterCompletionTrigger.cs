using UnityEngine;

public class ARBookChapterCompletionTrigger : MonoBehaviour
{
    public int chapterId = 1;

    [Header("Capture Requirements")]
    public string requiredCaptureId = "Pikachu";
    public bool requireAllChapterCaptures;
    public string[] requiredChapterCaptureIds;
    public string missingCaptureDialogueFormat = "还需要先收服 {0}。";
    public string missingAllCapturesDialogueFormat = "这张地图还有未收服的精灵：{0}。";

    [Header("Chapter Requirements")]
    public bool requireCompletedChapters;
    public int[] requiredCompletedChapterIds;
    public ARBookConditionGroup extraConditions = new ARBookConditionGroup();

    [Header("Completion Dialogue")]
    public string completeDialogue = "地图探索完成。可以继续翻开任意地图。";
    public bool useIndependentMapCompleteDialogue = true;
    public string independentMapCompleteDialogueFormat =
        "地图 {0} 探索完成。可以继续翻开任意地图。";

    [Header("References")]
    public ARBookCollectionManager collectionManager;
    public ARBookChapterProgress chapterProgress;
    public ARBookQuestTracker questTracker;
    public DialogueManager dialogueManager;
    public GameObject transitionEffectRoot;
    public ParticleSystem transitionEffect;

    [Header("Options")]
    public bool onlyCompleteOnce = true;
    public bool showMissingRequirementDialogue = true;

    private void Start()
    {
        if (collectionManager == null)
        {
            collectionManager = FindObjectOfType<ARBookCollectionManager>();
        }

        if (chapterProgress == null)
        {
            chapterProgress = FindObjectOfType<ARBookChapterProgress>();
        }

        if (questTracker == null)
        {
            questTracker = FindObjectOfType<ARBookQuestTracker>();
        }

        if (dialogueManager == null)
        {
            dialogueManager = DialogueManager.Instance != null
                ? DialogueManager.Instance
                : FindObjectOfType<DialogueManager>();
        }
    }

    public void TryCompleteChapter()
    {
        if (chapterProgress == null)
        {
            Debug.LogWarning("ARBookChapterCompletionTrigger chapterProgress is not assigned.");
            return;
        }

        if (onlyCompleteOnce && chapterProgress.IsChapterCompleted(chapterId))
        {
            return;
        }

        if (collectionManager == null)
        {
            Debug.LogWarning("ARBookChapterCompletionTrigger collectionManager is not assigned.");
            return;
        }

        if (!HasRequiredChaptersCompleted())
        {
            return;
        }

        if (!HasRequiredCapturesCompleted())
        {
            return;
        }

        if (extraConditions != null &&
            !extraConditions.IsMet(collectionManager, chapterProgress))
        {
            ShowRequirementDialogue("章节目标", "还有章节条件没有完成。");
            return;
        }

        PlayTransitionEffect();
        chapterProgress.CompleteChapterWithMemoryFragment(
            chapterId,
            GetCompleteDialogue());

        if (questTracker != null && questTracker.chapterId == chapterId)
        {
            questTracker.NotifyChapterEndReached();
        }
    }

    private bool HasRequiredCapturesCompleted()
    {
        if (!string.IsNullOrWhiteSpace(requiredCaptureId) &&
            !collectionManager.IsCaptured(requiredCaptureId))
        {
            ShowRequirementDialogue(
                "章节目标",
                string.Format(missingCaptureDialogueFormat, requiredCaptureId));
            return false;
        }

        if (!requireAllChapterCaptures)
        {
            return true;
        }

        if (requiredChapterCaptureIds == null ||
            requiredChapterCaptureIds.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < requiredChapterCaptureIds.Length; i++)
        {
            string captureId = requiredChapterCaptureIds[i];
            if (string.IsNullOrWhiteSpace(captureId))
            {
                continue;
            }

            if (!collectionManager.IsCaptured(captureId))
            {
                ShowRequirementDialogue(
                    "章节目标",
                    string.Format(missingAllCapturesDialogueFormat, captureId));
                return false;
            }
        }

        return true;
    }

    private string GetCompleteDialogue()
    {
        if (useIndependentMapCompleteDialogue &&
            !string.IsNullOrWhiteSpace(independentMapCompleteDialogueFormat))
        {
            return string.Format(independentMapCompleteDialogueFormat, chapterId);
        }

        return completeDialogue;
    }

    private bool HasRequiredChaptersCompleted()
    {
        if (!requireCompletedChapters)
        {
            return true;
        }

        if (requiredCompletedChapterIds == null ||
            requiredCompletedChapterIds.Length == 0)
        {
            return true;
        }

        if (chapterProgress == null)
        {
            return false;
        }

        for (int i = 0; i < requiredCompletedChapterIds.Length; i++)
        {
            int requiredChapterId = requiredCompletedChapterIds[i];
            if (!chapterProgress.IsChapterCompleted(requiredChapterId))
            {
                ShowRequirementDialogue(
                    "章节目标",
                    $"还需要先完成地图 {requiredChapterId}。");
                return false;
            }
        }

        return true;
    }

    private void ShowRequirementDialogue(string speaker, string message)
    {
        if (showMissingRequirementDialogue && dialogueManager != null)
        {
            dialogueManager.ShowDialogue(speaker, message);
        }
    }

    private void PlayTransitionEffect()
    {
        if (transitionEffectRoot != null)
        {
            transitionEffectRoot.SetActive(true);

            ParticleSystem[] particleSystems =
                transitionEffectRoot.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Play();
            }

            return;
        }

        if (transitionEffect != null)
        {
            transitionEffect.gameObject.SetActive(true);
            transitionEffect.Play();
        }
    }
}
