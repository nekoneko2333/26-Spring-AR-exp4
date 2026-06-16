using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ARBookGameShellSetup
{
    private const string ShellName = "ARBookGameShell";

    [MenuItem("ARBook/Tools/Repair Current UI Bindings")]
    public static void RepairCurrentUiBindings()
    {
        ARBookGameShellController controller = FindOrCreateShellController();
        if (controller == null)
        {
            Debug.LogWarning("ARBookGameShellController was not found or created.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(
            controller.gameObject,
            "Repair ARBook Current UI Bindings");

        int bound = 0;
        bound += BindSceneData(controller);
        bound += BindShellUi(controller);
        bound += BindInteraction();
        bound += BindDialogue(controller);
        bound += BindBattle(controller);
        bound += BindDefaultVisibility(controller);
        int missing = ReportMissingReferences(controller);

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log(
            $"ARBook current UI binding repair complete. Updated {bound} bindings/states. Missing required references: {missing}.");
    }

    private static ARBookGameShellController FindOrCreateShellController()
    {
        GameObject shell = GameObject.Find(ShellName);
        if (shell == null)
        {
            shell = new GameObject(ShellName);
            Undo.RegisterCreatedObjectUndo(shell, "Create ARBookGameShell");
        }

        ARBookGameShellController controller =
            shell.GetComponent<ARBookGameShellController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<ARBookGameShellController>(shell);
        }

        return controller;
    }

    private static int BindSceneData(ARBookGameShellController controller)
    {
        int changed = 0;
        changed += SetObject(
            controller,
            controller.collectionManager,
            FindAny<ARBookCollectionManager>(),
            value => controller.collectionManager = value);
        changed += SetObject(
            controller,
            controller.chapterProgress,
            FindAny<ARBookChapterProgress>(),
            value => controller.chapterProgress = value);
        changed += SetObject(
            controller,
            controller.progressResetter,
            FindAny<ARBookDebugProgressResetter>(),
            value => controller.progressResetter = value);
        changed += SetObject(
            controller,
            controller.chapterHudController,
            FindAny<ARBookChapterHUDController>(),
            value => controller.chapterHudController = value);

        if (controller.chineseFont == null)
        {
            TMP_Text text = FindAny<TMP_Text>();
            if (text != null)
            {
                Undo.RecordObject(controller, "Bind UI Font");
                controller.chineseFont = text.font;
                changed++;
            }
        }

        if (controller.companionPlacementRoot == null && Camera.main != null)
        {
            Undo.RecordObject(controller, "Bind Companion Placement Root");
            controller.companionPlacementRoot = Camera.main.transform;
            changed++;
        }

        if (controller.companions == null || controller.companions.Length == 0)
        {
            Debug.LogWarning(
                "ARBookGameShellController companions is empty. Not auto-resetting it to avoid overwriting manual prefab/image bindings.");
        }

        return changed;
    }

    private static int BindShellUi(ARBookGameShellController controller)
    {
        int changed = 0;
        Canvas shellCanvas = FindNamedComponent<Canvas>("ARBookGameShellCanvas");
        if (shellCanvas == null && controller.rootCanvas != null)
        {
            shellCanvas = controller.rootCanvas;
        }

        if (shellCanvas == null)
        {
            RectTransform generated = FindNamedComponent<RectTransform>(
                "ARBookGameShellGeneratedRoot");
            shellCanvas = generated != null
                ? generated.GetComponentInParent<Canvas>(true)
                : FindAny<Canvas>();
        }

        changed += SetObject(
            controller,
            controller.rootCanvas,
            shellCanvas,
            value => controller.rootCanvas = value);

        Transform searchRoot = shellCanvas != null ? shellCanvas.transform : null;
        RectTransform generatedRoot = FindRect(
            searchRoot,
            "ARBookGameShellGeneratedRoot");
        if (generatedRoot == null)
        {
            generatedRoot = FindNamedComponent<RectTransform>(
                "ARBookGameShellGeneratedRoot");
        }

        changed += SetObject(
            controller,
            controller.generatedRoot,
            generatedRoot,
            value => controller.generatedRoot = value);

        Transform root = generatedRoot != null
            ? generatedRoot
            : searchRoot;

        changed += SetObject(controller, controller.homeRoot, FindRectAny(root, "Home"), value => controller.homeRoot = value);
        changed += SetObject(controller, controller.hudRoot, FindRectAny(root, "HUD"), value => controller.hudRoot = value);
        changed += SetObject(controller, controller.companionRoot, FindRectAny(root, "CompanionMode"), value => controller.companionRoot = value);
        changed += SetObject(controller, controller.backpackRoot, FindRectAny(root, "Backpack"), value => controller.backpackRoot = value);
        changed += SetObject(controller, controller.dialogueRoot, FindRectAny(root, "DialoguePanel", "DialogueCanvas", "DialogueBox"), value => controller.dialogueRoot = value);
        changed += SetObject(controller, controller.battleRoot, FindRectAny(root, "BattlePanel", "BattleCanvas", "BattleControls"), value => controller.battleRoot = value);

        changed += SetObject(controller, controller.companionGrid, FindRect(controller.companionRoot, "CompanionGrid"), value => controller.companionGrid = value);
        changed += SetObject(controller, controller.startButton, FindButton(controller.homeRoot, "StartButton"), value => controller.startButton = value);
        changed += SetObject(controller, controller.restartButton, FindButton(controller.homeRoot, "RestartButton"), value => controller.restartButton = value);
        changed += SetObject(controller, controller.homeCompanionButton, FindButton(controller.homeRoot, "CompanionButton", "HomeCompanionButton"), value => controller.homeCompanionButton = value);
        changed += SetObject(controller, controller.backpackButton, FindButton(controller.hudRoot, "BackpackButton", "BagButton"), value => controller.backpackButton = value);
        changed += SetObject(controller, controller.hudCompanionButton, FindButton(controller.hudRoot, "CompanionButton", "HUDCompanionButton"), value => controller.hudCompanionButton = value);
        changed += SetObject(controller, controller.homeButton, FindButton(controller.hudRoot, "HomeButton"), value => controller.homeButton = value);
        changed += SetObject(controller, controller.placeButton, FindButton(controller.companionRoot, "PlaceButton"), value => controller.placeButton = value);
        changed += SetButtonLabel(controller.placeButton, "携带");
        changed += SetObject(controller, controller.affectionButton, FindButton(controller.companionRoot, "AffectionButton", "InteractButton"), value => controller.affectionButton = value);
        changed += SetButtonLabel(controller.affectionButton, "陪伴");
        changed += SetObject(controller, controller.clearCompanionsButton, FindButton(controller.companionRoot, "ClearButton", "ClearCompanionsButton"), value => controller.clearCompanionsButton = value);
        if (controller.clearCompanionsButton != null)
        {
            SetInactive(controller.clearCompanionsButton.gameObject, ref changed);
        }
        changed += SetObject(controller, controller.closeCompanionButton, FindButton(controller.companionRoot, "CloseButton", "CloseCompanionButton"), value => controller.closeCompanionButton = value);
        changed += SetButtonLabel(controller.closeCompanionButton, "返回");
        changed += SetObject(controller, controller.closeBackpackButton, FindButton(controller.backpackRoot, "CloseButton", "CloseBackpackButton"), value => controller.closeBackpackButton = value);
        changed += SetObject(controller, controller.dialogueContinueButton, FindButton(controller.dialogueRoot, "ContinueButton", "NextButton"), value => controller.dialogueContinueButton = value);
        changed += SetObject(controller, controller.battleAttackButton, FindButton(controller.battleRoot, "AttackButton"), value => controller.battleAttackButton = value);
        changed += SetObject(controller, controller.battleExitButton, FindButton(controller.battleRoot, "ExitButton", "CloseButton"), value => controller.battleExitButton = value);

        TMP_Text startLabel = controller.startButton != null
            ? controller.startButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        changed += SetObject(controller, controller.startButtonText, startLabel, value => controller.startButtonText = value);
        changed += SetObject(controller, controller.questText, FindText(controller.hudRoot, "QuestText"), value => controller.questText = value);
        changed += SetObject(controller, controller.progressText, FindText(controller.hudRoot, "ProgressText"), value => controller.progressText = value);
        changed += SetObject(controller, controller.capturedCountText, FindText(controller.backpackRoot, "BackpackText"), value => controller.capturedCountText = value);
        changed += SetObject(controller, controller.companionDetailText, FindText(controller.companionRoot, "DetailText"), value => controller.companionDetailText = value);
        changed += SetObject(controller, controller.dialogueSpeakerText, FindText(controller.dialogueRoot, "SpeakerNameText", "SpeakerName"), value => controller.dialogueSpeakerText = value);
        changed += SetObject(controller, controller.dialogueBodyText, FindText(controller.dialogueRoot, "DialogueText"), value => controller.dialogueBodyText = value);
        changed += SetObject(controller, controller.battleMessageText, FindText(controller.battleRoot, "BattleMessageText", "MessageText"), value => controller.battleMessageText = value);
        if (controller.battleMessageText == null && controller.battleRoot != null)
        {
            TMP_Text messageText = EnsureText(
                controller.battleRoot,
                "BattleMessageText",
                "选择行动",
                new Vector2(0f, -130f),
                new Vector2(420f, 60f));
            changed += SetObject(controller, controller.battleMessageText, messageText, value => controller.battleMessageText = value);
        }
        changed += SetObject(controller, controller.battleLeftHPText, FindText(controller.battleRoot, "LeftHPText", "EnemyHPText"), value => controller.battleLeftHPText = value);
        changed += SetObject(controller, controller.battleRightHPText, FindText(controller.battleRoot, "RightHPText", "PlayerHPText"), value => controller.battleRightHPText = value);

        changed += SetObject(controller, controller.hpFill, FindNamedChild<Image>(controller.hudRoot, "HPFill"), value => controller.hpFill = value);
        changed += SetObject(controller, controller.dialogueLeftHighlight, FindNamedChild<Image>(controller.dialogueRoot, "LeftSpeakerHighlight"), value => controller.dialogueLeftHighlight = value);
        changed += SetObject(controller, controller.dialogueRightHighlight, FindNamedChild<Image>(controller.dialogueRoot, "RightSpeakerHighlight"), value => controller.dialogueRightHighlight = value);
        changed += SetObject(controller, controller.battleLeftHPSlider, FindNamedChild<Slider>(controller.battleRoot, "LeftHPSlider", "EnemyHPSlider"), value => controller.battleLeftHPSlider = value);
        changed += SetObject(controller, controller.battleRightHPSlider, FindNamedChild<Slider>(controller.battleRoot, "RightHPSlider", "PlayerHPSlider"), value => controller.battleRightHPSlider = value);

        if (controller.hudRoot != null)
        {
            TMP_Text companionStatus = EnsureText(
                controller.hudRoot,
                "CompanionCameraStatusText",
                "心情",
                new Vector2(0f, -52f),
                new Vector2(520f, 46f));
            Image moodFill = EnsureImage(
                controller.hudRoot,
                "CompanionMoodFill",
                new Vector2(0f, -88f),
                new Vector2(520f, 24f));
            Button returnGame = EnsureButton(
                controller.hudRoot,
                "CompanionReturnGameButton",
                "返回游戏",
                new Vector2(-250f, 58f));
            Button companionInteract = EnsureButton(
                controller.hudRoot,
                "CompanionInteractButton",
                "互动",
                new Vector2(0f, 58f));
            Button returnHome = EnsureButton(
                controller.hudRoot,
                "CompanionReturnHomeButton",
                "返回首页",
                new Vector2(250f, 58f));
            changed += SetObject(controller, controller.companionCameraInteractButton, companionInteract, value => controller.companionCameraInteractButton = value);
            changed += SetObject(controller, controller.companionCameraStatusText, companionStatus, value => controller.companionCameraStatusText = value);
            changed += SetObject(controller, controller.companionMoodFill, moodFill, value => controller.companionMoodFill = value);
            changed += SetObject(controller, controller.companionReturnGameButton, returnGame, value => controller.companionReturnGameButton = value);
            changed += SetObject(controller, controller.companionReturnHomeButton, returnHome, value => controller.companionReturnHomeButton = value);
        }

        return changed;
    }

    private static int BindDialogue(ARBookGameShellController controller)
    {
        int changed = 0;
        DialogueManager manager = FindAny<DialogueManager>();
        if (manager != null && controller.dialogueRoot != null)
        {
            Undo.RecordObject(manager, "Bind DialogueManager UI");
            manager.dialoguePanel = controller.dialogueRoot.gameObject;
            manager.speakerNameTMPText = controller.dialogueSpeakerText;
            manager.dialogueTMPText = controller.dialogueBodyText;
            manager.continueButton = controller.dialogueContinueButton;
            manager.speakerNameText = null;
            manager.dialogueText = null;
            manager.dialoguePanel.SetActive(false);
            EditorUtility.SetDirty(manager);
            changed += 6;
        }

        ARBookCinematicDialogueController cinematic =
            FindAny<ARBookCinematicDialogueController>();
        if (cinematic != null && controller.dialogueRoot != null)
        {
            Undo.RecordObject(cinematic, "Bind Cinematic Dialogue UI");
            cinematic.dialogueUIRoot = controller.dialogueRoot.gameObject;
            cinematic.speakerNameText = controller.dialogueSpeakerText;
            cinematic.dialogueText = controller.dialogueBodyText;
            cinematic.leftSpeakerHighlight = controller.dialogueLeftHighlight;
            cinematic.rightSpeakerHighlight = controller.dialogueRightHighlight;
            EditorUtility.SetDirty(cinematic);
            changed += 5;

            changed += EnsurePersistentListener(
                controller.dialogueContinueButton,
                cinematic,
                "ContinueDialogue",
                cinematic.ContinueDialogue);
        }

        return changed;
    }

    private static int BindInteraction()
    {
        int changed = 0;
        ARBookInteractionButton interaction = FindAny<ARBookInteractionButton>();
        if (interaction == null)
        {
            return changed;
        }

        Button button = FindInteractionButton();
        GameObject interactionRoot = FindInteractionRoot(button);
        TMP_Text prompt = button != null
            ? button.GetComponentInChildren<TMP_Text>(true)
            : null;

        Undo.RecordObject(interaction, "Bind Interaction UI");
        if (interaction.interactButton == null && button != null)
        {
            interaction.interactButton = button;
            changed++;
        }

        if (interaction.interactionRoot != interactionRoot && interactionRoot != null)
        {
            interaction.interactionRoot = interactionRoot;
            changed++;
        }

        if (interaction.promptTMPText == null && prompt != null)
        {
            interaction.promptTMPText = prompt;
            changed++;
        }

        if (interaction.playerMover == null)
        {
            ARBookPlayerMover mover = FindAny<ARBookPlayerMover>();
            if (mover != null)
            {
                interaction.playerMover = mover;
                changed++;
            }
        }

        if (interaction.captureController == null)
        {
            ARBookCaptureController capture = FindAny<ARBookCaptureController>();
            if (capture != null)
            {
                interaction.captureController = capture;
                changed++;
            }
        }

        interaction.promptFormat = "\u4e92\u52a8\uff1a{0}";
        interaction.hideButtonWhenNoTarget = true;
        interaction.useActivePlayerMover = true;
        interaction.activateCapturableInteractablesOnStart = true;
        EditorUtility.SetDirty(interaction);
        return changed + 4;
    }

    private static int BindBattle(ARBookGameShellController controller)
    {
        int changed = 0;
        ARBookBattleController battle = FindAny<ARBookBattleController>();
        if (battle == null || controller.battleRoot == null)
        {
            return changed;
        }

        Undo.RecordObject(battle, "Bind Battle UI");
        Transform battleControls = FindNamedChild(
            controller.battleRoot,
            "BattleControls") ?? controller.battleRoot;
        battle.battleControlsRoot = battleControls.gameObject;
        battle.messageText = controller.battleMessageText;
        battle.battleAttackButton = controller.battleAttackButton;
        battle.battleExitButton = controller.battleExitButton;
        battle.companionAButton = EnsureButton(
            battleControls,
            "CompanionAButton",
            "A 宝可梦攻击",
            new Vector2(-210f, 38f));
        battle.companionBButton = EnsureButton(
            battleControls,
            "CompanionBButton",
            "B 宝可梦攻击",
            new Vector2(-210f, -38f));
        battle.companionAButtonText = battle.companionAButton != null
            ? battle.companionAButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        battle.companionBButtonText = battle.companionBButton != null
            ? battle.companionBButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        controller.battleRoot.gameObject.SetActive(false);
        EditorUtility.SetDirty(battle);
        changed += 7;

        changed += EnsurePersistentListener(
            controller.battleAttackButton,
            battle,
            "PlayerAttack",
            battle.PlayerAttack);
        changed += EnsurePersistentListener(
            controller.battleExitButton,
            battle,
            "ExitBattle",
            battle.ExitBattle);
        changed += EnsurePersistentListener(
            battle.companionAButton,
            battle,
            "CompanionAAttack",
            battle.CompanionAAttack);
        changed += EnsurePersistentListener(
            battle.companionBButton,
            battle,
            "CompanionBAttack",
            battle.CompanionBAttack);

        if (battle.enemy != null)
        {
            bool enemyChanged = false;
            Undo.RecordObject(battle.enemy, "Bind Enemy Battle HUD");
            if (controller.battleLeftHPSlider != null &&
                battle.enemy.hpSlider != controller.battleLeftHPSlider)
            {
                battle.enemy.hpSlider = controller.battleLeftHPSlider;
                enemyChanged = true;
                changed++;
            }

            if (controller.battleLeftHPText != null &&
                battle.enemy.hpText != controller.battleLeftHPText)
            {
                battle.enemy.hpText = controller.battleLeftHPText;
                enemyChanged = true;
                changed++;
            }

            if (enemyChanged)
            {
                EditorUtility.SetDirty(battle.enemy);
            }
        }

        if (battle.player != null)
        {
            bool playerChanged = false;
            Undo.RecordObject(battle.player, "Bind Player Battle HUD");
            if (controller.battleRightHPSlider != null &&
                battle.player.hpSlider != controller.battleRightHPSlider)
            {
                battle.player.hpSlider = controller.battleRightHPSlider;
                playerChanged = true;
                changed++;
            }

            if (controller.battleRightHPText != null &&
                battle.player.hpText != controller.battleRightHPText)
            {
                battle.player.hpText = controller.battleRightHPText;
                playerChanged = true;
                changed++;
            }

            if (playerChanged)
            {
                EditorUtility.SetDirty(battle.player);
            }
        }

        return changed;
    }

    private static int BindDefaultVisibility(ARBookGameShellController controller)
    {
        int changed = 0;
        Undo.RecordObject(controller, "Configure UI Visibility");
        controller.hideLegacyChapterHudTexts = true;
        controller.ApplyDefaultUiVisibility();
        changed++;

        if (controller.chapterHudController != null)
        {
            TMP_Text oldQuest = controller.chapterHudController.questText;
            TMP_Text oldProgress = controller.chapterHudController.chapterProgressText;
            TMP_Text oldChallenge = controller.chapterHudController.challengeText;
            SetInactive(oldQuest != null ? oldQuest.gameObject : null, ref changed);
            SetInactive(oldProgress != null ? oldProgress.gameObject : null, ref changed);
            SetInactive(oldChallenge != null ? oldChallenge.gameObject : null, ref changed);

            Undo.RecordObject(controller.chapterHudController, "Clear Legacy HUD Text UI");
            controller.chapterHudController.questText = null;
            controller.chapterHudController.chapterProgressText = null;
            controller.chapterHudController.challengeText = null;
            EditorUtility.SetDirty(controller.chapterHudController);
            changed++;
        }

        return changed;
    }

    private static int ReportMissingReferences(ARBookGameShellController controller)
    {
        List<string> missing = new List<string>();
        AddMissing(missing, controller.rootCanvas, "Root Canvas");
        AddMissing(missing, controller.generatedRoot, "Generated Root");
        AddMissing(missing, controller.homeRoot, "Home Root");
        AddMissing(missing, controller.hudRoot, "HUD Root");
        AddMissing(missing, controller.backpackRoot, "Backpack Root");
        AddMissing(missing, controller.companionRoot, "Companion Root");
        AddMissing(missing, controller.dialogueRoot, "Dialogue Root");
        AddMissing(missing, controller.battleRoot, "Battle Root");
        AddMissing(missing, controller.startButton, "Start Button");
        AddMissing(missing, controller.restartButton, "Restart Button");
        AddMissing(missing, controller.questText, "Quest Text");
        AddMissing(missing, controller.progressText, "Progress Text");
        AddMissing(missing, controller.dialogueBodyText, "Dialogue Text");
        AddMissing(missing, controller.dialogueContinueButton, "Dialogue Continue Button");
        AddMissing(missing, controller.battleMessageText, "Battle Message Text");
        AddMissing(missing, controller.battleAttackButton, "Battle Attack Button");
        ARBookInteractionButton interaction = FindAny<ARBookInteractionButton>();
        if (interaction == null)
        {
            missing.Add("Interaction Button Controller");
        }
        else
        {
            AddMissing(missing, interaction.interactButton, "Interact Button");
            AddMissing(missing, interaction.promptTMPText, "Interaction Prompt Text");
        }

        for (int i = 0; i < missing.Count; i++)
        {
            Debug.LogWarning($"ARBook UI binding missing: {missing[i]}");
        }

        return missing.Count;
    }

    private static void AddMissing(
        List<string> missing,
        Object value,
        string label)
    {
        if (value == null)
        {
            missing.Add(label);
        }
    }

    private static int EnsurePersistentListener(
        Button button,
        Object target,
        string methodName,
        UnityAction action)
    {
        if (button == null || target == null || action == null)
        {
            return 0;
        }

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == target &&
                button.onClick.GetPersistentMethodName(i) == methodName)
            {
                return 0;
            }
        }

        Undo.RecordObject(button, "Bind UI Button Event");
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
        return 1;
    }

    private static int SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return 0;
        }

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null || text.text == label)
        {
            return 0;
        }

        Undo.RecordObject(text, "Set Button Label");
        text.text = label;
        EditorUtility.SetDirty(text);
        return 1;
    }

    private static Button EnsureButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition)
    {
        if (parent == null)
        {
            return null;
        }

        Button existing = FindButton(parent, name);
        if (existing != null)
        {
            return existing;
        }

        GameObject buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        Undo.RegisterCreatedObjectUndo(buttonObject, $"Create {name}");
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 64f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textObject, $"Create {name} Text");
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 6f);
        textRect.offsetMax = new Vector2(-8f, -6f);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = label;
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;

        EditorUtility.SetDirty(buttonObject);
        return button;
    }

    private static TMP_Text EnsureText(
        Transform parent,
        string name,
        string textValue,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        if (parent == null)
        {
            return null;
        }

        TMP_Text existing = FindText(parent, name);
        if (existing != null)
        {
            return existing;
        }

        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textObject, $"Create {name}");
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = textValue;
        text.fontSize = 26f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;

        EditorUtility.SetDirty(textObject);
        return text;
    }

    private static Image EnsureImage(
        Transform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        if (parent == null)
        {
            return null;
        }

        Image existing = FindNamedChild<Image>(parent, name);
        if (existing != null)
        {
            return existing;
        }

        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image));
        Undo.RegisterCreatedObjectUndo(imageObject, $"Create {name}");
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;

        EditorUtility.SetDirty(imageObject);
        return image;
    }

    private static int SetObject<T>(
        Object undoTarget,
        T current,
        T value,
        System.Action<T> assign)
        where T : Object
    {
        if (value == null || current == value)
        {
            return 0;
        }

        Undo.RecordObject(undoTarget, "Bind UI Reference");
        assign(value);
        EditorUtility.SetDirty(undoTarget);
        return 1;
    }

    private static void SetInactive(GameObject target, ref int changed)
    {
        if (target == null || !target.activeSelf)
        {
            return;
        }

        Undo.RecordObject(target, "Hide Legacy UI");
        target.SetActive(false);
        EditorUtility.SetDirty(target);
        changed++;
    }

    private static T FindAny<T>() where T : Object
    {
        return Object.FindObjectOfType<T>(true);
    }

    private static Button FindInteractionButton()
    {
        Button[] buttons = Object.FindObjectsOfType<Button>(true);
        Button fallback = null;
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || button.name != "InteractButton")
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = button;
            }

            Transform parent = button.transform.parent;
            if (parent != null && parent.name == "ActionButtons")
            {
                return button;
            }
        }

        return fallback;
    }

    private static GameObject FindInteractionRoot(Button button)
    {
        if (button == null)
        {
            return null;
        }

        Transform parent = button.transform.parent;
        if (parent != null && parent.name == "ActionButtons")
        {
            return parent.gameObject;
        }

        return button.gameObject;
    }

    private static T FindNamedComponent<T>(params string[] names)
        where T : Component
    {
        Transform transform = FindNamedTransform(names);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private static RectTransform FindRect(Transform root, params string[] names)
    {
        return FindNamedChild<RectTransform>(root, names);
    }

    private static RectTransform FindRectAny(Transform root, params string[] names)
    {
        RectTransform rect = FindRect(root, names);
        if (rect != null)
        {
            return rect;
        }

        return FindNamedComponent<RectTransform>(names);
    }

    private static Button FindButton(Transform root, params string[] names)
    {
        return FindNamedChild<Button>(root, names);
    }

    private static TMP_Text FindText(Transform root, params string[] names)
    {
        return FindNamedChild<TMP_Text>(root, names);
    }

    private static T FindNamedChild<T>(Transform root, params string[] names)
        where T : Component
    {
        Transform transform = FindNamedChild(root, names);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private static Transform FindNamedChild(Transform root, params string[] names)
    {
        if (root == null || names == null)
        {
            return null;
        }

        for (int i = 0; i < names.Length; i++)
        {
            Transform found = FindDescendant(root, names[i]);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindNamedTransform(params string[] names)
    {
        Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
        for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == names[nameIndex])
                {
                    return transforms[i];
                }
            }
        }

        return null;
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

