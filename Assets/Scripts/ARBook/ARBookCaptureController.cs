using UnityEngine;
using UnityEngine.UI;

public class ARBookCaptureController : MonoBehaviour
{
    public Button captureButton;
    public DialogueManager dialogueManager;
    public ARBookCollectionManager collectionManager;

    private ARBookInteractable currentTarget;

    private void Start()
    {
        if (dialogueManager == null)
        {
            dialogueManager = DialogueManager.Instance != null
                ? DialogueManager.Instance
                : FindObjectOfType<DialogueManager>();
        }

        if (collectionManager == null)
        {
            collectionManager = FindObjectOfType<ARBookCollectionManager>();
        }

        if (captureButton != null)
        {
            captureButton.onClick.AddListener(CaptureCurrentTarget);
        }
        else
        {
            Debug.LogWarning("ARBookCaptureController captureButton is not assigned.");
        }

        RefreshCaptureButton();
    }

    public void SetCurrentTarget(ARBookInteractable target)
    {
        currentTarget = target;

        if (currentTarget != null
            && currentTarget.canBeCaptured
            && collectionManager != null
            && collectionManager.IsCaptured(currentTarget.captureId))
        {
            currentTarget.isCaptured = true;
        }

        RefreshCaptureButton();
    }

    private void CaptureCurrentTarget()
    {
        if (!CanShowCaptureButton())
        {
            RefreshCaptureButton();
            return;
        }

        ARBookCaptureRequirement requirement =
            currentTarget.GetComponent<ARBookCaptureRequirement>();
        if (requirement != null && !requirement.IsSatisfied())
        {
            if (dialogueManager != null)
            {
                dialogueManager.ShowDialogue(
                    requirement.lockedSpeaker,
                    requirement.GetLockedDialogue());
            }

            RefreshCaptureButton();
            return;
        }

        collectionManager.CaptureCreature(currentTarget.captureId);
        currentTarget.isCaptured = true;

        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogue(currentTarget.GetDisplayName(), currentTarget.captureDialogue);
        }
        else
        {
            Debug.LogWarning("ARBookCaptureController dialogueManager is not assigned.");
        }

        currentTarget.onCaptured?.Invoke();
        RefreshCaptureButton();
    }

    private void RefreshCaptureButton()
    {
        if (captureButton != null)
        {
            captureButton.gameObject.SetActive(CanShowCaptureButton());
        }
    }

    private bool CanShowCaptureButton()
    {
        if (currentTarget == null || !currentTarget.canBeCaptured || currentTarget.isCaptured)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(currentTarget.captureId))
        {
            return false;
        }

        if (collectionManager == null)
        {
            return false;
        }

        return !collectionManager.IsCaptured(currentTarget.captureId);
    }
}
