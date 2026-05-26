using UnityEngine;

public class ARFitToImageTarget : MonoBehaviour
{
    public Transform contentRoot;
    public float targetWidth = 25f;
    public float targetHeight = 39.26896f;
    [Range(0.05f, 1f)] public float fillRatio = 0.35f;
    public float liftFromTarget = 0.2f;
    public bool fitOnStart = true;

    private void Start()
    {
        if (fitOnStart)
        {
            Fit();
        }
    }

    [ContextMenu("Fit Content To Image Target")]
    public void Fit()
    {
        if (contentRoot == null)
        {
            contentRoot = transform;
        }

        Renderer[] renderers = contentRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float largestSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largestSide <= 0.0001f)
        {
            return;
        }

        float desiredSize = Mathf.Min(targetWidth, targetHeight) * fillRatio;
        float scaleFactor = desiredSize / largestSide;
        contentRoot.localScale *= scaleFactor;

        renderers = contentRoot.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 offsetToCenter = contentRoot.position - bounds.center;
        contentRoot.position += offsetToCenter;

        renderers = contentRoot.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float bottomOffset = liftFromTarget - bounds.min.y;
        contentRoot.position += Vector3.up * bottomOffset;
    }
}
