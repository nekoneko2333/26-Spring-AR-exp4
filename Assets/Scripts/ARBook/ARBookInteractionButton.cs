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
    public bool useActivePlayerMover = true;
    public bool activateCapturableInteractablesOnStart = true;

    private ARBookInteractable currentInteractable;

    private void Start()
    {
        if (activateCapturableInteractablesOnStart)
        {
            ActivateCapturableInteractables();
        }

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

    private void ActivateCapturableInteractables()
    {
        ARBookInteractable[] interactables = FindObjectsOfType<ARBookInteractable>(true);
        for (int i = 0; i < interactables.Length; i++)
        {
            ARBookInteractable interactable = interactables[i];
            if (interactable != null && interactable.canBeCaptured)
            {
                interactable.gameObject.SetActive(true);
            }
        }
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
        ResolvePlayerMover();
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
        ARBookPlayerMover[] movers = GetCandidatePlayerMovers();
        ARBookInteractable bestInteractable = null;
        ARBookPlayerMover bestMover = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < interactables.Length; i++)
        {
            ARBookInteractable interactable = interactables[i];
            if (interactable == null)
            {
                continue;
            }

            for (int j = 0; j < movers.Length; j++)
            {
                ARBookPlayerMover mover = movers[j];
                if (mover == null || !interactable.CanInteract(mover))
                {
                    continue;
                }

                float distance = Vector3.Distance(mover.transform.position, interactable.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestInteractable = interactable;
                    bestMover = mover;
                }
            }
        }

        if (bestMover != null)
        {
            playerMover = bestMover;
        }

        return bestInteractable;
    }

    private void ResolvePlayerMover()
    {
        if (!useActivePlayerMover && playerMover != null)
        {
            return;
        }

        if (playerMover != null && playerMover.gameObject.activeInHierarchy)
        {
            return;
        }

        ARBookPlayerMover[] movers = FindObjectsOfType<ARBookPlayerMover>(true);
        for (int i = 0; i < movers.Length; i++)
        {
            if (movers[i] != null && movers[i].gameObject.activeInHierarchy && movers[i].enabled)
            {
                playerMover = movers[i];
                return;
            }
        }
    }

    private ARBookPlayerMover[] GetCandidatePlayerMovers()
    {
        if (!useActivePlayerMover && playerMover != null)
        {
            return new[] { playerMover };
        }

        ARBookPlayerMover[] allMovers = FindObjectsOfType<ARBookPlayerMover>(true);
        int activeCount = 0;

        for (int i = 0; i < allMovers.Length; i++)
        {
            if (allMovers[i] != null && allMovers[i].gameObject.activeInHierarchy && allMovers[i].enabled)
            {
                activeCount++;
            }
        }

        ARBookPlayerMover[] activeMovers = new ARBookPlayerMover[activeCount];
        int index = 0;

        for (int i = 0; i < allMovers.Length; i++)
        {
            if (allMovers[i] != null && allMovers[i].gameObject.activeInHierarchy && allMovers[i].enabled)
            {
                activeMovers[index] = allMovers[i];
                index++;
            }
        }

        return activeMovers;
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
