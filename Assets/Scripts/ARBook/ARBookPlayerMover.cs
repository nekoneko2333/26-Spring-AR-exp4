using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class ARBookPlayerMover : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float stopDistance = 0.01f;
    public Transform routeRoot;
    [FormerlySerializedAs("currentNode")] public Transform startPoint;
    public bool isMoving;
    public bool rotateToMoveDirection = true;
    public float turnSpeed = 720f;
    [FormerlySerializedAs("snapToCurrentNodeOnStart")]
    public bool snapToStartPointOnStart = true;
    public bool useSurfaceHeight;
    public float heightOffset;
    public GameObject visibleModel;
    public bool activateVisibleModelOnStart = true;
    public bool ignoreMoveRequestsWhileMoving;

    [Header("Animation")]
    public Animator characterAnimator;
    public string walkingBoolParameter = "IsWalking";
    public string greetingTriggerParameter = "Wave";
    public string speedFloatParameter = "Speed";
    public string turnFloatParameter = "Turn";
    public string idleVariantIntParameter = "IdleVariant";
    [Min(0.1f)] public float runSpeedThreshold = 3.5f;
    [Min(1f)] public float idleVariantInterval = 5f;

    public NavMeshAgent navMeshAgent;
    public float navMeshSampleDistance = 3f;
    [Min(0.01f)] public float targetVerticalTolerance = 0.75f;

    private Coroutine moveRoutine;
    private Quaternion modelFacingCorrection = Quaternion.identity;
    private float nextIdleVariantTime;

    private void Awake()
    {
        ConfigureNavMeshAgent();
        ResolveAnimator();
    }

    private void Start()
    {
        if (moveSpeed < 0.01f)
        {
            moveSpeed = 2f;
        }

        if (activateVisibleModelOnStart)
        {
            ActivateVisibleModel();
        }

        CacheModelFacingCorrection();
        SetWalkingAnimation(false);
        nextIdleVariantTime = Time.time + idleVariantInterval;

        if (snapToStartPointOnStart && startPoint != null)
        {
            SetPositionAtStartPoint();
        }
    }

    private void Update()
    {
        if (!isMoving)
        {
            RandomizeIdleVariantIfNeeded(false);
        }
    }

    public void MoveToSurfacePoint(Vector3 worldPoint)
    {
        if (isMoving && ignoreMoveRequestsWhileMoving)
        {
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveToSurfacePointRoutine(worldPoint));
    }

    public void PlayGreeting()
    {
        SetAnimationTrigger(greetingTriggerParameter);
    }

    public void PlayAnimationTrigger(string parameterName)
    {
        SetAnimationTrigger(parameterName);
    }

    public float GetDistanceTo(Vector3 worldPoint)
    {
        Transform movementSpace = GetMovementSpace();
        if (movementSpace == null)
        {
            return Vector3.Distance(transform.position, worldPoint);
        }

        Vector3 localPoint = movementSpace.InverseTransformPoint(worldPoint);
        return Vector3.Distance(transform.localPosition, localPoint);
    }

    private IEnumerator MoveToSurfacePointRoutine(Vector3 worldPoint)
    {
        isMoving = true;
        SetWalkingAnimation(true);
        yield return MoveWithNavMeshRoutine(worldPoint);
        isMoving = false;
        SetWalkingAnimation(false);
        moveRoutine = null;
    }

    private IEnumerator MoveWithNavMeshRoutine(Vector3 worldPoint)
    {
        ConfigureNavMeshAgent();

        if (navMeshAgent == null)
        {
            Debug.LogWarning("NavMesh movement requires a NavMeshAgent.");
            yield break;
        }

        int areaMask = navMeshAgent.areaMask;
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = navMeshAgent.agentTypeID,
            areaMask = areaMask
        };

        float sampleDistance = Mathf.Max(navMeshSampleDistance, navMeshAgent.height * 2f);
        bool foundStart = NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit startHit,
            sampleDistance,
            areaMask);
        bool foundTarget = NavMesh.SamplePosition(
            worldPoint,
            out NavMeshHit targetHit,
            Mathf.Min(sampleDistance, Mathf.Max(0.01f, targetVerticalTolerance)),
            areaMask);

        if (!foundTarget)
        {
            Debug.LogWarning(
                $"{name} could not find NavMesh near the tapped point {worldPoint}.");
            yield break;
        }

        if (!foundStart)
        {
            Debug.LogWarning(
                $"{name} is outside the baked NavMesh. Moving directly to the nearest valid point.");
            yield return MoveToWorldPositionRoutine(targetHit.position);
            yield break;
        }

        NavMeshPath path = new NavMeshPath();
        if (!NavMesh.CalculatePath(startHit.position, targetHit.position, filter, path) ||
            path.status != NavMeshPathStatus.PathComplete ||
            path.corners.Length < 2)
        {
            Debug.LogWarning(
                $"{name} could not calculate a complete NavMesh path. Moving directly to the valid point.");
            yield return MoveToWorldPositionRoutine(targetHit.position);
            yield break;
        }

        Transform movementSpace = GetMovementSpace();
        float fixedY = transform.localPosition.y;

        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 localCorner = movementSpace != null
                ? movementSpace.InverseTransformPoint(path.corners[i])
                : path.corners[i];

            localCorner.y = useSurfaceHeight
                ? localCorner.y + heightOffset
                : fixedY;

            yield return MoveToLocalPositionRoutine(localCorner);
        }
    }

    private IEnumerator MoveToWorldPositionRoutine(Vector3 worldPosition)
    {
        Transform movementSpace = GetMovementSpace();
        Vector3 localPosition = movementSpace != null
            ? movementSpace.InverseTransformPoint(worldPosition)
            : worldPosition;

        localPosition.y = useSurfaceHeight
            ? localPosition.y + heightOffset
            : transform.localPosition.y;

        yield return MoveToLocalPositionRoutine(localPosition);
    }

    private IEnumerator MoveToLocalPositionRoutine(Vector3 targetPosition)
    {
        while (true)
        {
            Vector3 currentPosition = transform.localPosition;
            if (Vector3.Distance(currentPosition, targetPosition) <= stopDistance)
            {
                transform.localPosition = targetPosition;
                yield break;
            }

            Vector3 nextPosition = Vector3.MoveTowards(
                currentPosition,
                targetPosition,
                moveSpeed * Time.deltaTime);

            Vector3 moveDirection = nextPosition - currentPosition;
            UpdateLocomotionParameters(moveDirection);
            if (rotateToMoveDirection && moveDirection.sqrMagnitude > 0.0001f)
            {
                moveDirection.y = 0f;
                if (moveDirection.sqrMagnitude > 0.0001f)
                {
                    Quaternion movementRotation =
                        Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
                    Quaternion targetRotation =
                        movementRotation * modelFacingCorrection;

                    transform.localRotation = Quaternion.RotateTowards(
                        transform.localRotation,
                        targetRotation,
                        turnSpeed * Time.deltaTime);
                }
            }

            transform.localPosition = nextPosition;
            yield return null;
        }
    }

    private void CacheModelFacingCorrection()
    {
        Transform modelTransform = visibleModel != null
            ? visibleModel.transform
            : transform.childCount > 0
                ? transform.GetChild(0)
                : null;

        if (modelTransform == null)
        {
            modelFacingCorrection = Quaternion.identity;
            return;
        }

        Vector3 modelForwardInParent = transform.InverseTransformDirection(
            modelTransform.forward);
        modelForwardInParent.y = 0f;

        if (modelForwardInParent.sqrMagnitude <= 0.0001f)
        {
            modelFacingCorrection = Quaternion.identity;
            return;
        }

        Quaternion modelAxisRotation = Quaternion.LookRotation(
            modelForwardInParent.normalized,
            Vector3.up);
        modelFacingCorrection = Quaternion.Inverse(modelAxisRotation);
    }

    private Transform GetMovementSpace()
    {
        return routeRoot != null ? routeRoot : transform.parent;
    }

    private void SetPositionAtStartPoint()
    {
        Transform movementSpace = GetMovementSpace();
        Vector3 targetPosition = movementSpace != null
            ? movementSpace.InverseTransformPoint(startPoint.position)
            : startPoint.position;

        targetPosition.y = useSurfaceHeight
            ? targetPosition.y + heightOffset
            : transform.localPosition.y;

        transform.localPosition = targetPosition;
    }

    private void ActivateVisibleModel()
    {
        if (visibleModel != null)
        {
            visibleModel.SetActive(true);
            return;
        }

        if (transform.childCount > 0)
        {
            transform.GetChild(0).gameObject.SetActive(true);
        }
    }

    private void ResolveAnimator()
    {
        if (characterAnimator != null)
        {
            return;
        }

        if (visibleModel != null)
        {
            characterAnimator = visibleModel.GetComponentInChildren<Animator>(true);
        }

        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>(true);
        }
    }

    private void SetWalkingAnimation(bool walking)
    {
        ResolveAnimator();
        if (HasParameter(walkingBoolParameter, AnimatorControllerParameterType.Bool))
        {
            characterAnimator.SetBool(walkingBoolParameter, walking);
        }

        if (HasParameter(speedFloatParameter, AnimatorControllerParameterType.Float))
        {
            float normalizedSpeed = walking
                ? Mathf.Clamp01(moveSpeed / Mathf.Max(0.1f, runSpeedThreshold))
                : 0f;
            characterAnimator.SetFloat(speedFloatParameter, normalizedSpeed);
        }

        if (!walking)
        {
            SetTurnParameter(0f);
            RandomizeIdleVariantIfNeeded(true);
        }
    }

    private void SetAnimationTrigger(string parameterName)
    {
        ResolveAnimator();
        if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            characterAnimator.SetTrigger(parameterName);
        }
        else
        {
            Debug.LogWarning(
                $"{name} animator has no Trigger parameter named '{parameterName}'.");
        }
    }

    private bool HasParameter(
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        if (characterAnimator == null ||
            string.IsNullOrWhiteSpace(parameterName) ||
            characterAnimator.runtimeAnimatorController == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = characterAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName &&
                parameters[i].type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateLocomotionParameters(Vector3 moveDirection)
    {
        ResolveAnimator();
        if (characterAnimator == null)
        {
            return;
        }

        if (HasParameter(speedFloatParameter, AnimatorControllerParameterType.Float))
        {
            float normalizedSpeed =
                Mathf.Clamp01(moveSpeed / Mathf.Max(0.1f, runSpeedThreshold));
            characterAnimator.SetFloat(speedFloatParameter, normalizedSpeed);
        }

        Vector3 flatDirection = moveDirection;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            float signedAngle = Vector3.SignedAngle(
                transform.forward,
                flatDirection.normalized,
                Vector3.up);
            SetTurnParameter(Mathf.Clamp(signedAngle / 90f, -1f, 1f));
        }

        RandomizeIdleVariantIfNeeded(false);
    }

    private void SetTurnParameter(float value)
    {
        if (HasParameter(turnFloatParameter, AnimatorControllerParameterType.Float))
        {
            characterAnimator.SetFloat(turnFloatParameter, value);
        }
    }

    private void RandomizeIdleVariantIfNeeded(bool forceSchedule)
    {
        if (!HasParameter(
                idleVariantIntParameter,
                AnimatorControllerParameterType.Int))
        {
            return;
        }

        if (!forceSchedule && Time.time < nextIdleVariantTime)
        {
            return;
        }

        characterAnimator.SetInteger(
            idleVariantIntParameter,
            Random.Range(0, 2));
        nextIdleVariantTime =
            Time.time + Mathf.Max(1f, idleVariantInterval);
    }

    private void ConfigureNavMeshAgent()
    {
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        if (navMeshAgent == null)
        {
            return;
        }

        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;

        if (navMeshAgent.enabled)
        {
            navMeshAgent.enabled = false;
        }
    }
}
