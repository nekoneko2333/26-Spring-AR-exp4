using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public GameObject dialoguePanel;

    [Header("Legacy UI Text")]
    public Text speakerNameText;
    public Text dialogueText;

    [Header("TextMeshPro UI Text")]
    public TMP_Text speakerNameTMPText;
    public TMP_Text dialogueTMPText;

    public Button continueButton;

    private string currentSpeakerName;
    private string[] currentDialogueLines;
    private int currentDialogueIndex;
    private bool dialogueOpenByManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple DialogueManager instances found. The newest one will be used.");
        }

        Instance = this;
    }

    private void Start()
    {
        if (continueButton != null &&
            !HasPersistentClick(continueButton, this, nameof(ContinueDialogue)))
        {
            continueButton.onClick.AddListener(ContinueDialogue);
        }
        else
        {
            Debug.LogWarning("DialogueManager continueButton is not assigned.");
        }

        HideDialogue();
    }

    public void ShowDialogue(string speakerName, string text)
    {
        ShowDialogueSequence(speakerName, new[] { text });
    }

    public void ShowDialogueSequence(string speakerName, string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            ShowDialogueLine(speakerName, string.Empty);
            return;
        }

        currentSpeakerName = speakerName;
        currentDialogueLines = lines;
        currentDialogueIndex = 0;

        ShowDialogueLine(currentSpeakerName, currentDialogueLines[currentDialogueIndex]);
    }

    public void QueueDialogue(string speakerName, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (currentDialogueLines == null || currentDialogueLines.Length == 0 || dialoguePanel == null || !dialoguePanel.activeSelf)
        {
            ShowDialogue(speakerName, text);
            return;
        }

        string[] extendedLines = new string[currentDialogueLines.Length + 1];
        for (int i = 0; i < currentDialogueLines.Length; i++)
        {
            extendedLines[i] = currentDialogueLines[i];
        }

        extendedLines[extendedLines.Length - 1] = text;
        currentDialogueLines = extendedLines;
    }

    public void ContinueDialogue()
    {
        if (currentDialogueLines == null || currentDialogueLines.Length == 0)
        {
            if (dialogueOpenByManager)
            {
                HideDialogue();
            }

            return;
        }

        currentDialogueIndex++;
        if (currentDialogueIndex >= currentDialogueLines.Length)
        {
            HideDialogue();
            return;
        }

        ShowDialogueLine(currentSpeakerName, currentDialogueLines[currentDialogueIndex]);
    }

    private void ShowDialogueLine(string speakerName, string text)
    {
        if (dialoguePanel == null)
        {
            Debug.LogWarning("DialogueManager dialoguePanel is not assigned.");
            return;
        }

        dialoguePanel.SetActive(true);
        dialogueOpenByManager = true;

        if (speakerNameTMPText != null)
        {
            speakerNameTMPText.text = speakerName;
        }
        else if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
        }
        else
        {
            Debug.LogWarning("DialogueManager speaker name text is not assigned.");
        }

        if (dialogueTMPText != null)
        {
            dialogueTMPText.text = text;
        }
        else if (dialogueText != null)
        {
            dialogueText.text = text;
        }
        else
        {
            Debug.LogWarning("DialogueManager dialogue text is not assigned.");
        }
    }

    public void HideDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            currentDialogueLines = null;
            currentDialogueIndex = 0;
            dialogueOpenByManager = false;
        }
        else
        {
            Debug.LogWarning("DialogueManager dialoguePanel is not assigned.");
        }
    }

    private static bool HasPersistentClick(Button button, Object target, string methodName)
    {
        if (button == null || target == null || string.IsNullOrWhiteSpace(methodName))
        {
            return false;
        }

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == target &&
                button.onClick.GetPersistentMethodName(i) == methodName)
            {
                return true;
            }
        }

        return false;
    }
}
