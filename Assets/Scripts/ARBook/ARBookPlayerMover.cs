using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ARBookPlayerMover : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float stopDistance = 0.01f;
    public Transform routeRoot;
    public Transform currentNode;
    public bool isMoving;
    public bool rotateToMoveDirection = true;
    public bool snapToCurrentNodeOnStart = true;
    public bool useNearestNodeAsStart = true;
    public bool useNodeHeight;
    public float heightOffset;
    public GameObject visibleModel;
    public bool activateVisibleModelOnStart = true;
    public bool logMovementDebug;
    public bool ignoreMoveRequestsWhileMoving = true;
    public bool moveDirectlyToTappedNode = true;
    public ARBookMapNode[] orderedNodes;

    private Coroutine moveRoutine;

    private void Start()
    {
        if (moveSpeed < 0.5f)
        {
            Debug.LogWarning($"Move speed was too low on {name}. Using 2 for this play session.");
            moveSpeed = 2f;
        }

        EnsureOrderedNodes();

        if (activateVisibleModelOnStart)
        {
            ActivateVisibleModel();
        }

        if (useNearestNodeAsStart)
        {
            SetCurrentNodeToNearestNode();
        }

        if (snapToCurrentNodeOnStart && currentNode != null)
        {
            SetPositionAtNode(currentNode);
        }
    }

    public void MoveToNode(ARBookMapNode targetNode)
    {
        if (targetNode == null)
        {
            Debug.LogWarning("MoveToNode was called with a null target node.");
            return;
        }

        if (!targetNode.isUnlocked)
        {
            Debug.Log($"Cannot move to locked node: {targetNode.name}");
            return;
        }

        if (isMoving && ignoreMoveRequestsWhileMoving)
        {
            Debug.Log($"Ignoring node tap while moving: {targetNode.name}");
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveAlongRouteRoutine(targetNode));
    }

    private IEnumerator MoveAlongRouteRoutine(ARBookMapNode targetNode)
    {
        isMoving = true;

        List<ARBookMapNode> route = BuildRoute(targetNode);
        for (int i = 0; i < route.Count; i++)
        {
            yield return MoveOneStepRoutine(route[i]);
            currentNode = route[i].transform;
            route[i].onNodeReached?.Invoke();
        }

        isMoving = false;
        moveRoutine = null;
    }

    private IEnumerator MoveOneStepRoutine(ARBookMapNode targetNode)
    {
        Transform movementSpace = GetMovementSpace();
        float fixedY = transform.localPosition.y;
        bool loggedThisStep = false;

        while (true)
        {
            Vector3 currentPosition = transform.localPosition;
            Vector3 targetPosition = GetTargetLocalPosition(targetNode.transform, movementSpace);
            targetPosition.y = useNodeHeight ? targetPosition.y + heightOffset : fixedY;

            if (logMovementDebug && !loggedThisStep)
            {
                Debug.Log($"Moving {name} local {currentPosition} -> {targetPosition} for node {targetNode.name}");
                loggedThisStep = true;
            }

            if (Vector3.Distance(currentPosition, targetPosition) <= stopDistance)
            {
                transform.localPosition = targetPosition;
                yield break;
            }

            Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);

            Vector3 moveDirection = nextPosition - currentPosition;
            if (rotateToMoveDirection && moveDirection.sqrMagnitude > 0.0001f)
            {
                moveDirection.y = 0f;
                if (moveDirection.sqrMagnitude > 0.0001f)
                {
                    transform.localRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
                }
            }

            transform.localPosition = nextPosition;

            yield return null;
        }
    }

    private List<ARBookMapNode> BuildRoute(ARBookMapNode targetNode)
    {
        if (moveDirectlyToTappedNode)
        {
            return new List<ARBookMapNode> { targetNode };
        }

        EnsureOrderedNodes();

        if (orderedNodes == null || orderedNodes.Length == 0 || currentNode == null)
        {
            return new List<ARBookMapNode> { targetNode };
        }

        ARBookMapNode currentMapNode = currentNode.GetComponent<ARBookMapNode>();
        if (currentMapNode == null)
        {
            return new List<ARBookMapNode> { targetNode };
        }

        int currentIndex = System.Array.IndexOf(orderedNodes, currentMapNode);
        int targetIndex = System.Array.IndexOf(orderedNodes, targetNode);
        if (currentIndex < 0 || targetIndex < 0)
        {
            return new List<ARBookMapNode> { targetNode };
        }

        List<ARBookMapNode> route = new List<ARBookMapNode>();
        int step = targetIndex >= currentIndex ? 1 : -1;

        for (int i = currentIndex + step; i != targetIndex + step; i += step)
        {
            if (orderedNodes[i] != null && orderedNodes[i].isUnlocked)
            {
                route.Add(orderedNodes[i]);
            }
            else
            {
                Debug.Log($"Route stopped at locked or missing node index: {i}");
                break;
            }
        }

        return route;
    }

    private void EnsureOrderedNodes()
    {
        if (orderedNodes != null && orderedNodes.Length > 0)
        {
            return;
        }

        orderedNodes = FindObjectsOfType<ARBookMapNode>()
            .Where(node => node != null && node.nodeIndex > 0)
            .OrderBy(node => node.nodeIndex)
            .ToArray();
    }

    private Vector3 GetTargetLocalPosition(Transform target, Transform movementSpace)
    {
        if (movementSpace != null)
        {
            return movementSpace.InverseTransformPoint(target.position);
        }

        if (target.parent == transform.parent)
        {
            return target.localPosition;
        }

        return transform.parent != null
            ? transform.parent.InverseTransformPoint(target.position)
            : target.position;
    }

    private Transform GetMovementSpace()
    {
        if (routeRoot != null)
        {
            return routeRoot;
        }

        return transform.parent;
    }

    private void SetPositionAtNode(Transform node)
    {
        Transform movementSpace = GetMovementSpace();
        Vector3 targetPosition = GetTargetLocalPosition(node, movementSpace);

        if (useNodeHeight)
        {
            targetPosition.y += heightOffset;
        }
        else
        {
            targetPosition.y = transform.localPosition.y;
        }

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

    private void SetCurrentNodeToNearestNode()
    {
        ARBookMapNode nearestNode = FindNearestNode();
        if (nearestNode == null)
        {
            return;
        }

        currentNode = nearestNode.transform;

        if (logMovementDebug)
        {
            Debug.Log($"Nearest start node for {name}: {nearestNode.name}");
        }
    }

    private ARBookMapNode FindNearestNode()
    {
        if (orderedNodes == null || orderedNodes.Length == 0)
        {
            return null;
        }

        Transform movementSpace = GetMovementSpace();
        Vector3 currentPosition = transform.localPosition;

        ARBookMapNode nearestNode = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < orderedNodes.Length; i++)
        {
            ARBookMapNode node = orderedNodes[i];
            if (node == null)
            {
                continue;
            }

            Vector3 nodePosition = GetTargetLocalPosition(node.transform, movementSpace);
            nodePosition.y = currentPosition.y;
            float distance = Vector3.Distance(currentPosition, nodePosition);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestNode = node;
            }
        }

        return nearestNode;
    }
}
