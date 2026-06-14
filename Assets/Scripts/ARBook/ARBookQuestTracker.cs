using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARBookQuestTracker : MonoBehaviour
{
    public enum QuestStep
    {
        TalkToMentor,
        CollectFragments,
        TalkToCreature,
        CaptureCreature,
        ReachChapterEnd,
        Completed
    }

    [Header("Quest")]
    public int chapterId = 1;
    public string questTitle = "第一章：森林初遇";
    public ARBookInteractable mentor;
    public ARBookInteractable creature;
    public string requiredCaptureId = "Pikachu";
    public ARBookChapterObjectiveManager objectiveManager;
    public ARBookCollectionManager collectionManager;
    public ARBookChapterProgress chapterProgress;

    [Header("Quest Markers")]
    public bool installQuestMarkersAutomatically = true;
    public Vector3 markerLocalOffset = new Vector3(0f, 1.5f, 0f);
    public TMP_FontAsset markerFont;

    [Header("Step Text")]
    public string talkToMentorText = "与导师交谈";
    public string collectFragmentsText = "收集闪电碎片";
    public string talkToCreatureText = "与 Pikachu 交谈";
    public string captureCreatureText = "收服 Pikachu";
    public string reachChapterEndText = "前往章节终点";
    public string completedText = "章节任务完成";

    [Header("UI")]
    public TMP_Text questTMPText;
    public Text questText;
    public string currentPrefix = "[当前] ";
    public string completedPrefix = "[完成] ";
    public string pendingPrefix = "  ";
    public bool showPendingSteps = true;

    public QuestStep CurrentStep { get; private set; }

    private void OnEnable()
    {
        ARBookInteractable.Interacted += HandleInteractableUsed;
    }

    private void OnDisable()
    {
        ARBookInteractable.Interacted -= HandleInteractableUsed;
    }

    private void Start()
    {
        ResolveReferences();
        InstallQuestMarkers();
        CurrentStep = LoadStep();
        CatchUpFromSavedGame();
        RefreshUI();
    }

    private void Update()
    {
        QuestStep previousStep = CurrentStep;
        CatchUpFromSavedGame();

        if (previousStep != CurrentStep ||
            CurrentStep == QuestStep.CollectFragments)
        {
            RefreshUI();
        }
    }

    public bool IsCurrentStep(QuestStep step)
    {
        return CurrentStep == step;
    }

    public void NotifyChapterEndReached()
    {
        if (CurrentStep == QuestStep.ReachChapterEnd)
        {
            SetStep(QuestStep.Completed);
        }
    }

    public void ClearQuestProgress()
    {
        PlayerPrefs.DeleteKey(GetStepKey());
        PlayerPrefs.Save();
        CurrentStep = QuestStep.TalkToMentor;
        RefreshUI();
    }

    public void RefreshUI()
    {
        string value = BuildQuestText();

        if (questTMPText != null)
        {
            questTMPText.text = value;
        }

        if (questText != null)
        {
            questText.text = value;
        }
    }

    private void HandleInteractableUsed(ARBookInteractable interactable)
    {
        if (CurrentStep == QuestStep.TalkToMentor && interactable == mentor)
        {
            SetStep(QuestStep.CollectFragments);
            return;
        }

        if (CurrentStep == QuestStep.TalkToCreature && interactable == creature)
        {
            SetStep(QuestStep.CaptureCreature);
        }
    }

    private void CatchUpFromSavedGame()
    {
        ResolveReferences();

        if (CurrentStep == QuestStep.CollectFragments &&
            objectiveManager != null &&
            objectiveManager.IsObjectiveCompleted)
        {
            SetStep(QuestStep.TalkToCreature);
        }

        if ((CurrentStep == QuestStep.TalkToCreature ||
             CurrentStep == QuestStep.CaptureCreature) &&
            collectionManager != null &&
            collectionManager.IsCaptured(requiredCaptureId))
        {
            SetStep(QuestStep.ReachChapterEnd);
        }

        if (CurrentStep == QuestStep.ReachChapterEnd &&
            chapterProgress != null &&
            chapterProgress.IsChapterCompleted(chapterId))
        {
            SetStep(QuestStep.Completed);
        }
    }

    private void SetStep(QuestStep step)
    {
        if (step <= CurrentStep)
        {
            return;
        }

        CurrentStep = step;
        PlayerPrefs.SetInt(GetStepKey(), (int)CurrentStep);
        PlayerPrefs.Save();
        RefreshUI();
    }

    private QuestStep LoadStep()
    {
        int savedStep = PlayerPrefs.GetInt(GetStepKey(), (int)QuestStep.TalkToMentor);
        return (QuestStep)Mathf.Clamp(
            savedStep,
            (int)QuestStep.TalkToMentor,
            (int)QuestStep.Completed);
    }

    private string BuildQuestText()
    {
        string stepText;
        switch (CurrentStep)
        {
            case QuestStep.TalkToMentor:
                stepText = talkToMentorText;
                break;
            case QuestStep.CollectFragments:
                stepText = GetCollectFragmentsText();
                break;
            case QuestStep.TalkToCreature:
                stepText = talkToCreatureText;
                break;
            case QuestStep.CaptureCreature:
                stepText = captureCreatureText;
                break;
            case QuestStep.ReachChapterEnd:
                stepText = reachChapterEndText;
                break;
            default:
                stepText = completedText;
                break;
        }

        string prefix = CurrentStep == QuestStep.Completed ? completedPrefix : currentPrefix;
        return $"{questTitle}\n{prefix}{stepText}";
    }

    private string GetCollectFragmentsText()
    {
        if (objectiveManager == null)
        {
            return collectFragmentsText;
        }

        int count = Mathf.Min(
            objectiveManager.CollectedCount,
            objectiveManager.requiredCollectibleCount);
        return $"{collectFragmentsText} ({count} / {objectiveManager.requiredCollectibleCount})";
    }

    private void ResolveReferences()
    {
        if (objectiveManager == null)
        {
            objectiveManager = GetComponentInChildren<ARBookChapterObjectiveManager>(true);
        }

        if (collectionManager == null)
        {
            collectionManager = FindObjectOfType<ARBookCollectionManager>();
        }

        if (chapterProgress == null)
        {
            chapterProgress = FindObjectOfType<ARBookChapterProgress>();
        }
    }

    private void InstallQuestMarkers()
    {
        if (!installQuestMarkersAutomatically)
        {
            return;
        }

        InstallQuestMarker(mentor, QuestStep.TalkToMentor);
        InstallQuestMarker(creature, QuestStep.TalkToCreature);
    }

    private void InstallQuestMarker(ARBookInteractable interactable, QuestStep visibleStep)
    {
        if (interactable == null)
        {
            return;
        }

        ARBookQuestMarker marker = interactable.GetComponent<ARBookQuestMarker>();
        if (marker == null)
        {
            marker = interactable.gameObject.AddComponent<ARBookQuestMarker>();
        }

        marker.questTracker = this;
        marker.visibleStep = visibleStep;
        marker.localOffset = markerLocalOffset;
        marker.fontAsset = markerFont;
    }

    private string GetStepKey()
    {
        return $"QuestStep_{chapterId}";
    }
}
