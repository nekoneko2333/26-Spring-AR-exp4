using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ARBookCompanionPortraitBinder
{
    private const string TextureFolder =
        "Assets/Editor/Vuforia/ImageTargetTextures/mcfAR";

    [MenuItem("ARBook/Tools/Bind Companion Portraits From Vuforia Textures")]
    public static void BindCompanionPortraits()
    {
        ARBookGameShellController controller =
            UnityEngine.Object.FindObjectOfType<ARBookGameShellController>(true);
        if (controller == null)
        {
            Debug.LogWarning("没有找到 ARBookGameShellController，无法绑定陪伴图片。");
            return;
        }

        if (controller.companions == null || controller.companions.Length == 0)
        {
            Debug.LogWarning(
                "ARBookGameShellController companions 为空。为避免覆盖手动绑定的图片和模型，本工具不会自动重置目录。");
            return;
        }

        Dictionary<string, Texture2D> textures = LoadTextures();
        Debug.Log($"陪伴图片绑定：从 {TextureFolder} 找到 {textures.Count} 张可用图片。");

        int changed = 0;
        int alreadyBound = 0;
        int missing = 0;
        Undo.RecordObject(controller, "Bind Companion Portraits");

        for (int i = 0; i < controller.companions.Length; i++)
        {
            ARBookGameShellController.CompanionDefinition companion =
                controller.companions[i];
            if (companion == null)
            {
                continue;
            }

            Texture2D texture = FindTextureFor(companion, textures);
            if (texture == null)
            {
                missing++;
                Debug.LogWarning(
                    $"陪伴图片未匹配：captureId={companion.captureId}, imageTargetName={companion.imageTargetName}, displayName={companion.displayName}");
                continue;
            }

            if (companion.portraitTexture == texture)
            {
                alreadyBound++;
                continue;
            }

            companion.portraitTexture = texture;
            companion.portrait = null;
            changed++;
            Debug.Log($"陪伴图片已绑定：{companion.captureId} -> {texture.name}");
        }

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log($"陪伴模式图片绑定完成：更新 {changed} 个，已绑定 {alreadyBound} 个，未匹配 {missing} 个。");
    }

    private static Dictionary<string, Texture2D> LoadTextures()
    {
        Dictionary<string, Texture2D> textures =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                continue;
            }

            AddTexture(textures, texture.name, texture);
            AddTexture(textures, System.IO.Path.GetFileNameWithoutExtension(path), texture);
        }

        return textures;
    }

    private static void AddTexture(
        Dictionary<string, Texture2D> textures,
        string rawKey,
        Texture2D texture)
    {
        string key = Normalize(rawKey);
        if (!string.IsNullOrWhiteSpace(key) && !textures.ContainsKey(key))
        {
            textures.Add(key, texture);
        }
    }

    private static Texture2D FindTextureFor(
        ARBookGameShellController.CompanionDefinition companion,
        Dictionary<string, Texture2D> textures)
    {
        string[] keys =
        {
            companion.imageTargetName,
            companion.captureId,
            companion.displayName,
            ResolveDefaultImageTargetName(companion.captureId)
        };

        for (int i = 0; i < keys.Length; i++)
        {
            string key = Normalize(keys[i]);
            if (!string.IsNullOrWhiteSpace(key) &&
                textures.TryGetValue(key, out Texture2D texture))
            {
                return texture;
            }
        }

        return null;
    }

    private static string ResolveDefaultImageTargetName(string captureId)
    {
        if (string.Equals(captureId, "ElectrodeHisuian", StringComparison.OrdinalIgnoreCase))
        {
            return "electrode";
        }

        if (string.Equals(captureId, "Talonflame", StringComparison.OrdinalIgnoreCase))
        {
            return "GalarianZapdos";
        }

        return captureId;
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string normalized = value.Trim();
        const string scaledSuffix = "_scaled";
        if (normalized.EndsWith(scaledSuffix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - scaledSuffix.Length);
        }

        return normalized
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }
}
