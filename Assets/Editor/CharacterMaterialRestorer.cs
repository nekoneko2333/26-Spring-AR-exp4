using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CharacterMaterialRestorer
{
    private const string CharactersRoot = "Assets/models/Characters";

    private static readonly string[] TextureExtensions =
    {
        ".png", ".jpg", ".jpeg", ".tga", ".psd"
    };

    [MenuItem("Tools/Characters/Restore Materials")]
    public static void RestoreAllCharacterMaterials()
    {
        var fbxPaths = AssetDatabase.FindAssets("t:Model", new[] { CharactersRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path)
            .ToArray();

        var restoredCount = 0;
        foreach (var fbxPath in fbxPaths)
        {
            restoredCount += RestoreModelMaterials(fbxPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Character material restore finished. Remapped {restoredCount} material slot(s) across {fbxPaths.Length} FBX file(s).");
    }

    [MenuItem("Tools/Characters/Print FBX Material Slots")]
    public static void PrintFbxMaterialSlots()
    {
        var fbxPaths = AssetDatabase.FindAssets("t:Model", new[] { CharactersRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path);

        foreach (var fbxPath in fbxPaths)
        {
            var names = LoadEmbeddedMaterialNames(fbxPath).ToArray();
            Debug.Log($"{fbxPath}: {string.Join(", ", names)}");
        }
    }

    private static int RestoreModelMaterials(string fbxPath)
    {
        var modelImporter = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (modelImporter == null)
        {
            return 0;
        }

        var characterRoot = FindCharacterRoot(fbxPath);
        var textures = LoadTextures(characterRoot);
        var materialFolder = EnsureMaterialFolder(characterRoot);
        var embeddedMaterials = LoadEmbeddedMaterials(fbxPath);
        var remapped = 0;

        foreach (var embeddedMaterial in embeddedMaterials)
        {
            var material = FindOrCreateMaterial(materialFolder, embeddedMaterial.name);
            ApplyBestTextures(material, embeddedMaterial.name, textures);
            EditorUtility.SetDirty(material);

            var sourceIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), embeddedMaterial.name);
            modelImporter.AddRemap(sourceIdentifier, material);
            remapped++;
        }

        if (remapped > 0)
        {
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            modelImporter.materialLocation = ModelImporterMaterialLocation.External;
            modelImporter.SaveAndReimport();
        }

        return remapped;
    }

    private static string FindCharacterRoot(string assetPath)
    {
        var normalized = assetPath.Replace('\\', '/');
        var relative = normalized.Substring(CharactersRoot.Length).TrimStart('/');
        var firstSlash = relative.IndexOf('/');
        if (firstSlash < 0)
        {
            return Path.GetDirectoryName(normalized).Replace('\\', '/');
        }

        return $"{CharactersRoot}/{relative.Substring(0, firstSlash)}";
    }

    private static string EnsureMaterialFolder(string characterRoot)
    {
        var materialFolder = $"{characterRoot}/Materials";
        if (!AssetDatabase.IsValidFolder(materialFolder))
        {
            AssetDatabase.CreateFolder(characterRoot, "Materials");
        }

        return materialFolder;
    }

    private static IReadOnlyList<Material> LoadEmbeddedMaterials(string fbxPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<Material>()
            .Where(material => !string.IsNullOrWhiteSpace(material.name))
            .ToArray();
    }

    private static IEnumerable<string> LoadEmbeddedMaterialNames(string fbxPath)
    {
        return LoadEmbeddedMaterials(fbxPath).Select(material => material.name);
    }

    private static IReadOnlyList<Texture2D> LoadTextures(string characterRoot)
    {
        return AssetDatabase.FindAssets("t:Texture2D", new[] { characterRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => TextureExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .Select(path => AssetDatabase.LoadAssetAtPath<Texture2D>(path))
            .Where(texture => texture != null)
            .ToArray();
    }

    private static Material FindOrCreateMaterial(string folder, string materialName)
    {
        var safeName = string.Join("_", materialName.Split(Path.GetInvalidFileNameChars()));
        var path = $"{folder}/{safeName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        material = new Material(Shader.Find("Standard"))
        {
            name = materialName
        };
        AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(path));
        return material;
    }

    private static void ApplyBestTextures(Material material, string slotName, IReadOnlyList<Texture2D> textures)
    {
        var baseColor = FindTexture(slotName, textures, TextureKind.BaseColor);
        if (baseColor != null)
        {
            material.SetTexture("_MainTex", baseColor);
        }

        var normal = FindTexture(slotName, textures, TextureKind.Normal);
        if (normal != null)
        {
            MarkTextureAsNormalMap(normal);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_BumpMap", normal);
        }

        var metallic = FindTexture(slotName, textures, TextureKind.Metallic);
        if (metallic != null)
        {
            material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetTexture("_MetallicGlossMap", metallic);
            material.SetFloat("_Metallic", 1f);
        }

        var emission = FindTexture(slotName, textures, TextureKind.Emission);
        if (emission != null)
        {
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", emission);
            material.SetColor("_EmissionColor", Color.white);
        }
    }

    private static Texture2D FindTexture(string slotName, IReadOnlyList<Texture2D> textures, TextureKind kind)
    {
        var slotTokens = Tokenize(slotName);
        var candidates = textures
            .Where(texture => MatchesKind(texture.name, kind))
            .Select(texture => new
            {
                Texture = texture,
                Score = ScoreTexture(slotTokens, Tokenize(texture.name), kind)
            })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ToArray();

        return candidates.FirstOrDefault()?.Texture;
    }

    private static bool MatchesKind(string textureName, TextureKind kind)
    {
        var name = textureName.ToLowerInvariant();
        switch (kind)
        {
            case TextureKind.BaseColor:
                return name.Contains("basecolor") || name.Contains("albedo") || name.Contains("diffuse") || name.Contains("_co") || name.EndsWith("co.ktx");
            case TextureKind.Normal:
                return name.Contains("normal") || name.Contains("_nrm") || name.Contains("_n.");
            case TextureKind.Metallic:
                return name.Contains("metallic") || name.Contains("metalness");
            case TextureKind.Emission:
                return name.Contains("emissive") || name.Contains("emission");
            default:
                return false;
        }
    }

    private static int ScoreTexture(IReadOnlyCollection<string> slotTokens, IReadOnlyCollection<string> textureTokens, TextureKind kind)
    {
        var score = slotTokens.Intersect(textureTokens).Count() * 10;

        if (slotTokens.Contains("face") && textureTokens.Contains("face"))
        {
            score += 50;
        }
        else if (!slotTokens.Contains("face") && textureTokens.Contains("face"))
        {
            score -= 25;
        }

        if (slotTokens.Contains("hair") && textureTokens.Contains("hair"))
        {
            score += 40;
        }

        if ((slotTokens.Contains("body") || slotTokens.Contains("skin") || slotTokens.Contains("mouth")) &&
            (textureTokens.Contains("body") || textureTokens.Contains("skin") || textureTokens.Contains("co")))
        {
            score += 20;
        }

        if ((slotTokens.Contains("clothes") || slotTokens.Contains("cloth")) && textureTokens.Contains("clothes"))
        {
            score += 40;
        }

        if ((slotTokens.Contains("accesories") || slotTokens.Contains("accessories")) &&
            (textureTokens.Contains("accesories") || textureTokens.Contains("accessories")))
        {
            score += 40;
        }

        if (slotTokens.Contains("eyes") && textureTokens.Contains("eyes"))
        {
            score += 40;
        }

        if (kind == TextureKind.BaseColor && textureTokens.Contains("co"))
        {
            score += 5;
        }

        return score;
    }

    private static IReadOnlyCollection<string> Tokenize(string value)
    {
        return value.ToLowerInvariant()
            .Replace(".ktx", "")
            .Replace("basecolor", " basecolor ")
            .Replace("normal", " normal ")
            .Replace("metallic", " metallic ")
            .Replace("emissive", " emissive ")
            .Split(new[] { '_', '-', '.', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token == "g" ? string.Empty : token)
            .Where(token => token.Length > 0)
            .ToArray();
    }

    private static void MarkTextureAsNormalMap(Texture2D texture)
    {
        var path = AssetDatabase.GetAssetPath(texture);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.NormalMap)
        {
            return;
        }

        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

    private enum TextureKind
    {
        BaseColor,
        Normal,
        Metallic,
        Emission
    }
}
