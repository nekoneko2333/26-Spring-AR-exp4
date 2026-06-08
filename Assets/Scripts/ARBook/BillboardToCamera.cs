using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    public bool onlyRotateAroundY = true;

    private void LateUpdate()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 lookPosition = mainCamera.transform.position;
        if (onlyRotateAroundY)
        {
            lookPosition.y = transform.position.y;
        }

        Vector3 direction = lookPosition - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
