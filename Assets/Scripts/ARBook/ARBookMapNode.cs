using UnityEngine;
using UnityEngine.Events;

public class ARBookMapNode : MonoBehaviour
{
    public int nodeIndex;
    public string nodeName;
    public bool isUnlocked = true;
    public UnityEvent onNodeReached;
    public ARBookPlayerMover playerMover;

    public void OnTapped()
    {
        if (!isUnlocked)
        {
            return;
        }

        if (playerMover == null)
        {
            playerMover = FindPlayerMoverInSameRoot();
        }

        if (playerMover == null)
        {
            Debug.LogWarning($"No ARBookPlayerMover found for node: {GetDisplayName()}");
            return;
        }

        playerMover.MoveToNode(this);
    }

    private string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(nodeName) ? gameObject.name : nodeName;
    }

    private ARBookPlayerMover FindPlayerMoverInSameRoot()
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            ARBookPlayerMover mover = parent.GetComponentInChildren<ARBookPlayerMover>(true);
            if (mover != null)
            {
                return mover;
            }

            parent = parent.parent;
        }

        return FindObjectOfType<ARBookPlayerMover>();
    }
}
