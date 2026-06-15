using UnityEngine;

public class ARBookWalkableColliderInstaller : MonoBehaviour
{
    public int walkableLayer = 6;
    public bool includeInactive = true;

    private void OnEnable()
    {
        Install();
    }

    [ContextMenu("Install Walkable Colliders")]
    public void Install()
    {
        MeshFilter[] meshFilters =
            GetComponentsInChildren<MeshFilter>(includeInactive);

        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            GameObject target = meshFilter.gameObject;
            target.layer = walkableLayer;

            if (target.GetComponent<Collider>() != null)
            {
                continue;
            }

            MeshCollider collider = target.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.sharedMesh;
            collider.convex = false;
        }
    }
}
