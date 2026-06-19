using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ARBookCompanionStaticSlotOverlayInstaller
{
    [MenuItem("ARBook/Tools/Bind Existing Companion Slot UI")]
    public static void BindExistingUi()
    {
        ARBookGameShellController controller =
            Object.FindObjectOfType<ARBookGameShellController>(true);
        if (controller == null)
        {
            Debug.LogWarning("ARBookGameShellController was not found. Companion slot UI binding skipped.");
            return;
        }

        ARBookCompanionStaticSlotOverlay overlay =
            controller.GetComponent<ARBookCompanionStaticSlotOverlay>();
        if (overlay == null)
        {
            Undo.RecordObject(controller.gameObject, "Bind Companion Static Slot UI");
            overlay = Undo.AddComponent<ARBookCompanionStaticSlotOverlay>(controller.gameObject);
        }
        else
        {
            Undo.RecordObject(overlay, "Configure Companion Static Slot Overlay");
        }

        overlay.controller = controller;
        overlay.carryLimit = 2;
        overlay.BindExistingUiByName();
        EditorUtility.SetDirty(overlay);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log("Existing companion slot UI references bound. No UI objects were created.");
    }
}
