using UnityEngine;

public class ARViewAlignmentCalibrator : MonoBehaviour
{
    public ARViewAlignmentChallenge challenge;
    public Transform alignmentTarget;
    public Camera referenceCamera;
    public bool logAngleInPlayMode;
    public float logInterval = 0.5f;

    private float nextLogTime;

    private void Reset()
    {
        challenge = GetComponent<ARViewAlignmentChallenge>();
        if (challenge != null)
        {
            alignmentTarget = challenge.alignmentTarget;
        }
    }

    private void Update()
    {
        if (!logAngleInPlayMode || Time.time < nextLogTime)
        {
            return;
        }

        LogCurrentAngle();
        nextLogTime = Time.time + Mathf.Max(0.1f, logInterval);
    }

    [ContextMenu("Use Current Camera View As Expected")]
    public void UseCurrentCameraViewAsExpected()
    {
        Camera cameraToUse = referenceCamera != null ? referenceCamera : Camera.main;
        if (cameraToUse == null)
        {
            Debug.LogWarning($"{name} could not find a camera to calibrate from.");
            return;
        }

        UseCameraViewAsExpected(cameraToUse.transform);
    }

    public void UseCameraViewAsExpected(Transform cameraTransform)
    {
        ResolveReferences();

        if (challenge == null || alignmentTarget == null || cameraTransform == null)
        {
            Debug.LogWarning($"{name} needs a challenge, an alignment target, and a camera.");
            return;
        }

        Vector3 worldDirection = (cameraTransform.position - alignmentTarget.position).normalized;
        if (worldDirection.sqrMagnitude <= 0.0001f)
        {
            Debug.LogWarning($"{name} camera is too close to the alignment target.");
            return;
        }

        challenge.expectedViewDirection =
            alignmentTarget.InverseTransformDirection(worldDirection).normalized;

        Debug.Log(
            $"{name} calibrated {challenge.name}: expectedViewDirection = " +
            $"{challenge.expectedViewDirection}");
    }

    [ContextMenu("Log Current Alignment Angle")]
    public void LogCurrentAngle()
    {
        ResolveReferences();

        Camera cameraToUse = referenceCamera != null ? referenceCamera : Camera.main;
        if (challenge == null || alignmentTarget == null || cameraToUse == null)
        {
            Debug.LogWarning($"{name} needs a challenge, an alignment target, and a camera.");
            return;
        }

        Vector3 currentDirection =
            (cameraToUse.transform.position - alignmentTarget.position).normalized;
        Vector3 expectedWorldDirection =
            alignmentTarget.TransformDirection(challenge.expectedViewDirection.normalized);
        float angle = Vector3.Angle(expectedWorldDirection, currentDirection);

        Debug.Log(
            $"{name} angle={angle:0.0}, tolerance={challenge.angleTolerance:0.0}, " +
            $"pass={angle <= challenge.angleTolerance}");
    }

    private void ResolveReferences()
    {
        if (challenge == null)
        {
            challenge = GetComponent<ARViewAlignmentChallenge>();
        }

        if (alignmentTarget == null && challenge != null)
        {
            alignmentTarget = challenge.alignmentTarget;
        }
    }

    private void OnDrawGizmosSelected()
    {
        ResolveReferences();

        if (challenge == null || alignmentTarget == null)
        {
            return;
        }

        Vector3 start = alignmentTarget.position;
        Vector3 direction =
            alignmentTarget.TransformDirection(challenge.expectedViewDirection.normalized);
        float length = Mathf.Max(0.25f, alignmentTarget.lossyScale.magnitude * 0.25f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(start, start + direction * length);
        Gizmos.DrawWireSphere(start + direction * length, length * 0.08f);
    }
}
