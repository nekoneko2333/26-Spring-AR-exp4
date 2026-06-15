using System.Collections;
using UnityEngine;

public class ARBookFrozenBackground : MonoBehaviour
{
    public Renderer backgroundRenderer;
    public Camera presentationCamera;
    [Min(0.1f)] public float backgroundDistance = 20f;
    public bool fitQuadToCamera = true;

    private Texture2D capturedTexture;

    public IEnumerator Capture()
    {
        yield return new WaitForEndOfFrame();

        Release();
        capturedTexture = ScreenCapture.CaptureScreenshotAsTexture();

        if (backgroundRenderer != null)
        {
            backgroundRenderer.material.mainTexture = capturedTexture;
        }

        if (fitQuadToCamera)
        {
            FitBackgroundQuad();
        }
    }

    public void FitBackgroundQuad()
    {
        if (backgroundRenderer == null || presentationCamera == null)
        {
            return;
        }

        Transform quad = backgroundRenderer.transform;
        quad.SetParent(presentationCamera.transform, false);
        quad.localPosition = new Vector3(0f, 0f, backgroundDistance);
        quad.localRotation = Quaternion.identity;

        float height = 2f * backgroundDistance *
                       Mathf.Tan(presentationCamera.fieldOfView * 0.5f *
                                 Mathf.Deg2Rad);
        float width = height * presentationCamera.aspect;
        quad.localScale = new Vector3(width, height, 1f);
    }

    public void Release()
    {
        if (capturedTexture == null)
        {
            return;
        }

        if (backgroundRenderer != null)
        {
            backgroundRenderer.material.mainTexture = null;
        }

        Destroy(capturedTexture);
        capturedTexture = null;
    }

    private void OnDestroy()
    {
        Release();
    }
}
