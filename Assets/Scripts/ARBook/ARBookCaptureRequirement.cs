using UnityEngine;

public class ARBookCaptureRequirement : MonoBehaviour
{
    public ARBookChapterObjectiveManager objectiveManager;
    public ARBookChallenge requiredChallenge;
    public bool autoResolveRequiredChallenge;
    [Min(1)] public int requiredCollectibleCount = 3;
    public string lockedSpeaker = "Pikachu";
    [TextArea(2, 4)] public string lockedDialogue = "需要先找齐三个闪电碎片。";
    [TextArea(2, 4)] public string challengeLockedDialogue = "需要先完成精灵挑战。";

    public bool IsSatisfied()
    {
        ResolveObjectiveManager();
        ResolveChallenge();

        bool collectiblesSatisfied =
            objectiveManager == null ||
            objectiveManager.HasRequiredCollectibles(requiredCollectibleCount);
        bool challengeSatisfied =
            requiredChallenge == null ||
            requiredChallenge.IsCompleted;

        return collectiblesSatisfied && challengeSatisfied;
    }

    public string GetLockedDialogue()
    {
        ResolveObjectiveManager();
        ResolveChallenge();

        if (objectiveManager != null &&
            !objectiveManager.HasRequiredCollectibles(requiredCollectibleCount))
        {
            return $"{lockedDialogue}\n" +
                   $"{objectiveManager.CollectedCount} / {requiredCollectibleCount}";
        }

        if (requiredChallenge != null && !requiredChallenge.IsCompleted)
        {
            return challengeLockedDialogue;
        }

        return lockedDialogue;
    }

    private void ResolveObjectiveManager()
    {
        if (objectiveManager != null)
        {
            return;
        }

        objectiveManager = GetComponentInParent<ARBookChapterObjectiveManager>(true);
    }

    private void ResolveChallenge()
    {
        if (requiredChallenge != null)
        {
            return;
        }

        if (!autoResolveRequiredChallenge)
        {
            return;
        }

        requiredChallenge = GetComponentInParent<ARBookChallenge>(true);
    }
}
