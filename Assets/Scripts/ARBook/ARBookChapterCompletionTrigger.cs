using UnityEngine;

public class ARBookChapterCompletionTrigger : MonoBehaviour
{
    public int chapterId = 1;
    public string requiredCaptureId = "Pikachu";
    public string completeDialogue = "Chapter 1 is complete. Open Chapter 2.";
    public ARBookCollectionManager collectionManager;
    public ARBookChapterProgress chapterProgress;
    public bool onlyCompleteOnce = true;

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

        if (!collectionManager.IsCaptured(requiredCaptureId))
        {
            return;
        }

        chapterProgress.CompleteChapterWithMemoryFragment(chapterId, completeDialogue);
    }
}
