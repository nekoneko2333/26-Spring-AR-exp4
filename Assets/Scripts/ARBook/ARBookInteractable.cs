using UnityEngine;
using UnityEngine.Events;

public class ARBookInteractable : MonoBehaviour
{
    public string displayName;
    [TextArea(2, 4)] public string[] dialogueFragments;
    public string animationTriggerName;
    public bool faceCameraOnInteract = true;
    public bool cycleDialogue = true;
    public ARBookMapNode interactionNode;
    public int interactionNodeIndex;
    public float interactionRadius = 2f;
    public bool requirePlayerAtInteractionNode = true;
    public bool canBeCaptured;
    public bool isCaptured;
    public string captureId;
    [TextArea(2, 4)] public string captureDialogue;
    public UnityEvent onCaptured;

    private int dialogueIndex;

    public bool CanInteract(ARBookPlayerMover playerMover)
    {
        if (!isActiveAndEnabled)
        {
            return false;
        }

        if (playerMover == null)
        {
            return true;
        }

        if (requirePlayerAtInteractionNode)
        {
            ARBookMapNode requiredNode = GetInteractionNode();
            if (requiredNode != null)
            {
                return playerMover.currentNode == requiredNode.transform;
            }
        }

        return Vector3.Distance(playerMover.transform.position, transform.position) <= interactionRadius;
    }

    public void Interact()
    {
        if (faceCameraOnInteract)
        {
            FaceCamera();
        }

        if (!string.IsNullOrWhiteSpace(animationTriggerName))
        {
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(animationTriggerName);
            }
        }

        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }

        if (dialogueManager == null)
        {
            Debug.LogWarning($"No DialogueManager found for interactable: {GetDisplayName()}");
            return;
        }

        dialogueManager.ShowDialogueSequence(GetDisplayName(), GetDialogueSequence());
    }

    private string[] GetDialogueSequence()
    {
        if (dialogueFragments == null || dialogueFragments.Length == 0)
        {
            return new[] { string.IsNullOrWhiteSpace(captureDialogue) ? string.Empty : captureDialogue };
        }

        if (!cycleDialogue)
        {
            return dialogueFragments;
        }

        string dialogue = dialogueFragments[Mathf.Clamp(dialogueIndex, 0, dialogueFragments.Length - 1)];
        
        if (cycleDialogue)
        {
            dialogueIndex = (dialogueIndex + 1) % dialogueFragments.Length;
        }

        return new[] { dialogue };
    }

    private void FaceCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("ARBookInteractable could not find Camera.main.");
            return;
        }

        Vector3 lookPosition = mainCamera.transform.position;
        lookPosition.y = transform.position.y;

        Vector3 direction = lookPosition - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    public string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    }

    private ARBookMapNode GetInteractionNode()
    {
        if (interactionNode != null)
        {
            return interactionNode;
        }

        if (interactionNodeIndex <= 0)
        {
            return null;
        }

        ARBookMapNode[] nodes = FindNodesInSameChapterRoot();

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].nodeIndex == interactionNodeIndex)
            {
                interactionNode = nodes[i];
                return interactionNode;
            }
        }

        return null;
    }

    private ARBookMapNode[] FindNodesInSameChapterRoot()
    {
        Transform parent = transform;
        while (parent != null)
        {
            if (parent.GetComponentInChildren<ARBookPlayerMover>(true) != null)
            {
                ARBookMapNode[] nodes = parent.GetComponentsInChildren<ARBookMapNode>(true);
                if (nodes.Length > 0)
                {
                    return nodes;
                }
            }

            parent = parent.parent;
        }

        return FindObjectsOfType<ARBookMapNode>();
    }
}
