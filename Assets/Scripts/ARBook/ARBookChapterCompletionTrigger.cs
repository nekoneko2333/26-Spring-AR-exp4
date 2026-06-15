using UnityEngine;

public class ARBookChapterCompletionTrigger : MonoBehaviour
{
    public int chapterId = 1;
    public string requiredCaptureId = "Pikachu";
    public bool requireCompletedChapters;
    public int[] requiredCompletedChapterIds;
    public ARBookConditionGroup extraConditions = new ARBookConditionGroup();
    public string completeDialogue = "地图探索完成。可以继续翻开任意地图。";
    public bool useIndependentMapCompleteDialogue = true;
    public string independentMapCompleteDialogueFormat =
        "地图 {0} 探索完成。可以继续翻开任意地图。";
    public ARBookCollectionManager collectionManager;
    public ARBookChapterProgress chapterProgress;
    public ARBookQuestTracker questTracker;
    public DialogueManager dialogueManager;
    public GameObject transitionEffectRoot;
    public ParticleSystem transitionEffect;
    public bool onlyCompleteOnce = true;
    public bool showMissingRequirementDialogue = true;
    public string missingCaptureDialogueFormat = "还需要先收服 {0}。";

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

        if (!string.IsNullOrWhiteSpace(requiredCaptureId) &&
            !collectionManager.IsCaptured(requiredCaptureId))
        {
            if (showMissingRequirementDialogue && dialogueManager != null)
            {
                dialogueManager.ShowDialogue(
                    "章节目标",
                    string.Format(missingCaptureDialogueFormat, requiredCaptureId));
            }
            return;
        }

        if (extraConditions != null &&
            !extraConditions.IsMet(collectionManager, chapterProgress))
        {
            if (showMissingRequirementDialogue && dialogueManager != null)
            {
                dialogueManager.ShowDialogue(
                    "Chapter Objective",
                    "Some chapter conditions are not complete yet.");
            }
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

        if (requiredCompletedChapterIds == null || requiredCompletedChapterIds.Length == 0)
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
                if (showMissingRequirementDialogue && dialogueManager != null)
                {
                    dialogueManager.ShowDialogue(
                        "章节目标",
                        $"还需要先完成 Chapter {requiredChapterId}。");
                }

                return false;
            }
        }

        return true;
    }

    private void PlayTransitionEffect()
    {
        if (transitionEffectRoot != null)
        {
            transitionEffectRoot.SetActive(true);

            ParticleSystem[] particleSystems = transitionEffectRoot.GetComponentsInChildren<ParticleSystem>();
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
