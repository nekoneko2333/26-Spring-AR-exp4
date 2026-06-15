using System;
using UnityEngine;
using UnityEngine.Events;

public class ARBookInteractable : MonoBehaviour
{
    public static event Action<ARBookInteractable> Interacted;

    public string displayName;
    [TextArea(2, 4)] public string[] dialogueFragments;
    public string animationTriggerName;
    public bool faceCameraOnInteract = true;
    public bool cycleDialogue = true;
    public float interactionRadius = 2f;
    public bool canBeCaptured;
    public bool isCaptured;
    public string captureId;
    [TextArea(2, 4)] public string captureDialogue;
    public UnityEvent onCaptured;
    public bool useDefaultDialogue = true;
    public UnityEvent onInteracted;
    [Tooltip("有可见模型时，自动使用 3D 双角色对话。")]
    public bool useCinematicDialogue = true;
    [Tooltip("通常留空。自动识别错误时，拖入该角色的模型根物体。")]
    public GameObject presentationModelRoot;
    [Tooltip("可选。对话或战斗副本使用的动画控制器。")]
    public RuntimeAnimatorController presentationAnimatorController;
    [Tooltip("战斗演出中保持模型当前待机，不切换其他战斗动画。")]
    public bool keepBattleIdle;

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

        return playerMover.GetDistanceTo(transform.position) <= interactionRadius;
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

        Interacted?.Invoke(this);
        onInteracted?.Invoke();

        if (useCinematicDialogue &&
            ARBookPresentationDirector.TryBeginDialogue(this))
        {
            return;
        }

        if (!useDefaultDialogue)
        {
            return;
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

        dialogueManager.ShowDialogueSequence(
            GetDisplayName(),
            ConsumeDialogueSequence());
    }

    public string[] ConsumeDialogueSequence()
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

}
