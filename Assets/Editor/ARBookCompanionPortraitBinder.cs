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
            Debug.LogWarning("ARBookGameShellController was not found. Portrait binding skipped.");
            return;
        }

        if (controller.companions == null || controller.companions.Length == 0)
        {
            Debug.LogWarning("ARBookGameShellController.companions is empty. Portrait binding skipped.");
            return;
        }

        Dictionary<string, Sprite> sprites = LoadSprites();
        Debug.Log($"Pokemon portrait binding: found {sprites.Count} CaptureID PNG sprites in {SpriteFolder}.");

        int changed = 0;
        int alreadyBound = 0;
        int missing = 0;
        Undo.RecordObject(controller, "Bind Pokemon Portraits");

        for (int i = 0; i < controller.companions.Length; i++)
        {
            ARBookGameShellController.CompanionDefinition companion = controller.companions[i];
            if (companion == null || string.IsNullOrWhiteSpace(companion.captureId))
            {
                continue;
            }

            if (!sprites.TryGetValue(companion.captureId, out Sprite sprite))
            {
                missing++;
                Debug.LogWarning($"Pokemon portrait missing: captureId={companion.captureId}, expected {SpriteFolder}/{companion.captureId}.png");
                continue;
            }

            if (companion.portrait == sprite)
            {
                alreadyBound++;
                continue;
            }

            companion.portrait = sprite;
            changed++;
            Debug.Log($"Pokemon portrait bound: {companion.captureId} -> {sprite.name}");
        }

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log($"Pokemon portrait binding complete. changed={changed}, alreadyBound={alreadyBound}, missing={missing}");
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
                Debug.LogWarning($"Could not load PNG as Sprite: {path}");
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
