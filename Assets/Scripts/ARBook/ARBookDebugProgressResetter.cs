using UnityEngine;

public class ARBookDebugProgressResetter : MonoBehaviour
{
    public bool clearOnAwake = true;
    public bool logClearedKeys = true;
    public int maxChapterId = 5;
    public string[] captureIds =
    {
        "Pikachu",
        "Celebi",
        "Infernape",
        "Manaphy",
        "Zekrom"
    };
    public string[] collectibleIds =
    {
        "Fragment_01",
        "Fragment_02",
        "Fragment_03"
    };
    public string[] challengeIds =
    {
        "PikachuSequence",
        "CelebiViewAlignment",
        "VolcanoSeal",
        "LakePath"
    };

    private void Awake()
    {
        if (clearOnAwake)
        {
            ClearARBookProgress();
        }
    }

    [ContextMenu("Clear AR Book Progress")]
    public void ClearARBookProgress()
    {
        for (int chapterId = 1; chapterId <= maxChapterId; chapterId++)
        {
            DeleteKey($"ChapterCompleted_{chapterId}");
            DeleteKey($"MemoryFragment_{chapterId}");
            DeleteKey($"QuestStep_{chapterId}");
            DeleteKey($"ChapterObjectiveCount_{chapterId}");

            for (int i = 0; i < collectibleIds.Length; i++)
            {
                DeleteKey($"ChapterCollectible_{chapterId}_{collectibleIds[i]}");
            }

            for (int i = 0; i < challengeIds.Length; i++)
            {
                DeleteKey($"ChallengeCompleted_{chapterId}_{challengeIds[i]}");
            }
        }

        for (int i = 0; i < captureIds.Length; i++)
        {
            DeleteKey($"Captured_{captureIds[i]}");
        }

        DeleteKey("CapturedIds");
        DeleteKey("FinaleCompleted");
        DeleteKey(ARBookPlayerPower.AttackBonusKey);
        PlayerPrefs.Save();

        if (logClearedKeys)
        {
            Debug.Log("ARBook debug progress cleared.");
        }
    }

    private void DeleteKey(string key)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            return;
        }

        PlayerPrefs.DeleteKey(key);

        if (logClearedKeys)
        {
            Debug.Log($"Deleted PlayerPrefs key: {key}");
        }
    }
}
