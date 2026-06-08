using UnityEngine;
using UnityEngine.Events;

public class ARBookMapNode : MonoBehaviour
{
    public int nodeIndex;
    public string nodeName;
    public bool isUnlocked = true;
    public UnityEvent onNodeReached;

    private ARBookPlayerMover playerMover;

    public void OnTapped()
    {
        if (!isUnlocked)
        {
            Debug.Log($"Map node locked: {GetDisplayName()}");
            return;
        }

        if (playerMover == null)
        {
            playerMover = FindObjectOfType<ARBookPlayerMover>();
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
}
