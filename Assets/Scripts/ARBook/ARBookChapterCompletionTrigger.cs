using UnityEngine;

public class ARBookChapterCompletionTrigger : MonoBehaviour
{
    public int chapterId = 1;
    public string requiredCaptureId = "Pikachu";
    public string completeDialogue = "Chapter 1 is complete. Open Chapter 2.";
    public ARBookCollectionManager collectionManager;
    public ARBookChapterProgress chapterProgress;
    public GameObject transitionEffectRoot;
    public ParticleSystem transitionEffect;
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

        PlayTransitionEffect();
        chapterProgress.CompleteChapterWithMemoryFragment(chapterId, completeDialogue);
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
