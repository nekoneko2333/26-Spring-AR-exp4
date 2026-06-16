using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

public static class ARBookDebugProgressSetup
{
    private static readonly string[] PokemonCaptureIds =
    {
        "Bulbasaur",
        "Talonflame",
        "Axew",
        "Pikachu",
        "Meowth",
        "Infernape",
        "Squirtle",
        "Jirachi",
        "Sneasler",
        "Zorua",
        "Zekrom",
        "Zygarde10",
        "Toxtricity",
        "Scizor",
        "Mismagius",
        "Mew",
        "Manaphy",
        "ElectrodeHisuian",
        "Dragapult",
        "Celebi"
    };

    [MenuItem("ARBook/Tools/Fill Debug Capture IDs")]
    public static void FillDebugCaptureIds()
    {
        ARBookDebugProgressResetter resetter =
            Object.FindObjectOfType<ARBookDebugProgressResetter>(true);
        if (resetter == null)
        {
            Debug.LogWarning("没有找到 ARBookDebugProgressResetter。");
            return;
        }

        Undo.RecordObject(resetter, "Fill Debug Capture IDs");
        resetter.captureIds = ResolveCaptureIds();
        EditorUtility.SetDirty(resetter);
        EditorSceneManager.MarkSceneDirty(resetter.gameObject.scene);
        Debug.Log($"已填入 Debug captureIds：{resetter.captureIds.Length} 个。");
    }

    private static string[] ResolveCaptureIds()
    {
        ARBookGameShellController controller =
            Object.FindObjectOfType<ARBookGameShellController>(true);
        if (controller == null ||
            controller.companions == null ||
            controller.companions.Length == 0)
        {
            return (string[])PokemonCaptureIds.Clone();
        }

        List<string> ids = new List<string>();
        for (int i = 0; i < controller.companions.Length; i++)
        {
            ARBookGameShellController.CompanionDefinition companion =
                controller.companions[i];
            if (companion == null ||
                string.IsNullOrWhiteSpace(companion.captureId) ||
                ids.Contains(companion.captureId))
            {
                continue;
            }

            ids.Add(companion.captureId);
        }

        return ids.Count > 0 ? ids.ToArray() : (string[])PokemonCaptureIds.Clone();
    }
}
