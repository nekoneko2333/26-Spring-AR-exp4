using UnityEngine;
using UnityEngine.Events;

public class ARBookProximityTrigger : MonoBehaviour
{
    public ARBookPlayerMover playerMover;
    public float triggerRadius = 1f;
    public bool triggerOnlyOnce = true;
    public UnityEvent onPlayerEntered;

    private bool wasInside;
    private bool hasTriggered;

    private void Update()
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        ARBookPlayerMover activeMover = ResolvePlayerMover();
        if (activeMover == null)
        {
            return;
        }

        bool isInside = activeMover.GetDistanceTo(transform.position) <= triggerRadius;

        if (isInside && !wasInside)
        {
            hasTriggered = true;
            onPlayerEntered?.Invoke();
        }

        wasInside = isInside;
    }

    private ARBookPlayerMover ResolvePlayerMover()
    {
        if (playerMover != null &&
            playerMover.isActiveAndEnabled &&
            playerMover.gameObject.activeInHierarchy)
        {
            return playerMover;
        }

        Transform parent = transform.parent;
        while (parent != null)
        {
            ARBookPlayerMover mover = parent.GetComponentInChildren<ARBookPlayerMover>(true);
            if (mover != null &&
                mover.isActiveAndEnabled &&
                mover.gameObject.activeInHierarchy)
            {
                playerMover = mover;
                return playerMover;
            }

            parent = parent.parent;
        }

        ARBookPlayerMover[] movers = FindObjectsOfType<ARBookPlayerMover>(true);
        for (int i = 0; i < movers.Length; i++)
        {
            if (movers[i] != null &&
                movers[i].isActiveAndEnabled &&
                movers[i].gameObject.activeInHierarchy)
            {
                playerMover = movers[i];
                return playerMover;
            }
        }

        return null;
    }
}
