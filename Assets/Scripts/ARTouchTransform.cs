using UnityEngine;

public class ARTouchTransform : MonoBehaviour
{
    public float rotateSpeed = 0.25f;
    public float mouseRotateSpeed = 4f;
    public float pinchScaleSpeed = 0.01f;
    public float minScale = 0.2f;
    public float maxScale = 2.5f;

    private float initialTouchDistance;
    private Vector3 initialScale;

    private void Update()
    {
        HandleMouse();
        HandleTouch();
    }

    private void HandleMouse()
    {
        if (Input.touchCount > 0)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            float rotation = -Input.GetAxis("Mouse X") * mouseRotateSpeed;
            transform.Rotate(0f, rotation, 0f, Space.World);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            ApplyScale(transform.localScale + Vector3.one * scroll * 0.08f);
        }
    }

    private void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                transform.Rotate(0f, -touch.deltaPosition.x * rotateSpeed, 0f, Space.World);
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);

            if (first.phase == TouchPhase.Began || second.phase == TouchPhase.Began)
            {
                initialTouchDistance = Vector2.Distance(first.position, second.position);
                initialScale = transform.localScale;
                return;
            }

            float currentDistance = Vector2.Distance(first.position, second.position);
            float delta = currentDistance - initialTouchDistance;
            ApplyScale(initialScale + Vector3.one * delta * pinchScaleSpeed);
        }
    }

    private void ApplyScale(Vector3 targetScale)
    {
        float clamped = Mathf.Clamp(targetScale.x, minScale, maxScale);
        transform.localScale = Vector3.one * clamped;
    }
}
