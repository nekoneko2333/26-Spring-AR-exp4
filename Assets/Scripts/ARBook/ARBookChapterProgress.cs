using UnityEngine;

public class ARBookChapterProgress : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public string chapter01CompleteDialogue = "地图 1 探索完成。可以继续翻开任意地图。";

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

    public void ClearChapterProgress(int chapterId)
    {
        PlayerPrefs.DeleteKey(GetChapterCompletedKey(chapterId));
        PlayerPrefs.DeleteKey(GetMemoryFragmentKey(chapterId));
        PlayerPrefs.Save();
    }

    public void CompleteChapterWithMemoryFragment(int chapterId, string completeDialogue)
    {
        SetMemoryFragmentCollected(chapterId);
        CompleteChapter(chapterId);
    }

    public void CompleteChapter01AfterPikachuCaptured()
    {
        CompleteChapterWithMemoryFragment(1, chapter01CompleteDialogue);
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
