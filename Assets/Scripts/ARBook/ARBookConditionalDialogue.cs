using UnityEngine;
using UnityEngine.Events;

public class ARBookConditionalDialogue : MonoBehaviour
{
    public ARBookConditionGroup conditions = new ARBookConditionGroup();
    public ARBookCollectionManager collectionManager;
    public ARBookChapterProgress chapterProgress;
    public DialogueManager dialogueManager;

    [Header("Dialogue")]
    public string speakerName = "Chapter Objective";
    [TextArea(2, 4)] public string successDialogue = "The mechanism responds.";
    [TextArea(2, 4)] public string missingDialogue = "Nothing happens yet.";

    [Header("Events")]
    public UnityEvent onConditionsMet;
    public UnityEvent onConditionsNotMet;

    [ContextMenu("Try Show Dialogue")]
    public void TryShowDialogue()
    {
        ResolveReferences();
        bool met = conditions == null ||
                   conditions.IsMet(collectionManager, chapterProgress);

        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogue(
                speakerName,
                met ? successDialogue : missingDialogue);
        }

        if (met)
        {
            onConditionsMet?.Invoke();
        }
        else
        {
            onConditionsNotMet?.Invoke();
        }
    }

    [ContextMenu("Log Conditions")]
    public void LogConditions()
    {
        ResolveReferences();
        string debugText = conditions == null
            ? "No condition group."
            : conditions.GetDebugText(collectionManager, chapterProgress);
        Debug.Log($"{name}\n{debugText}");
    }

    private void ResolveReferences()
    {
        if (collectionManager == null)
        {
            collectionManager = FindObjectOfType<ARBookCollectionManager>();
        }

        if (chapterProgress == null)
        {
            chapterProgress = FindObjectOfType<ARBookChapterProgress>();
        }

        if (dialogueManager == null)
        {
            dialogueManager = DialogueManager.Instance != null
                ? DialogueManager.Instance
                : FindObjectOfType<DialogueManager>();
        }
    }
}
