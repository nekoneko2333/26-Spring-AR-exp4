using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ImageTargetChildScaleTool
{
    private const float TargetScale = 0.28f;

    [MenuItem("Tools/Image Targets/Set Only Child Scale To 0.28")]
    public static void SetOnlyChildScale()
    {
        var changedCount = 0;
        var skippedCount = 0;
        for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            var scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                var transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var transform in transforms)
                {
                    var gameObject = transform.gameObject;
                    if (!IsImageTarget(gameObject) || HasCameraOrLight(gameObject))
                    {
                        continue;
                    }

                    if (transform.childCount != 1)
                    {
                        skippedCount++;
                        Debug.LogWarning($"Skipped {GetHierarchyPath(transform)} because it has {transform.childCount} direct child object(s).");
                        continue;
                    }

                    var onlyChild = transform.GetChild(0);
                    if (HasCameraOrLight(onlyChild.gameObject))
                    {
                        skippedCount++;
                        Debug.LogWarning($"Skipped {GetHierarchyPath(onlyChild)} because the only child is a Camera or Light.");
                        continue;
                    }

                    Undo.RecordObject(onlyChild, "Set ImageTarget Only Child Scale");
                    onlyChild.localScale *= TargetScale;
                    EditorUtility.SetDirty(onlyChild);
                    EditorSceneManager.MarkSceneDirty(scene);
                    changedCount++;
                }
            }
        }

        Debug.Log($"Multiplied ImageTarget only-child localScale by {TargetScale}. Changed: {changedCount}, skipped: {skippedCount}.");
    }

    private static bool IsImageTarget(GameObject gameObject)
    {
        if (gameObject.name.ToLowerInvariant().Contains("imagetarget"))
        {
            return true;
        }

        foreach (var component in gameObject.GetComponents<Component>())
        {
            if (component == null)
            {
                continue;
            }

            var typeName = component.GetType().Name.ToLowerInvariant();
            if (typeName.Contains("imagetarget") || typeName.Contains("observerbehaviour"))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasCameraOrLight(GameObject gameObject)
    {
        return gameObject.GetComponent<Camera>() != null || gameObject.GetComponent<Light>() != null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = $"{transform.name}/{path}";
        }

        return path;
    }
}
