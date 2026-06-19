using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ARBookUiStyleRepairTool
{
    private const string ButtonSpritePath = "Assets/UI/按键.png";

    [MenuItem("ARBook/Tools/Apply Black Text And Button Sprite")]
    public static void ApplyBlackTextAndButtonSprite()
    {
        Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
        if (buttonSprite == null)
        {
            Debug.LogError($"找不到按钮图片 Sprite：{ButtonSpritePath}");
            return;
        }

        int textCount = ApplyTextColor();
        int buttonCount = ApplyButtonSprite(buttonSprite);

        Debug.Log(
            $"UI 样式修复完成：文字改黑 {textCount} 个，按钮绑定图片 {buttonCount} 个。");
    }

    private static int ApplyTextColor()
    {
        int changed = 0;
        HashSet<Scene> scenes = new HashSet<Scene>();

        TMP_Text[] tmpTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        for (int i = 0; i < tmpTexts.Length; i++)
        {
            TMP_Text text = tmpTexts[i];
            if (!IsSceneObject(text))
            {
                continue;
            }

            Undo.RecordObject(text, "Set text color black");
            text.color = Color.black;
            EditorUtility.SetDirty(text);
            scenes.Add(text.gameObject.scene);
            changed++;
        }

        Text[] legacyTexts = Resources.FindObjectsOfTypeAll<Text>();
        for (int i = 0; i < legacyTexts.Length; i++)
        {
            Text text = legacyTexts[i];
            if (!IsSceneObject(text))
            {
                continue;
            }

            Undo.RecordObject(text, "Set text color black");
            text.color = Color.black;
            EditorUtility.SetDirty(text);
            scenes.Add(text.gameObject.scene);
            changed++;
        }

        MarkScenesDirty(scenes);
        return changed;
    }

    private static int ApplyButtonSprite(Sprite buttonSprite)
    {
        int changed = 0;
        HashSet<Scene> scenes = new HashSet<Scene>();
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (!IsSceneObject(button))
            {
                continue;
            }

            Image image = button.GetComponent<Image>();
            if (image == null)
            {
                Undo.AddComponent<Image>(button.gameObject);
                image = button.GetComponent<Image>();
            }

            Undo.RecordObject(image, "Bind button sprite");
            Undo.RecordObject(button, "Bind button target graphic");
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.raycastTarget = true;
            button.targetGraphic = image;
            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(button);
            scenes.Add(button.gameObject.scene);
            changed++;
        }

        MarkScenesDirty(scenes);
        return changed;
    }

    private static bool IsSceneObject(Component component)
    {
        return component != null &&
               component.gameObject != null &&
               component.gameObject.scene.IsValid() &&
               component.gameObject.scene.isLoaded &&
               !EditorUtility.IsPersistent(component);
    }

    private static void MarkScenesDirty(HashSet<Scene> scenes)
    {
        foreach (Scene scene in scenes)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
