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
    public bool tryAllEnabledCamerasOnMiss = true;
    public float minimumRaycastDistance = 1000f;
    public bool interactImmediatelyOnModelTap;
    public bool enableSurfaceMovement;
    public LayerMask walkableSurfaceLayers;
    public bool debugMovementRaycasts = true;
    public bool debugMovementHitDetails;
    [Range(1, 12)] public int debugHitDetailLimit = 6;

    [Header("Move Target Effect")]
    [Tooltip("循环粒子预制体。有效点击后显示，角色到达或移动失败时自动隐藏。")]
    public GameObject moveTargetEffectPrefab;
    public Transform moveTargetEffectRoot;
    public Vector3 moveTargetEffectOffset = new Vector3(0f, 0.02f, 0f);
    [Tooltip("自动使用当前人物 NavMeshAgent 的直径作为粒子世界缩放。")]
    public bool scaleEffectToPlayerDiameter = true;
    public bool showMoveTargetEffectAtTappedSurface = true;
    [Tooltip("人物直径的倍率。1 表示与人物寻路直径相同。")]
    [Range(0.5f, 2f)] public float playerDiameterScaleMultiplier = 1f;
    [Tooltip("关闭自动人物直径后使用的世界缩放。")]
    [Range(0.1f, 2f)] public float moveTargetEffectScale = 0.7f;
    public bool alignMoveTargetEffectToSurface = true;

    private float nextTapTime;
    private GameObject moveTargetEffectInstance;
    private ARBookPlayerMover observedMover;
    private Vector3 pendingSurfaceNormal = Vector3.up;
    private Vector3 pendingSurfacePoint;

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

        Physics.SyncTransforms();

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        float distance = Mathf.Max(raycastDistance, minimumRaycastDistance);
        RaycastHit[] hits = Physics.RaycastAll(ray, distance, raycastLayers);
        Camera hitCamera = mainCamera;
        if (hits.Length == 0 && tryAllEnabledCamerasOnMiss)
        {
            hits = RaycastWithFallbackCameras(
                screenPosition,
                mainCamera,
                distance,
                out hitCamera);
        }

        if (hits.Length == 0)
        {
            if (debugMovementRaycasts)
            {
                Debug.Log(
                    $"Movement tap {screenPosition} hit no physics collider. " +
                    $"mainCamera='{mainCamera.name}', " +
                    $"cameraPosition={mainCamera.transform.position}, " +
                    $"rayOrigin={ray.origin}, rayDirection={ray.direction}, " +
                    $"distance={distance}, raycastLayers={raycastLayers.value}.",
                    this);
            }

            return;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        if (debugMovementRaycasts && hitCamera != mainCamera)
        {
            Debug.Log(
                $"Movement tap {screenPosition} missed Camera.main " +
                $"'{mainCamera.name}' but hit using camera '{hitCamera.name}'.",
                this);
        }

        LogHitDetails(screenPosition, hits);

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
                pendingSurfacePoint = hits[i].point;
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

    private RaycastHit[] RaycastWithFallbackCameras(
        Vector2 screenPosition,
        Camera mainCamera,
        float distance,
        out Camera hitCamera)
    {
        hitCamera = mainCamera;
        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null ||
                camera == mainCamera ||
                !camera.enabled ||
                !camera.gameObject.activeInHierarchy)
            {
                continue;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, distance, raycastLayers);
            if (hits.Length > 0)
            {
                hitCamera = camera;
                return hits;
            }
        }

        return Array.Empty<RaycastHit>();
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

        Vector3 effectPosition = showMoveTargetEffectAtTappedSurface
            ? pendingSurfacePoint
            : worldPosition;
        moveTargetEffectInstance.transform.position =
            effectPosition + moveTargetEffectOffset;
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
        DestroyMoveTargetEffect(false);
    }

    private void DestroyMoveTargetEffect(bool immediate)
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

        if (immediate || !Application.isPlaying)
        {
            DestroyImmediate(moveTargetEffectInstance);
        }
        else
        {
            Destroy(moveTargetEffectInstance);
        }

        moveTargetEffectInstance = null;
    }

    private void OnDestroy()
    {
        ObserveMover(null);
        DestroyMoveTargetEffect(true);
    }

    private void LogHitDetails(Vector2 screenPosition, RaycastHit[] hits)
    {
        if (!debugMovementRaycasts || !debugMovementHitDetails)
        {
            return;
        }

        int count = Mathf.Min(
            hits.Length,
            Mathf.Max(1, debugHitDetailLimit));
        string message =
            $"Movement tap {screenPosition} hit {hits.Length} collider(s).";
        for (int i = 0; i < count; i++)
        {
            Collider hitCollider = hits[i].collider;
            GameObject hitObject = hitCollider != null
                ? hitCollider.gameObject
                : null;
            int layer = hitObject != null ? hitObject.layer : -1;
            bool walkable = hitObject != null &&
                IsLayerInMask(layer, walkableSurfaceLayers);
            message +=
                $"\n  #{i}: name='{(hitCollider != null ? hitCollider.name : "null")}', " +
                $"layer={layer}, walkable={walkable}, " +
                $"distance={hits[i].distance:F3}, point={hits[i].point}, " +
                $"normal={hits[i].normal}";
        }

        Debug.Log(message, this);
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
