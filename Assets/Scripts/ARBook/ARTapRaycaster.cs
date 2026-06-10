using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class ARTapRaycaster : MonoBehaviour
{
    public float raycastDistance = 100f;
    public LayerMask raycastLayers = ~0;
    public float tapCooldown = 0.15f;
    public bool interactImmediatelyOnModelTap;
    public bool enableSurfaceMovement;
    public LayerMask walkableSurfaceLayers;

    private float nextTapTime;

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

            if (IsPointerOverUI(touch.fingerId))
            {
                return false;
            }

            screenPosition = touch.position;
            return true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI(-1))
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
            return;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
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
                mover.MoveToSurfacePoint(hits[i].point);
            }

            return;
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

    private bool IsPointerOverUI(int fingerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return fingerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }
}
