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
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(HideDialogue);
        }
        else
        {
            Debug.LogWarning("DialogueManager continueButton is not assigned.");
        }

        HideDialogue();
    }

    public void ShowDialogue(string speakerName, string text)
    {
        if (dialoguePanel == null)
        {
            Debug.LogWarning("DialogueManager dialoguePanel is not assigned.");
            return;
        }

        dialoguePanel.SetActive(true);

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
        }
        else
        {
            Debug.LogWarning("DialogueManager dialoguePanel is not assigned.");
        }
    }
}
