using UnityEngine;
using UnityEngine.UI;

public class ARBookCaptureController : MonoBehaviour
{
    public Button captureButton;
    public DialogueManager dialogueManager;
    public ARBookCollectionManager collectionManager;
    public ARBookGameShellController gameShell;

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

        if (captureButton != null &&
            !HasPersistentClick(captureButton, this, nameof(CaptureCurrentTarget)))
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

    public void CaptureCurrentTarget()
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

        ARBookInteractable target = currentTarget;
        ARBookPresentationDirector director =
            ARBookPresentationDirector.Instance != null
                ? ARBookPresentationDirector.Instance
                : FindObjectOfType<ARBookPresentationDirector>(true);

        if (director != null &&
            director.BeginCaptureBattle(target, () => CompleteCapture(target)))
        {
            RefreshCaptureButton();
            return;
        }

        Debug.LogWarning(
            "没有可用的战斗演出系统，已直接执行收服。",
            this);
        CompleteCapture(target);
    }

    private void CompleteCapture(ARBookInteractable target)
    {
        if (target == null || collectionManager == null)
        {
            return;
        }

        collectionManager.CaptureCreature(target.captureId);
        target.isCaptured = true;

        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogue(
                target.GetDisplayName(),
                target.captureDialogue);
        }

        target.onCaptured?.Invoke();
        RefreshCaptureButton();
    }

    public void RefreshCaptureButton()
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

        if (gameShell == null || !gameShell.IsHudVisible)
        {
            return false;
        }

        return !collectionManager.IsCaptured(currentTarget.captureId);
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
