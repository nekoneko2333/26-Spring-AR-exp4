using UnityEditor;
using UnityEngine;

public static class BatchMeshColliderTool
{
    [MenuItem("ARBook/工具/给选中物体批量添加 Mesh Collider")]
    private static void AddMeshColliders()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("请先在层级窗口中选择一个或多个物体。");
            return;
        }

        int addedCount = 0;
        int existingCount = 0;
        int missingMeshCount = 0;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("批量添加 Mesh Collider");

        foreach (GameObject selectedObject in selectedObjects)
        {
            MeshFilter[] meshFilters =
                selectedObject.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh == null)
                {
                    missingMeshCount++;
                    continue;
                }

                MeshCollider existingCollider =
                    meshFilter.GetComponent<MeshCollider>();
                if (existingCollider != null)
                {
                    existingCount++;
                    continue;
                }

                MeshCollider meshCollider =
                    Undo.AddComponent<MeshCollider>(meshFilter.gameObject);
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;

                EditorUtility.SetDirty(meshFilter.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    meshCollider);
                addedCount++;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log(
            $"批量添加 Mesh Collider 完成：新增 {addedCount} 个，" +
            $"跳过已有 {existingCount} 个，无网格 {missingMeshCount} 个。");
    }

    [MenuItem(
        "ARBook/工具/给选中物体批量添加 Mesh Collider",
        true)]
    private static bool ValidateAddMeshColliders()
    {
        return Selection.gameObjects != null &&
               Selection.gameObjects.Length > 0;
    }
}
