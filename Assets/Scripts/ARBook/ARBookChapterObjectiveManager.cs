using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ARBookChapterObjectiveManager : MonoBehaviour
{
    [Header("Objective")]
    public int chapterId = 1;
    public string objectiveTitle = "收集闪电碎片";
    [Min(1)] public int requiredCollectibleCount = 3;

    [Header("UI")]
    public TMP_Text objectiveTMPText;
    public Text objectiveText;
    public string progressFormat = "{0}: {1} / {2}";
    public string completedFormat = "{0}：已完成";

    [Header("Feedback")]
    public DialogueManager dialogueManager;
    public string completionSpeaker = "任务";
    [TextArea(2, 4)] public string completionDialogue =
        "闪电碎片已集齐，现在可以与 Pikachu 互动并尝试收服。";
    public UnityEvent onObjectiveCompleted;

    public int CollectedCount => PlayerPrefs.GetInt(GetCountKey(), 0);
    public bool IsObjectiveCompleted => CollectedCount >= requiredCollectibleCount;

    private bool completionHandledThisSession;

    private void Start()
    {
        ResolveDialogueManager();
        RefreshUI();
    }

    public bool Collect(string collectibleId)
    {
        if (string.IsNullOrWhiteSpace(collectibleId))
        {
            Debug.LogWarning($"{name} cannot collect an item with an empty collectibleId.");
            return false;
        }

        if (IsCollected(collectibleId))
        {
            return false;
        }

        PlayerPrefs.SetInt(GetCollectibleKey(collectibleId), 1);
        PlayerPrefs.SetInt(GetCountKey(), CollectedCount + 1);
        PlayerPrefs.Save();

        RefreshUI();

        if (IsObjectiveCompleted)
        {
            HandleObjectiveCompleted();
        }

        return true;
    }

    public bool IsCollected(string collectibleId)
    {
        if (string.IsNullOrWhiteSpace(collectibleId))
        {
            return false;
        }

        return PlayerPrefs.GetInt(GetCollectibleKey(collectibleId), 0) == 1;
    }

    public bool HasRequiredCollectibles(int requiredCount)
    {
        return CollectedCount >= Mathf.Max(0, requiredCount);
    }

    public string GetProgressText()
    {
        if (IsObjectiveCompleted)
        {
            return string.Format(completedFormat, objectiveTitle);
        }

        return string.Format(
            progressFormat,
            objectiveTitle,
            Mathf.Min(CollectedCount, requiredCollectibleCount),
            requiredCollectibleCount);
    }

    public void RefreshUI()
    {
        string text = GetProgressText();

        if (objectiveTMPText != null)
        {
            objectiveTMPText.text = text;
        }

        if (objectiveText != null)
        {
            objectiveText.text = text;
        }
    }

    public void ClearProgress()
    {
        ARBookCollectible[] collectibles = GetComponentsInChildren<ARBookCollectible>(true);
        for (int i = 0; i < collectibles.Length; i++)
        {
            if (collectibles[i] != null &&
                !string.IsNullOrWhiteSpace(collectibles[i].collectibleId))
            {
                PlayerPrefs.DeleteKey(GetCollectibleKey(collectibles[i].collectibleId));
            }
        }

        PlayerPrefs.DeleteKey(GetCountKey());
        PlayerPrefs.Save();
        completionHandledThisSession = false;
        RefreshUI();
    }

    private void HandleObjectiveCompleted()
    {
        if (completionHandledThisSession)
        {
            return;
        }

        completionHandledThisSession = true;
        onObjectiveCompleted?.Invoke();

        if (!string.IsNullOrWhiteSpace(completionDialogue))
        {
            ResolveDialogueManager();
            if (dialogueManager != null)
            {
                dialogueManager.ShowDialogue(completionSpeaker, completionDialogue);
            }
        }
    }

    private void ResolveDialogueManager()
    {
        if (dialogueManager != null)
        {
            return;
        }

        dialogueManager = DialogueManager.Instance != null
            ? DialogueManager.Instance
            : FindObjectOfType<DialogueManager>();
    }

    private string GetCountKey()
    {
        return $"ChapterObjectiveCount_{chapterId}";
    }

    private string GetCollectibleKey(string collectibleId)
    {
        return $"ChapterCollectible_{chapterId}_{collectibleId}";
    }
}
