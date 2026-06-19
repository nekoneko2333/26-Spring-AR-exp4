using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ARBookCompanionPortraitBinder
{
    private const string SpriteFolder = "Assets/UI/pokemonpic";

    [MenuItem("ARBook/Tools/Bind Companion Portraits From CaptureID PNGs")]
    public static void BindCompanionPortraits()
    {
        ARBookGameShellController controller =
            UnityEngine.Object.FindObjectOfType<ARBookGameShellController>(true);
        if (controller == null)
        {
            Debug.LogWarning("没有找到 ARBookGameShellController，无法绑定宝可梦图片。");
            return;
        }

        if (controller.companions == null || controller.companions.Length == 0)
        {
            Debug.LogWarning("ARBookGameShellController companions 为空，无法绑定宝可梦图片。");
            return;
        }

        Dictionary<string, Sprite> sprites = LoadSprites();
        Debug.Log($"宝可梦图片绑定：从 {SpriteFolder} 找到 {sprites.Count} 张 CaptureID 图片。");

        int changed = 0;
        int alreadyBound = 0;
        int missing = 0;
        Undo.RecordObject(controller, "Bind Pokemon Portraits");

        for (int i = 0; i < controller.companions.Length; i++)
        {
            ARBookGameShellController.CompanionDefinition companion =
                controller.companions[i];
            if (companion == null || string.IsNullOrWhiteSpace(companion.captureId))
            {
                continue;
            }

            if (!sprites.TryGetValue(companion.captureId, out Sprite sprite))
            {
                missing++;
                Debug.LogWarning(
                    $"宝可梦图片未匹配：captureId={companion.captureId}。需要文件 {SpriteFolder}/{companion.captureId}.png");
                continue;
            }

            if (companion.portrait == sprite && companion.portraitTexture == null)
            {
                alreadyBound++;
                continue;
            }

            companion.portrait = sprite;
            companion.portraitTexture = null;
            changed++;
            Debug.Log($"宝可梦图片已绑定：{companion.captureId} -> {sprite.name}");
        }

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log($"宝可梦图片绑定完成：更新 {changed} 个，已绑定 {alreadyBound} 个，未匹配 {missing} 个。");
    }

    private static Dictionary<string, Sprite> LoadSprites()
    {
        Dictionary<string, Sprite> sprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.Equals(
                System.IO.Path.GetExtension(path),
                ".png",
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            EnsureSpriteImport(path);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"无法作为 Sprite 加载：{path}");
                continue;
            }

            string captureId = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrWhiteSpace(captureId) && !sprites.ContainsKey(captureId))
            {
                sprites.Add(captureId, sprite);
            }
        }

        return sprites;
    }

    private static void EnsureSpriteImport(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }
}
