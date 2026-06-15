using TMPro;
using UnityEngine;

public class ARBookChapterHUDController : MonoBehaviour
{
    public TMP_Text questText;
    public TMP_Text chapterProgressText;
    public TMP_Text challengeText;
    public GameObject[] chapterRoots = new GameObject[5];
    public ARBookChallenge chapter02Challenge;
    public ARBookChallenge chapter03Challenge;
    public ARBookChallenge chapter04Challenge;

    private int activeChapterId;
    private ARBookQuestTracker chapter01QuestTracker;
    private ARBookChapterProgress chapterProgress;

    private void Start()
    {
        ResolveReferences();
        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void Refresh(bool force)
    {
        ResolveReferences();
        int detectedChapterId = DetectActiveChapter();

        if (force || detectedChapterId != activeChapterId)
        {
            activeChapterId = detectedChapterId;
            bool hasChapter = activeChapterId > 0;
            SetActive(questText, hasChapter);
            SetActive(chapterProgressText, hasChapter);
            SetActive(
                challengeText,
                activeChapterId >= 2 && activeChapterId <= 4);

            if (challengeText != null &&
                (activeChapterId < 2 || activeChapterId > 4))
            {
                challengeText.text = string.Empty;
            }
        }

        if (activeChapterId > 0)
        {
            RefreshText(activeChapterId);
        }
    }

    private void RefreshText(int chapterId)
    {
        if (questText != null)
        {
            questText.text = BuildQuestText(chapterId);
        }

        if (chapterProgressText == null)
        {
            return;
        }

        int completedCount = 0;
        if (chapterProgress != null)
        {
            for (int i = 1; i <= 5; i++)
            {
                if (chapterProgress.IsChapterCompleted(i))
                {
                    completedCount++;
                }
            }
        }

        bool currentCompleted = chapterProgress != null &&
                                chapterProgress.IsChapterCompleted(chapterId);
        chapterProgressText.text =
            $"章节进度：第 {chapterId} 章 / 5\n" +
            $"当前：{(currentCompleted ? "已完成" : "进行中")}　总完成：{completedCount} / 5";
    }

    private string BuildQuestText(int chapterId)
    {
        switch (chapterId)
        {
            case 1:
                if (chapter01QuestTracker != null)
                {
                    chapter01QuestTracker.RefreshUI();
                    if (chapter01QuestTracker.questTMPText != null)
                    {
                        return chapter01QuestTracker.questTMPText.text;
                    }
                }

                return "第一章：森林初遇\n[当前] 收集闪电碎片并收服 Pikachu";
            case 2:
                return BuildChallengeText(
                    "第二章：视角拼图",
                    "移动手机，从正确角度拼合图案",
                    chapter02Challenge);
            case 3:
                return BuildChallengeText(
                    "第三章：火山机关",
                    "按正确顺序解除火山封印",
                    chapter03Challenge);
            case 4:
                return BuildChallengeText(
                    "第四章：湖泊机关",
                    "解开湖边机关，让精灵来到岸边",
                    chapter04Challenge);
            case 5:
                return "第五章：遗迹\n[当前] 调查遗迹并寻找最终机关";
            default:
                return string.Empty;
        }
    }

    private static string BuildChallengeText(
        string title,
        string pendingText,
        ARBookChallenge challenge)
    {
        return challenge != null && challenge.IsCompleted
            ? $"{title}\n[完成] 机关已经解开"
            : $"{title}\n[当前] {pendingText}";
    }

    private int DetectActiveChapter()
    {
        for (int i = 0; i < chapterRoots.Length; i++)
        {
            if (chapterRoots[i] != null && chapterRoots[i].activeInHierarchy)
            {
                return i + 1;
            }
        }

        return 0;
    }

    private void ResolveReferences()
    {
        if (chapterProgress == null)
        {
            chapterProgress = FindObjectOfType<ARBookChapterProgress>();
        }

        if (chapterRoots.Length > 0 && chapterRoots[0] != null &&
            chapter01QuestTracker == null)
        {
            chapter01QuestTracker =
                chapterRoots[0].GetComponent<ARBookQuestTracker>();
        }
    }

    private static void SetActive(TMP_Text text, bool active)
    {
        if (text != null && text.gameObject.activeSelf != active)
        {
            text.gameObject.SetActive(active);
        }
    }
}
