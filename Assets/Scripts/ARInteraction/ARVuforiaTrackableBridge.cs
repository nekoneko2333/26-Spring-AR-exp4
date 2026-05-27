using UnityEngine;
using Vuforia;

[RequireComponent(typeof(TrackableBehaviour))]
public class ARVuforiaTrackableBridge : MonoBehaviour
{
    public ARTrackableEntity entity;

    private TrackableBehaviour trackableBehaviour;
    private bool wasTracked;

    private void Reset()
    {
        entity = GetComponent<ARTrackableEntity>();
    }

    private void Awake()
    {
        trackableBehaviour = GetComponent<TrackableBehaviour>();

        if (entity == null)
        {
            entity = GetComponent<ARTrackableEntity>();
        }
    }

    private void Start()
    {
        wasTracked = IsCurrentlyTracked();
    }

    private void Update()
    {
        if (entity == null || trackableBehaviour == null)
        {
            return;
        }

        var isTracked = IsCurrentlyTracked();
        if (isTracked == wasTracked)
        {
            return;
        }

        wasTracked = isTracked;
        if (isTracked)
        {
            entity.NotifyTargetFound();
        }
        else
        {
            entity.NotifyTargetLost();
        }
    }

    private bool IsCurrentlyTracked()
    {
        var property = trackableBehaviour.GetType().GetProperty("CurrentStatus");
        if (property == null)
        {
            return false;
        }

        var status = property.GetValue(trackableBehaviour, null);
        return IsTrackedStatusName(status != null ? status.ToString() : string.Empty);
    }

    private static bool IsTrackedStatusName(string status)
    {
        return status == "DETECTED" ||
               status == "TRACKED" ||
               status == "EXTENDED_TRACKED";
    }
}
