using System;
using System.Text;
using UnityEngine;

[Serializable]
public class ARBookConditionGroup
{
    public enum MatchMode
    {
        All,
        Any
    }

    public MatchMode matchMode = MatchMode.All;
    public ARBookCondition[] conditions;

    public bool IsMet(ARBookCollectionManager collectionManager, ARBookChapterProgress chapterProgress)
    {
        if (conditions == null || conditions.Length == 0)
        {
            return true;
        }

        if (matchMode == MatchMode.Any)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i] != null &&
                    conditions[i].IsMet(collectionManager, chapterProgress))
                {
                    return true;
                }
            }

            return false;
        }

        for (int i = 0; i < conditions.Length; i++)
        {
            if (conditions[i] == null ||
                !conditions[i].IsMet(collectionManager, chapterProgress))
            {
                return false;
            }
        }

        return true;
    }

    public string GetDebugText(ARBookCollectionManager collectionManager, ARBookChapterProgress chapterProgress)
    {
        if (conditions == null || conditions.Length == 0)
        {
            return "No conditions.";
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"MatchMode: {matchMode}");
        for (int i = 0; i < conditions.Length; i++)
        {
            builder.Append(i).Append(": ");
            builder.AppendLine(conditions[i] == null
                ? "null"
                : conditions[i].GetDebugText(collectionManager, chapterProgress));
        }

        return builder.ToString().TrimEnd();
    }
}
