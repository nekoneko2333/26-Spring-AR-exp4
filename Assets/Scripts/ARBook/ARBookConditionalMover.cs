using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ARBookConditionalMover : MonoBehaviour
{
    [Header("Conditions")]
    public ARBookConditionGroup conditions = new ARBookConditionGroup();
    public ARBookCollectionManager collectionManager;
    public ARBookChapterProgress chapterProgress;

    [Header("Movement")]
    public Transform target;
    public Transform startPoint;
    public Transform endPoint;
    public bool snapToStartOnStart;
    public bool useLocalSpace;
    public float moveSpeed = 1f;
    public float stopDistance = 0.01f;
    public bool rotateToMoveDirection = true;
    public float turnSpeed = 360f;
    public bool moveOnlyOnce = true;
    public bool autoMoveWhenConditionsMet;
    public bool evaluateRepeatedly;
    public float evaluateInterval = 0.5f;

    [Header("Events")]
    public UnityEvent onMoveStarted;
    public UnityEvent onMoveCompleted;
    public UnityEvent onMoveBlocked;

    private bool hasMoved;
    private bool isMoving;
    private float nextEvaluateTime;
    private Coroutine moveRoutine;

    private void Start()
    {
        if (target == null)
        {
            target = transform;
        }

        if (snapToStartOnStart && startPoint != null)
        {
            SetTargetPosition(startPoint.position);
        }

        if (autoMoveWhenConditionsMet)
        {
            TryMove(false);
        }
    }

    private void Update()
    {
        if (!evaluateRepeatedly || Time.time < nextEvaluateTime)
        {
            return;
        }

        if (autoMoveWhenConditionsMet)
        {
            TryMove(false);
        }

        nextEvaluateTime = Time.time + Mathf.Max(0.1f, evaluateInterval);
    }

    [ContextMenu("Try Move")]
    public void TryMove()
    {
        TryMove(true);
    }

    private void TryMove(bool notifyBlocked)
    {
        ResolveReferences();

        if (moveOnlyOnce && hasMoved)
        {
            return;
        }

        if (isMoving)
        {
            return;
        }

        if (conditions != null &&
            !conditions.IsMet(collectionManager, chapterProgress))
        {
            if (notifyBlocked)
            {
                onMoveBlocked?.Invoke();
            }

            return;
        }

        MoveNow();
    }

    [ContextMenu("Move Now Ignore Conditions")]
    public void MoveNow()
    {
        if (target == null)
        {
            target = transform;
        }

        if (target == null || endPoint == null)
        {
            Debug.LogWarning($"{name} needs a target and an end point.");
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveRoutine());
    }

    [ContextMenu("Reset To Start")]
    public void ResetToStart()
    {
        if (target == null)
        {
            target = transform;
        }

        if (startPoint == null || target == null)
        {
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        isMoving = false;
        hasMoved = false;
        SetTargetPosition(startPoint.position);
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

    private IEnumerator MoveRoutine()
    {
        isMoving = true;
        onMoveStarted?.Invoke();

        Vector3 destination = GetPositionInTargetSpace(endPoint.position);
        while (Vector3.Distance(GetCurrentPosition(), destination) > stopDistance)
        {
            Vector3 current = GetCurrentPosition();
            Vector3 next = Vector3.MoveTowards(
                current,
                destination,
                Mathf.Max(0.01f, moveSpeed) * Time.deltaTime);

            Vector3 direction = next - current;
            SetCurrentPosition(next);
            RotateTowards(direction);
            yield return null;
        }

        SetCurrentPosition(destination);
        isMoving = false;
        hasMoved = true;
        moveRoutine = null;
        onMoveCompleted?.Invoke();
    }

    private Vector3 GetCurrentPosition()
    {
        return useLocalSpace ? target.localPosition : target.position;
    }

    private void SetCurrentPosition(Vector3 position)
    {
        if (useLocalSpace)
        {
            target.localPosition = position;
        }
        else
        {
            target.position = position;
        }
    }

    private void SetTargetPosition(Vector3 worldPosition)
    {
        SetCurrentPosition(GetPositionInTargetSpace(worldPosition));
    }

    private Vector3 GetPositionInTargetSpace(Vector3 worldPosition)
    {
        if (!useLocalSpace)
        {
            return worldPosition;
        }

        Transform parent = target.parent;
        return parent != null ? parent.InverseTransformPoint(worldPosition) : worldPosition;
    }

    private void RotateTowards(Vector3 direction)
    {
        if (!rotateToMoveDirection || direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        target.rotation = Quaternion.RotateTowards(
            target.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);
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
    }
}
