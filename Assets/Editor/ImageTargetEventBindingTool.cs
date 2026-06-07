using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ImageTargetEventBindingTool
{
    [MenuItem("Tools/Image Targets/Bind Handler And First Child Events")]
    [MenuItem("Tools/Image Targets/Bind Events And Hide First Children")]
    [MenuItem("Tools/Image Targets/Bind First Child SetActive Events")]
    public static void BindHandlerAndFirstChildEvents()
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
                    if (!IsImageTarget(transform.gameObject))
                    {
                        continue;
                    }

                    if (transform.childCount == 0)
                    {
                        skippedCount++;
                        Debug.LogWarning($"Skipped {GetHierarchyPath(transform)} because it has no child object.");
                        continue;
                    }

                    var firstChild = transform.GetChild(0).gameObject;
                    var handler = transform.GetComponent<DefaultObserverEventHandler>();
                    if (handler == null)
                    {
                        Undo.AddComponent<DefaultObserverEventHandler>(transform.gameObject);
                        handler = transform.GetComponent<DefaultObserverEventHandler>();
                    }

                    Undo.RecordObject(handler, "Bind ImageTarget SetActive Events");
                    Undo.RecordObject(firstChild, "Hide ImageTarget First Child");
                    handler.StatusFilter = DefaultObserverEventHandler.TrackingStatusFilter.Tracked;
                    ReplaceSetActiveListener(handler, "OnTargetFound", firstChild, true);
                    ReplaceSetActiveListener(handler, "OnTargetLost", firstChild, false);
                    firstChild.SetActive(false);
                    EditorUtility.SetDirty(handler);
                    EditorUtility.SetDirty(firstChild);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    changedCount++;
                }
            }
        }

        Debug.Log($"Bound DefaultObserverEventHandler and first-child SetActive events. Changed: {changedCount}, skipped: {skippedCount}.");
    }

    [MenuItem("Tools/Image Targets/Hide First Children Now")]
    public static void HideFirstChildrenNow()
    {
        SetFirstChildrenActive(false);
    }

    [MenuItem("Tools/Image Targets/Show First Children Now")]
    public static void ShowFirstChildrenNow()
    {
        SetFirstChildrenActive(true);
    }

    private static void ReplaceSetActiveListener(DefaultObserverEventHandler handler, string eventName, GameObject target, bool active)
    {
        var serializedObject = new SerializedObject(handler);
        serializedObject.Update();

        var eventProperty = serializedObject.FindProperty(eventName);
        var callsProperty = eventProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");

        for (var i = callsProperty.arraySize - 1; i >= 0; i--)
        {
            var callProperty = callsProperty.GetArrayElementAtIndex(i);
            var methodProperty = callProperty.FindPropertyRelative("m_MethodName");
            if (methodProperty.stringValue == nameof(GameObject.SetActive))
            {
                callsProperty.DeleteArrayElementAtIndex(i);
            }
        }

        callsProperty.InsertArrayElementAtIndex(callsProperty.arraySize);
        var newCallProperty = callsProperty.GetArrayElementAtIndex(callsProperty.arraySize - 1);
        newCallProperty.FindPropertyRelative("m_Target").objectReferenceValue = target;
        newCallProperty.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = "UnityEngine.GameObject, UnityEngine";
        newCallProperty.FindPropertyRelative("m_MethodName").stringValue = nameof(GameObject.SetActive);
        newCallProperty.FindPropertyRelative("m_Mode").intValue = 6;

        var argumentsProperty = newCallProperty.FindPropertyRelative("m_Arguments");
        argumentsProperty.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = null;
        argumentsProperty.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = "UnityEngine.Object, UnityEngine";
        argumentsProperty.FindPropertyRelative("m_IntArgument").intValue = 0;
        argumentsProperty.FindPropertyRelative("m_FloatArgument").floatValue = 0f;
        argumentsProperty.FindPropertyRelative("m_StringArgument").stringValue = string.Empty;
        argumentsProperty.FindPropertyRelative("m_BoolArgument").boolValue = active;

        newCallProperty.FindPropertyRelative("m_CallState").intValue = 2;
        serializedObject.ApplyModifiedProperties();
    }

    private static void SetFirstChildrenActive(bool active)
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
                var handlers = root.GetComponentsInChildren<DefaultObserverEventHandler>(true);
                foreach (var handler in handlers)
                {
                    var transform = handler.transform;
                    if (transform.childCount == 0)
                    {
                        skippedCount++;
                        continue;
                    }

                    var firstChild = transform.GetChild(0).gameObject;

                    Undo.RecordObject(firstChild, active ? "Show ImageTarget First Child" : "Hide ImageTarget First Child");
                    firstChild.SetActive(active);
                    EditorUtility.SetDirty(firstChild);
                    EditorSceneManager.MarkSceneDirty(scene);
                    changedCount++;
                }
            }
        }

        Debug.Log($"{(active ? "Showed" : "Hid")} first child for ImageTargets. Changed: {changedCount}, skipped: {skippedCount}.");
    }

    private static bool IsImageTarget(GameObject gameObject)
    {
        foreach (var component in gameObject.GetComponents<Component>())
        {
            if (component == null)
            {
                continue;
            }

            var typeName = component.GetType().Name;
            if (typeName.Contains("ImageTarget") || typeName.Contains("ObserverBehaviour"))
            {
                return true;
            }
        }

        return false;
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
