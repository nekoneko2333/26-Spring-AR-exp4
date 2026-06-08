using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARBookInteractionButton : MonoBehaviour
{
    public ARBookPlayerMover playerMover;
    public Button interactButton;
    public Text promptText;
    public TMP_Text promptTMPText;
    public string promptFormat = "Interact: {0}";
    public bool hideButtonWhenNoTarget = true;
    public ARBookCaptureController captureController;

    private ARBookInteractable currentInteractable;

    private void Start()
    {
        if (playerMover == null)
        {
            playerMover = FindObjectOfType<ARBookPlayerMover>();
        }

        if (captureController == null)
        {
            captureController = FindObjectOfType<ARBookCaptureController>();
        }

        if (interactButton != null)
        {
            interactButton.onClick.AddListener(InteractWithCurrentTarget);
        }
        else
        {
            Debug.LogWarning("ARBookInteractionButton interactButton is not assigned.");
        }

        RefreshCurrentTarget();
    }

    private void Update()
    {
        RefreshCurrentTarget();
    }

    public void InteractWithCurrentTarget()
    {
        RefreshCurrentTarget();

        if (currentInteractable == null)
        {
            return;
        }

        currentInteractable.Interact();
    }

    private void RefreshCurrentTarget()
    {
        currentInteractable = FindBestInteractable();
        bool hasTarget = currentInteractable != null;

        if (interactButton != null)
        {
            interactButton.interactable = hasTarget;

            if (hideButtonWhenNoTarget)
            {
                interactButton.gameObject.SetActive(hasTarget);
            }
        }

        SetPrompt(hasTarget ? string.Format(promptFormat, currentInteractable.GetDisplayName()) : string.Empty);

        if (captureController != null)
        {
            captureController.SetCurrentTarget(hasTarget ? currentInteractable : null);
        }
    }

    private ARBookInteractable FindBestInteractable()
    {
        ARBookInteractable[] interactables = FindObjectsOfType<ARBookInteractable>(true);
        ARBookInteractable bestInteractable = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < interactables.Length; i++)
        {
            ARBookInteractable interactable = interactables[i];
            if (interactable == null || !interactable.CanInteract(playerMover))
            {
                continue;
            }

            float distance = playerMover != null
                ? Vector3.Distance(playerMover.transform.position, interactable.transform.position)
                : 0f;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestInteractable = interactable;
            }
        }

        return bestInteractable;
    }

    private void SetPrompt(string text)
    {
        if (promptTMPText != null)
        {
            promptTMPText.text = text;
        }

        if (promptText != null)
        {
            promptText.text = text;
        }
    }
}
