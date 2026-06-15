using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ARBookPresentationCameraRig : MonoBehaviour
{
    public Camera stageCamera;
    public Transform lookTarget;
    public Vector3 lookOffset = new Vector3(0f, 1f, 0f);
    [Min(0.01f)] public float radius = 5f;
    public float height = 1.5f;
    public float startAngle = -180f;
    public float endAngle = 180f;
    [Min(0.01f)] public float orbitDuration = 2.5f;
    [Header("Battle Intro")]
    public Transform introTarget;
    [Min(0.1f)] public float introRadius = 1.6f;
    public float introStartAngle = 0f;
    public float introEndAngle = 180f;
    [Range(0f, 1f)] public float introStartHeight = 0.08f;
    [Range(0f, 1f)] public float introEndHeight = 0.92f;
    [Min(0.01f)] public float pullBackDuration = 0.9f;
    public AnimationCurve orbitCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public UnityEvent onOrbitCompleted;

    private Coroutine orbitRoutine;

    [ContextMenu("Play Orbit")]
    public void PlayOrbit()
    {
        if (orbitRoutine != null)
        {
            StopCoroutine(orbitRoutine);
        }

        orbitRoutine = StartCoroutine(BattleIntroRoutine());
    }

    public void SetIntroTarget(Transform target)
    {
        introTarget = target;
    }

    public IEnumerator PlayBattleIntro()
    {
        if (orbitRoutine != null)
        {
            StopCoroutine(orbitRoutine);
            orbitRoutine = null;
        }

        yield return BattleIntroRoutine();
    }

    public void SnapToEnd()
    {
        PlaceCamera(endAngle);
    }

    private IEnumerator BattleIntroRoutine()
    {
        if (stageCamera == null)
        {
            yield break;
        }

        Bounds introBounds;
        if (!TryGetBounds(introTarget, out introBounds))
        {
            introBounds = new Bounds(
                introTarget != null ? introTarget.position : Vector3.zero,
                new Vector3(1f, 2f, 1f));
        }

        Vector3 orbitCenter = new Vector3(
            introBounds.center.x,
            0f,
            introBounds.center.z);
        Vector3 introForward = introTarget != null
            ? Vector3.ProjectOnPlane(
                introTarget.forward,
                Vector3.up).normalized
            : Vector3.forward;
        if (introForward.sqrMagnitude <= 0.0001f)
        {
            introForward = Vector3.forward;
        }

        Vector3 introRight = Vector3.Cross(
            Vector3.up,
            introForward).normalized;
        float elapsed = 0f;
        while (elapsed < orbitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / orbitDuration);
            float curved = orbitCurve != null
                ? orbitCurve.Evaluate(normalized)
                : normalized;
            float angle = Mathf.Lerp(
                introStartAngle,
                introEndAngle,
                curved);
            float targetY = Mathf.Lerp(
                introBounds.min.y +
                    introBounds.size.y * introStartHeight,
                introBounds.min.y +
                    introBounds.size.y * introEndHeight,
                curved);
            PlaceIntroCamera(
                orbitCenter,
                targetY,
                angle,
                introForward,
                introRight);
            yield return null;
        }

        Vector3 finalPosition;
        Quaternion finalRotation;
        GetFinalPose(out finalPosition, out finalRotation);

        Vector3 pullStartPosition = stageCamera.transform.position;
        Quaternion pullStartRotation = stageCamera.transform.rotation;
        elapsed = 0f;
        while (elapsed < pullBackDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(
                elapsed / pullBackDuration);
            float curved = Mathf.SmoothStep(0f, 1f, normalized);
            stageCamera.transform.position = Vector3.Lerp(
                pullStartPosition,
                finalPosition,
                curved);
            stageCamera.transform.rotation = Quaternion.Slerp(
                pullStartRotation,
                finalRotation,
                curved);
            yield return null;
        }

        stageCamera.transform.SetPositionAndRotation(
            finalPosition,
            finalRotation);
        orbitRoutine = null;
        onOrbitCompleted?.Invoke();
    }

    private void PlaceIntroCamera(
        Vector3 orbitCenter,
        float targetY,
        float angle,
        Vector3 introForward,
        Vector3 introRight)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 lookPoint = new Vector3(
            orbitCenter.x,
            targetY,
            orbitCenter.z);
        Vector3 horizontalOffset =
            introForward * (Mathf.Cos(radians) * introRadius) +
            introRight * (Mathf.Sin(radians) * introRadius);
        Vector3 position =
            lookPoint + horizontalOffset + Vector3.down * 0.12f;
        stageCamera.transform.position = position;
        stageCamera.transform.rotation = Quaternion.LookRotation(
            lookPoint - position,
            Vector3.up);
    }

    private static bool TryGetBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    private void GetFinalPose(
        out Vector3 position,
        out Quaternion rotation)
    {
        Vector3 target = lookTarget != null
            ? lookTarget.position + lookOffset
            : Vector3.zero;
        float radians = endAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Sin(radians) * radius,
            height,
            Mathf.Cos(radians) * radius);
        position = target + offset;
        rotation = Quaternion.LookRotation(target - position, Vector3.up);
    }

    private void PlaceCamera(float angle)
    {
        if (stageCamera == null || lookTarget == null)
        {
            return;
        }

        Vector3 target = lookTarget.position + lookOffset;
        float radians = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Sin(radians) * radius,
            height,
            Mathf.Cos(radians) * radius);

        stageCamera.transform.position = target + offset;
        stageCamera.transform.rotation =
            Quaternion.LookRotation(target - stageCamera.transform.position);
    }
}
