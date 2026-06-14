using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class ChineseTmpFontAssetUpdater
{
    private const string FontAssetPath = "Assets/Fonts/SimplifiedChinese/SourceHanSansSC-Normal SDF.asset";
    private const string CharacterSetPath = "Assets/Fonts/SimplifiedChinese/ChineseCharacterSet.txt";
    private static readonly string[] SourceFolders =
    {
        "Assets/Scripts",
        "Assets/Scenes"
    };

    [MenuItem("Tools/Fonts/Update Chinese TMP Font Asset")]
    public static void UpdateChineseFontAsset()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        TextAsset characterSet = AssetDatabase.LoadAssetAtPath<TextAsset>(CharacterSetPath);

        if (fontAsset == null)
        {
            Debug.LogError($"Missing TMP font asset: {FontAssetPath}");
            return;
        }

        if (characterSet == null)
        {
            Debug.LogError($"Missing Chinese character set: {CharacterSetPath}");
            return;
        }

        string characters = GetUniqueCharacters(characterSet.text);

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.isMultiAtlasTexturesEnabled = true;

        if (!fontAsset.TryAddCharacters(characters, out string missingCharacters))
        {
            Debug.LogWarning($"Chinese TMP font asset updated with missing characters: {missingCharacters}");
        }
        else
        {
            Debug.Log($"Chinese TMP font asset updated. Character count: {characters.Length}");
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Fonts/Rebuild Chinese Character Set And Font Asset")]
    public static void RebuildChineseCharacterSetAndFontAsset()
    {
        string characters = CollectProjectChineseCharacters();
        File.WriteAllText(CharacterSetPath, characters, Encoding.UTF8);
        AssetDatabase.ImportAsset(CharacterSetPath);

        Debug.Log($"Chinese character set rebuilt. Character count: {characters.Length}");
        UpdateChineseFontAsset();
    }

    private static string GetUniqueCharacters(string text)
    {
        var seen = new HashSet<char>();
        var characters = new List<char>();

        foreach (char character in text)
        {
            if (char.IsWhiteSpace(character) || !seen.Add(character))
            {
                continue;
            }

            characters.Add(character);
        }

        return new string(characters.ToArray());
    }

    private static string CollectProjectChineseCharacters()
    {
        var seen = new HashSet<char>();
        var characters = new List<char>();

        foreach (string sourceFolder in SourceFolders)
        {
            if (!Directory.Exists(sourceFolder))
            {
                continue;
            }

            foreach (string filePath in Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(filePath);
                if (extension != ".cs" && extension != ".unity" && extension != ".prefab")
                {
                    continue;
                }

                string content = File.ReadAllText(filePath, Encoding.UTF8);
                AddChineseCharacters(content, seen, characters);
                AddEscapedUnicodeCharacters(content, seen, characters);
            }
        }

        characters.Sort();
        return new string(characters.ToArray());
    }

    private static void AddChineseCharacters(string text, HashSet<char> seen, List<char> characters)
    {
        foreach (char character in text)
        {
            if (!IsChineseCharacterSetEntry(character) || !seen.Add(character))
            {
                continue;
            }

            characters.Add(character);
        }
    }

    private static void AddEscapedUnicodeCharacters(string text, HashSet<char> seen, List<char> characters)
    {
        foreach (Match match in Regex.Matches(text, @"\\u([0-9a-fA-F]{4})"))
        {
            int code = int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
            char character = (char)code;

            if (!IsChineseCharacterSetEntry(character) || !seen.Add(character))
            {
                continue;
            }

            characters.Add(character);
        }
    }

    private static bool IsChineseCharacterSetEntry(char character)
    {
        return (character >= 0x4E00 && character <= 0x9FFF) ||
            "：，。！？、；（）【】《》“”‘’".IndexOf(character) >= 0;
    }
}
