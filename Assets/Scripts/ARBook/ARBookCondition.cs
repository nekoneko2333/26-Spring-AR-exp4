using System;
using UnityEngine;

[Serializable]
public class ARBookCondition
{
    public enum ConditionType
    {
        CapturedCreature,
        ChapterCompleted,
        ChallengeCompleted,
        PlayerPrefsIntAtLeast,
        PlayerPrefsKeyEqualsOne,
        FinaleCompleted
    }

    public ConditionType type = ConditionType.CapturedCreature;
    public string id;
    public int chapterId = 1;
    public int requiredValue = 1;
    public bool invert;

    public bool IsMet(ARBookCollectionManager collectionManager, ARBookChapterProgress chapterProgress)
    {
        bool met;
        switch (type)
        {
            case ConditionType.CapturedCreature:
                met = collectionManager != null &&
                      !string.IsNullOrWhiteSpace(id) &&
                      collectionManager.IsCaptured(id);
                break;

            case ConditionType.ChapterCompleted:
                met = chapterProgress != null &&
                      chapterProgress.IsChapterCompleted(chapterId);
                break;

            case ConditionType.ChallengeCompleted:
                met = PlayerPrefs.GetInt(GetChallengeKey(), 0) == 1;
                break;

            case ConditionType.PlayerPrefsIntAtLeast:
                met = !string.IsNullOrWhiteSpace(id) &&
                      PlayerPrefs.GetInt(id, 0) >= requiredValue;
                break;

            case ConditionType.PlayerPrefsKeyEqualsOne:
                met = !string.IsNullOrWhiteSpace(id) &&
                      PlayerPrefs.GetInt(id, 0) == 1;
                break;

            case ConditionType.FinaleCompleted:
                met = PlayerPrefs.GetInt("FinaleCompleted", 0) == 1;
                break;

            default:
                met = false;
                break;
        }

        return invert ? !met : met;
    }

    public string GetDebugText(ARBookCollectionManager collectionManager, ARBookChapterProgress chapterProgress)
    {
        return $"{type} id={id} chapter={chapterId} required={requiredValue} " +
               $"invert={invert} met={IsMet(collectionManager, chapterProgress)}";
    }

    private string GetChallengeKey()
    {
        return $"ChallengeCompleted_{chapterId}_{id}";
    }
}
