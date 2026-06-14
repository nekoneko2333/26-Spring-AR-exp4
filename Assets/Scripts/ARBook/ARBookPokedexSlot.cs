using UnityEngine;

public class ARBookPokedexSlot : MonoBehaviour
{
    public enum UnlockMode
    {
        CapturedCreature,
        PlayerPrefsKey,
        ChapterCompleted,
        AlwaysUnlocked
    }

    [Header("Unlock")]
    public UnlockMode unlockMode = UnlockMode.CapturedCreature;
    public string unlockId;
    public int chapterId = 1;

    [Header("Display")]
    public GameObject unlockedRoot;
    public GameObject lockedRoot;
    public bool keepSlotVisibleWhenLocked = true;
    public bool debugLogging;

    public bool IsUnlocked(ARBookCollectionManager collectionManager)
    {
        switch (unlockMode)
        {
            case UnlockMode.CapturedCreature:
                return collectionManager != null &&
                       !string.IsNullOrWhiteSpace(unlockId) &&
                       collectionManager.IsCaptured(unlockId);

            case UnlockMode.PlayerPrefsKey:
                return !string.IsNullOrWhiteSpace(unlockId) &&
                       PlayerPrefs.GetInt(unlockId, 0) == 1;

            case UnlockMode.ChapterCompleted:
                return PlayerPrefs.GetInt($"ChapterCompleted_{chapterId}", 0) == 1;

            case UnlockMode.AlwaysUnlocked:
                return true;

            default:
                return false;
        }
    }

    public void Refresh(ARBookCollectionManager collectionManager)
    {
        bool unlocked = IsUnlocked(collectionManager);

        if (debugLogging)
        {
            Debug.Log(
                $"{name} refresh unlocked={unlocked}, mode={unlockMode}, id={unlockId}, " +
                $"chapter={chapterId}, unlockedRoot={GetObjectName(unlockedRoot)}, " +
                $"lockedRoot={GetObjectName(lockedRoot)}");
        }

        if (unlockedRoot != null)
        {
            unlockedRoot.SetActive(unlocked);
        }

        if (lockedRoot != null)
        {
            lockedRoot.SetActive(!unlocked && keepSlotVisibleWhenLocked);
        }
    }

    public string GetDebugState(ARBookCollectionManager collectionManager)
    {
        bool unlocked = IsUnlocked(collectionManager);
        return $"{name}: unlocked={unlocked}, mode={unlockMode}, id={unlockId}, " +
               $"chapter={chapterId}, captured={GetCapturedState(collectionManager)}, " +
               $"chapterDone={PlayerPrefs.GetInt($"ChapterCompleted_{chapterId}", 0)}, " +
               $"unlockedRoot={GetObjectName(unlockedRoot)}, " +
               $"unlockedRootActive={(unlockedRoot != null && unlockedRoot.activeSelf)}";
    }

    [ContextMenu("Log Pokedex Slot State")]
    public void LogDebugState()
    {
        Debug.Log(GetDebugState(FindObjectOfType<ARBookCollectionManager>()));
    }

    private string GetCapturedState(ARBookCollectionManager collectionManager)
    {
        if (collectionManager == null || string.IsNullOrWhiteSpace(unlockId))
        {
            return "n/a";
        }

        return collectionManager.IsCaptured(unlockId).ToString();
    }

    private string GetObjectName(GameObject target)
    {
        return target == null ? "null" : target.name;
    }
}
