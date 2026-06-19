using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ARBookCompanionReturnHomeButtonBinder
{
    [MenuItem("ARBook/Tools/Bind Companion Return Home Button")]
    public static void Bind()
    {
        ARBookGameShellController controller =
            Object.FindObjectOfType<ARBookGameShellController>(true);
        if (controller == null)
        {
            Debug.LogWarning("ARBookGameShellController was not found. Return-home button binding skipped.");
            return;
        }

        Button button = FindButton(controller.hudRoot, "CompanionReturnHomeButton") ??
                        FindButton(controller.companionRoot, "CompanionReturnHomeButton") ??
                        FindButton(controller.generatedRoot, "CompanionReturnHomeButton");

        if (button == null)
        {
            Debug.LogWarning("CompanionReturnHomeButton was not found. Create the real UI Button first, then run this menu again.");
            return;
        }

        Undo.RecordObject(controller, "Bind companion return-home button");
        Undo.RecordObject(button, "Bind companion return-home button");
        controller.companionReturnHomeButton = button;
        BindShowHome(button, controller);
        PlaceBottomRight(button);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(button.transform);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log("CompanionReturnHomeButton is bound to ShowHome and anchored to the bottom-right corner.");
    }

    private static void BindShowHome(Button button, ARBookGameShellController controller)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == controller &&
                button.onClick.GetPersistentMethodName(i) == nameof(ARBookGameShellController.ShowHome))
            {
                return;
            }
        }

        UnityEventTools.AddPersistentListener(button.onClick, controller.ShowHome);
    }

    private static void PlaceBottomRight(Button button)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        Undo.RecordObject(rect, "Place companion return-home button");
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(190f, 68f);
        rect.anchoredPosition = new Vector2(-34f, 34f);
    }

    private static Button FindButton(Transform root, string name)
    {
        Transform transform = FindDescendant(root, name);
        return transform != null ? transform.GetComponent<Button>() : null;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendant(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
