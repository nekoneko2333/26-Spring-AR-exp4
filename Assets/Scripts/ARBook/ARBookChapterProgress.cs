using UnityEngine;

public class ARBookChapterProgress : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public string chapter01CompleteDialogue = "Chapter 1 is complete. Open Chapter 2.";

    private const string ChapterCompletedKeyPrefix = "ChapterCompleted_";
    private const string MemoryFragmentKeyPrefix = "MemoryFragment_";

    private void Start()
    {
        if (dialogueManager == null)
        {
            dialogueManager = DialogueManager.Instance != null
                ? DialogueManager.Instance
                : FindObjectOfType<DialogueManager>();
        }
    }

    public void CompleteChapter(int chapterId)
    {
        PlayerPrefs.SetInt(GetChapterCompletedKey(chapterId), 1);
        PlayerPrefs.Save();
    }

    public bool IsChapterCompleted(int chapterId)
    {
        return PlayerPrefs.GetInt(GetChapterCompletedKey(chapterId), 0) == 1;
    }

    public void SetMemoryFragmentCollected(int chapterId)
    {
        PlayerPrefs.SetInt(GetMemoryFragmentKey(chapterId), 1);
        PlayerPrefs.Save();
    }

    public bool HasMemoryFragment(int chapterId)
    {
        return PlayerPrefs.GetInt(GetMemoryFragmentKey(chapterId), 0) == 1;
    }

    public void CompleteChapter01AfterPikachuCaptured()
    {
        SetMemoryFragmentCollected(1);
        CompleteChapter(1);

        if (dialogueManager != null)
        {
            dialogueManager.QueueDialogue("Chapter Complete", chapter01CompleteDialogue);
        }
        else
        {
            Debug.LogWarning("ARBookChapterProgress dialogueManager is not assigned.");
        }
    }

    private string GetChapterCompletedKey(int chapterId)
    {
        return ChapterCompletedKeyPrefix + chapterId;
    }

    private string GetMemoryFragmentKey(int chapterId)
    {
        return MemoryFragmentKeyPrefix + chapterId;
    }
}
