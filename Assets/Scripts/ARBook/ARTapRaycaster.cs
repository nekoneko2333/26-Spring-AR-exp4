using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class ARTapRaycaster : MonoBehaviour
{
    public float raycastDistance = 100f;
    public LayerMask raycastLayers = ~0;
    public float tapCooldown = 0.15f;
    public bool interactImmediatelyOnModelTap;
    public bool enableSurfaceMovement;
    public LayerMask walkableSurfaceLayers;
    public bool debugMovementRaycasts = true;

    [Header("Move Target Effect")]
    [Tooltip("循环粒子预制体。有效点击后显示，角色到达或移动失败时自动隐藏。")]
    public GameObject moveTargetEffectPrefab;
    public Transform moveTargetEffectRoot;
    public Vector3 moveTargetEffectOffset = new Vector3(0f, 0.02f, 0f);
    [Tooltip("自动使用当前人物 NavMeshAgent 的直径作为粒子世界缩放。")]
    public bool scaleEffectToPlayerDiameter = true;
    [Tooltip("人物直径的倍率。1 表示与人物寻路直径相同。")]
    [Range(0.5f, 2f)] public float playerDiameterScaleMultiplier = 1f;
    [Tooltip("关闭自动人物直径后使用的世界缩放。")]
    [Range(0.1f, 2f)] public float moveTargetEffectScale = 0.7f;
    public bool alignMoveTargetEffectToSurface = true;

    private float nextTapTime;
    private GameObject moveTargetEffectInstance;
    private ARBookPlayerMover observedMover;
    private Vector3 pendingSurfaceNormal = Vector3.up;

    private void Update()
    {
        if (Time.time < nextTapTime)
        {
            return;
        }

        if (TryGetTapPosition(out Vector2 screenPosition))
        {
            nextTapTime = Time.time + tapCooldown;
            RaycastAt(screenPosition);
        }
    }

    private bool TryGetTapPosition(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began)
            {
                return false;
            }

            if (IsPointerOverInteractiveUI(touch.position))
            {
                return false;
            }

            screenPosition = touch.position;
            return true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverInteractiveUI(Input.mousePosition))
            {
                return false;
            }

            screenPosition = Input.mousePosition;
            return true;
        }

        return false;
    }

    private void RaycastAt(Vector2 screenPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("ARTapRaycaster requires a Camera tagged as MainCamera.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance, raycastLayers);
        if (hits.Length == 0)
        {
            if (debugMovementRaycasts)
            {
                Debug.Log(
                    $"Movement tap {screenPosition} hit no physics collider.",
                    this);
            }

            return;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            ARBookCollectible collectible =
                hits[i].collider.GetComponentInParent<ARBookCollectible>();
            if (collectible != null && collectible.collectOnTap)
            {
                collectible.TryCollect();
                return;
            }

            ARBookInteractable interactable =
                hits[i].collider.GetComponentInParent<ARBookInteractable>();
            if (interactable != null)
            {
                if (interactImmediatelyOnModelTap)
                {
                    interactable.Interact();
                    return;
                }
            }
        }

        if (!enableSurfaceMovement)
        {
            return;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            if (!IsLayerInMask(hits[i].collider.gameObject.layer, walkableSurfaceLayers))
            {
                continue;
            }

            ARBookPlayerMover mover = FindPlayerMoverForSurface(hits[i].collider.transform);
            if (mover != null)
            {
                ObserveMover(mover);
                pendingSurfaceNormal = hits[i].normal;
                mover.MoveToSurfacePoint(hits[i].point);
            }
            else if (debugMovementRaycasts)
            {
                Debug.LogWarning(
                    $"Walkable surface '{hits[i].collider.name}' was hit, " +
                    "but no active ARBookPlayerMover was found.",
                    this);
            }

            return;
        }

        if (debugMovementRaycasts)
        {
            Debug.Log(
                $"Movement tap {screenPosition} hit {hits.Length} collider(s), " +
                $"but none were on walkable layers {walkableSurfaceLayers.value}.",
                this);
        }
    }

    private bool IsLayerInMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    private ARBookPlayerMover FindPlayerMoverForSurface(Transform surface)
    {
        Transform parent = surface;
        while (parent != null)
        {
            ARBookPlayerMover mover = parent.GetComponentInChildren<ARBookPlayerMover>(true);
            if (mover != null && mover.gameObject.activeInHierarchy)
            {
                return mover;
            }

            parent = parent.parent;
        }

        ARBookPlayerMover[] movers = FindObjectsOfType<ARBookPlayerMover>(true);
        for (int i = 0; i < movers.Length; i++)
        {
            if (movers[i] != null && movers[i].gameObject.activeInHierarchy)
            {
                return movers[i];
            }
        }

        return null;
    }

    private void ObserveMover(ARBookPlayerMover mover)
    {
        if (observedMover == mover)
        {
            return;
        }

        if (observedMover != null)
        {
            observedMover.MoveTargetAccepted -= ShowMoveTargetEffect;
            observedMover.MoveFinished -= HandleMoveFinished;
        }

        observedMover = mover;
        if (observedMover != null)
        {
            observedMover.MoveTargetAccepted += ShowMoveTargetEffect;
            observedMover.MoveFinished += HandleMoveFinished;
        }
    }

    private void ShowMoveTargetEffect(Vector3 worldPosition)
    {
        if (moveTargetEffectPrefab == null)
        {
            return;
        }

        if (moveTargetEffectInstance == null)
        {
            moveTargetEffectInstance = Instantiate(
                moveTargetEffectPrefab,
                moveTargetEffectRoot);
        }

        moveTargetEffectInstance.transform.position =
            worldPosition + moveTargetEffectOffset;
        float targetScale = Mathf.Clamp(
            moveTargetEffectScale,
            0.1f,
            2f);
        if (scaleEffectToPlayerDiameter &&
            observedMover != null &&
            observedMover.navMeshAgent != null)
        {
            targetScale =
                observedMover.navMeshAgent.radius *
                2f *
                Mathf.Clamp(playerDiameterScaleMultiplier, 0.5f, 2f);
        }

        targetScale = Mathf.Clamp(targetScale, 0.1f, 2f);
        Vector3 parentScale = moveTargetEffectInstance.transform.parent != null
            ? moveTargetEffectInstance.transform.parent.lossyScale
            : Vector3.one;
        moveTargetEffectInstance.transform.localScale = new Vector3(
            targetScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            targetScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
            targetScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
        moveTargetEffectInstance.transform.rotation =
            alignMoveTargetEffectToSurface
                ? Quaternion.FromToRotation(Vector3.up, pendingSurfaceNormal)
                : Quaternion.identity;
        moveTargetEffectInstance.SetActive(true);

        ParticleSystem[] particles =
            moveTargetEffectInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Play(true);
        }
    }

    private void HandleMoveFinished(bool reachedTarget)
    {
        HideMoveTargetEffect();
    }

    private void HideMoveTargetEffect()
    {
        if (moveTargetEffectInstance == null)
        {
            return;
        }

        ParticleSystem[] particles =
            moveTargetEffectInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        moveTargetEffectInstance.SetActive(false);
    }

    private void OnDestroy()
    {
        ObserveMover(null);
    }

    private bool IsPointerOverInteractiveUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData =
            new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
        {
            GameObject hitObject = results[i].gameObject;
            if (hitObject == null)
            {
                continue;
            }

            if (hitObject.GetComponentInParent<Selectable>() != null ||
                hitObject.GetComponentInParent<ScrollRect>() != null)
            {
                return true;
            }
        }

        return false;
    }
}
