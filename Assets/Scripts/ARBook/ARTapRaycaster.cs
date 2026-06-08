using UnityEngine;
using UnityEngine.EventSystems;

public class ARTapRaycaster : MonoBehaviour
{
    public float raycastDistance = 100f;
    public LayerMask raycastLayers = ~0;
    public float tapCooldown = 0.15f;
    public bool interactImmediatelyOnModelTap;

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
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, raycastLayers))
        {
            return;
        }

        ARBookMapNode node = hit.collider.GetComponentInParent<ARBookMapNode>();
        if (node != null)
        {
            node.OnTapped();
            return;
        }

        ARBookInteractable interactable = hit.collider.GetComponentInParent<ARBookInteractable>();
        if (interactable != null)
        {
            if (interactImmediatelyOnModelTap)
            {
                interactable.Interact();
            }
        }
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
