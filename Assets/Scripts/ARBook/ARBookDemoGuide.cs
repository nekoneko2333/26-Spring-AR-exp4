using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARBookDemoGuide : MonoBehaviour
{
    public ARBookCollectionManager collectionManager;
    public ARBookChapterProgress chapterProgress;
    public TMP_Text guideTMPText;
    public Text guideText;

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

        Refresh();
    }

    public void Refresh()
    {
        string text =
            "演示目标\n" +
            "1. 第一章：收集碎片并收服 Pikachu\n" +
            "2. 第二章：移动手机完成视角对齐\n" +
            "3. 第三、四章：收服主线精灵\n" +
            "4. 第五章：修复最终裂隙\n\n" +
            BuildProgressText();

        if (guideTMPText != null)
        {
            guideTMPText.text = text;
        }

        if (guideText != null)
        {
            guideText.text = text;
        }
    }

    private string BuildProgressText()
    {
        if (chapterProgress == null)
        {
            return "章节进度：未绑定";
        }

        return
            $"章节进度：1[{Done(1)}] 2[{Done(2)}] 3[{Done(3)}] 4[{Done(4)}] 5[{Done(5)}]";
    }

    private string Done(int chapterId)
    {
        return chapterProgress.IsChapterCompleted(chapterId) ? "完成" : "未完成";
    }
}
