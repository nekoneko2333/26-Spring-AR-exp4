using System.Collections;
using UnityEngine;

public class ARCharacterAgent : MonoBehaviour
{
    public Animator animator;
    public float moveSpeed = 1.5f;
    public float rotationSpeed = 540f;
    public float stopDistance = 0.45f;
    public string walkingBoolName = "IsWalking";
    public string captureTriggerName = "Capture";

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private Coroutine moveRoutine;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
    }

    public Coroutine MoveToCaptureRange(Transform target)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveToTarget(target));
        return moveRoutine;
    }

    public void PlayCapture()
    {
        SetWalking(false);

        if (animator != null && !string.IsNullOrWhiteSpace(captureTriggerName))
        {
            animator.SetTrigger(captureTriggerName);
        }
    }

    public void ResetToStart()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        transform.localPosition = startLocalPosition;
        transform.localRotation = startLocalRotation;
        SetWalking(false);
    }

    private IEnumerator MoveToTarget(Transform target)
    {
        SetWalking(true);

        while (target != null)
        {
            var toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= stopDistance)
            {
                break;
            }

            var direction = toTarget.normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            if (direction.sqrMagnitude > 0.0001f)
            {
                var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            yield return null;
        }

        SetWalking(false);
        moveRoutine = null;
    }

    private void SetWalking(bool isWalking)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(walkingBoolName))
        {
            animator.SetBool(walkingBoolName, isWalking);
        }
    }
}
