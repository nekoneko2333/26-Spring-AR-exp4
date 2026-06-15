using TMPro;
using UnityEditor;
using UnityEngine;

public static class ARBookGameShellSetup
{
    private const string ShellName = "ARBookGameShell";

    [MenuItem("ARBook/工具/创建或更新完整游戏UI外壳")]
    public static void CreateOrUpdateShell()
    {
        GameObject shellObject = GameObject.Find(ShellName);
        if (shellObject == null)
        {
            shellObject = new GameObject(ShellName);
            Undo.RegisterCreatedObjectUndo(shellObject, "Create ARBook Game Shell");
        }
        else
        {
            Undo.RecordObject(shellObject, "Update ARBook Game Shell");
        }

        ARBookGameShellController controller =
            shellObject.GetComponent<ARBookGameShellController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<ARBookGameShellController>(shellObject);
        }

        BindSceneReferences(controller);

        if (controller.companions == null || controller.companions.Length == 0)
        {
            controller.ResetCatalogToDefault();
        }

        AutoBindCompanionAssets(controller);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(shellObject);
        Debug.Log("ARBookGameShell 已创建/更新。运行游戏后会显示封面、常驻HUD、背包和陪伴模式。");
    }

    [MenuItem("ARBook/工具/转换为地图独立收服流程")]
    public static void ConvertToIndependentMapFlow()
    {
        ARBookChapterCompletionTrigger[] triggers =
            UnityEngine.Object.FindObjectsOfType<ARBookChapterCompletionTrigger>(true);
        for (int i = 0; i < triggers.Length; i++)
        {
            ARBookChapterCompletionTrigger trigger = triggers[i];
            if (trigger == null)
            {
                continue;
            }

            Undo.RecordObject(trigger, "Convert To Independent Map Flow");
            trigger.requireCompletedChapters = false;
            trigger.useIndependentMapCompleteDialogue = true;
            trigger.requiredCompletedChapterIds = new int[0];
            if (!string.IsNullOrWhiteSpace(trigger.completeDialogue) &&
                trigger.completeDialogue.Contains("Open Chapter"))
            {
                trigger.completeDialogue = $"地图 {trigger.chapterId} 探索完成。";
            }

            EditorUtility.SetDirty(trigger);
        }

        Debug.Log($"已转换 {triggers.Length} 个章节完成触发器为地图独立流程。");
    }

    private static void BindSceneReferences(ARBookGameShellController controller)
    {
        if (controller.collectionManager == null)
        {
            controller.collectionManager =
                UnityEngine.Object.FindObjectOfType<ARBookCollectionManager>(true);
        }

        if (controller.chapterProgress == null)
        {
            controller.chapterProgress =
                UnityEngine.Object.FindObjectOfType<ARBookChapterProgress>(true);
        }

        if (controller.progressResetter == null)
        {
            controller.progressResetter =
                UnityEngine.Object.FindObjectOfType<ARBookDebugProgressResetter>(true);
        }

        if (controller.chapterHudController == null)
        {
            controller.chapterHudController =
                UnityEngine.Object.FindObjectOfType<ARBookChapterHUDController>(true);
        }

        if (controller.chineseFont == null)
        {
            TMP_Text text = UnityEngine.Object.FindObjectOfType<TMP_Text>(true);
            if (text != null)
            {
                controller.chineseFont = text.font;
            }
        }

        if (controller.companionPlacementRoot == null &&
            Camera.main != null)
        {
            controller.companionPlacementRoot = Camera.main.transform;
        }
    }

    private static void AutoBindCompanionAssets(ARBookGameShellController controller)
    {
        if (controller == null || controller.companions == null)
        {
            return;
        }

        ARBookInteractable[] interactables =
            UnityEngine.Object.FindObjectsOfType<ARBookInteractable>(true);
        int imageCount = 0;
        int modelCount = 0;

        for (int i = 0; i < controller.companions.Length; i++)
        {
            ARBookGameShellController.CompanionDefinition companion =
                controller.companions[i];
            if (companion == null || string.IsNullOrWhiteSpace(companion.captureId))
            {
                continue;
            }

            if (companion.portraitTexture == null && companion.portrait == null)
            {
                Texture2D texture = FindVuforiaTexture(companion.captureId);
                if (texture != null)
                {
                    companion.portraitTexture = texture;
                    imageCount++;
                }
            }

            if (companion.sceneObject == null && companion.companionPrefab == null)
            {
                GameObject model = FindInteractableModel(interactables, companion.captureId);
                if (model != null)
                {
                    companion.sceneObject = model;
                    modelCount++;
                }
            }
        }

        Debug.Log(
            $"Companions 自动绑定完成：图片 {imageCount} 个，模型源 {modelCount} 个。");
    }

    private static Texture2D FindVuforiaTexture(string captureId)
    {
        string[] aliases = GetImageAliases(captureId);
        string[] guids = AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { "Assets/Editor/Vuforia/ImageTargetTextures/mcfAR" });

        for (int aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
        {
            string alias = aliases[aliasIndex].ToLowerInvariant();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path)
                    .ToLowerInvariant();
                if (!fileName.Contains(alias))
                {
                    continue;
                }

                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        return null;
    }

    private static string[] GetImageAliases(string captureId)
    {
        switch (captureId)
        {
            case "Talonflame":
                return new[] { "Talonflame", "GalarianZapdos", "zapdos" };
            case "Zygarde10":
                return new[] { "Zygarde", "Zygarde10", "zarude" };
            case "ElectrodeHisuian":
                return new[] { "Electrode", "HisuianElectrode" };
            default:
                return new[] { captureId };
        }
    }

    private static GameObject FindInteractableModel(
        ARBookInteractable[] interactables,
        string captureId)
    {
        if (interactables == null)
        {
            return null;
        }

        for (int i = 0; i < interactables.Length; i++)
        {
            ARBookInteractable interactable = interactables[i];
            if (interactable == null ||
                !interactable.canBeCaptured ||
                interactable.captureId != captureId)
            {
                continue;
            }

            if (interactable.presentationModelRoot != null)
            {
                return interactable.presentationModelRoot;
            }

            Renderer renderer = interactable.GetComponentInChildren<Renderer>(true);
            if (renderer != null)
            {
                return renderer.gameObject;
            }

            return interactable.gameObject;
        }

        return null;
    }

    [MenuItem("ARBook/工具/选中完整游戏UI外壳")]
    public static void SelectShell()
    {
        GameObject shellObject = GameObject.Find(ShellName);
        if (shellObject == null)
        {
            Debug.LogWarning("场景中还没有 ARBookGameShell。请先运行“创建或更新完整游戏UI外壳”。");
            return;
        }

        Selection.activeGameObject = shellObject;
        EditorGUIUtility.PingObject(shellObject);
    }
}
