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
            Undo.RecordObject(controller, "Reset Companion Catalog");
            controller.ResetCatalogToDefault();
            changed++;
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

        changed += SetObject(controller, controller.homeRoot, FindRect(root, "Home"), value => controller.homeRoot = value);
        changed += SetObject(controller, controller.hudRoot, FindRect(root, "HUD"), value => controller.hudRoot = value);
        changed += SetObject(controller, controller.companionRoot, FindRect(root, "CompanionMode"), value => controller.companionRoot = value);
        changed += SetObject(controller, controller.backpackRoot, FindRect(root, "Backpack"), value => controller.backpackRoot = value);
        changed += SetObject(controller, controller.dialogueRoot, FindRect(root, "DialoguePanel", "DialogueCanvas", "DialogueBox"), value => controller.dialogueRoot = value);
        changed += SetObject(controller, controller.battleRoot, FindRect(root, "BattlePanel", "BattleCanvas", "BattleControls"), value => controller.battleRoot = value);

        changed += SetObject(controller, controller.companionGrid, FindRect(controller.companionRoot, "CompanionGrid"), value => controller.companionGrid = value);
        changed += SetObject(controller, controller.startButton, FindButton(controller.homeRoot, "StartButton"), value => controller.startButton = value);
        changed += SetObject(controller, controller.restartButton, FindButton(controller.homeRoot, "RestartButton"), value => controller.restartButton = value);
        changed += SetObject(controller, controller.homeCompanionButton, FindButton(controller.homeRoot, "CompanionButton", "HomeCompanionButton"), value => controller.homeCompanionButton = value);
        changed += SetObject(controller, controller.backpackButton, FindButton(controller.hudRoot, "BackpackButton", "BagButton"), value => controller.backpackButton = value);
        changed += SetObject(controller, controller.hudCompanionButton, FindButton(controller.hudRoot, "CompanionButton", "HUDCompanionButton"), value => controller.hudCompanionButton = value);
        changed += SetObject(controller, controller.homeButton, FindButton(controller.hudRoot, "HomeButton"), value => controller.homeButton = value);
        changed += SetObject(controller, controller.placeButton, FindButton(controller.companionRoot, "PlaceButton"), value => controller.placeButton = value);
        changed += SetObject(controller, controller.affectionButton, FindButton(controller.companionRoot, "AffectionButton", "InteractButton"), value => controller.affectionButton = value);
        changed += SetObject(controller, controller.clearCompanionsButton, FindButton(controller.companionRoot, "ClearButton", "ClearCompanionsButton"), value => controller.clearCompanionsButton = value);
        changed += SetObject(controller, controller.closeCompanionButton, FindButton(controller.companionRoot, "CloseButton", "CloseCompanionButton"), value => controller.closeCompanionButton = value);
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
        changed += SetObject(controller, controller.battleLeftHPText, FindText(controller.battleRoot, "LeftHPText", "EnemyHPText"), value => controller.battleLeftHPText = value);
        changed += SetObject(controller, controller.battleRightHPText, FindText(controller.battleRoot, "RightHPText", "PlayerHPText"), value => controller.battleRightHPText = value);

        changed += SetObject(controller, controller.hpFill, FindNamedChild<Image>(controller.hudRoot, "HPFill"), value => controller.hpFill = value);
        changed += SetObject(controller, controller.dialogueLeftHighlight, FindNamedChild<Image>(controller.dialogueRoot, "LeftSpeakerHighlight"), value => controller.dialogueLeftHighlight = value);
        changed += SetObject(controller, controller.dialogueRightHighlight, FindNamedChild<Image>(controller.dialogueRoot, "RightSpeakerHighlight"), value => controller.dialogueRightHighlight = value);
        changed += SetObject(controller, controller.battleLeftHPSlider, FindNamedChild<Slider>(controller.battleRoot, "LeftHPSlider", "EnemyHPSlider"), value => controller.battleLeftHPSlider = value);
        changed += SetObject(controller, controller.battleRightHPSlider, FindNamedChild<Slider>(controller.battleRoot, "RightHPSlider", "PlayerHPSlider"), value => controller.battleRightHPSlider = value);

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
        battle.battleControlsRoot = controller.battleRoot.gameObject;
        battle.messageText = controller.battleMessageText;
        controller.battleRoot.gameObject.SetActive(false);
        EditorUtility.SetDirty(battle);
        changed += 3;

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

        if (battle.enemy != null)
        {
            Undo.RecordObject(battle.enemy, "Bind Enemy Battle HUD");
            battle.enemy.hpSlider = controller.battleLeftHPSlider;
            battle.enemy.hpText = controller.battleLeftHPText;
            EditorUtility.SetDirty(battle.enemy);
            changed += 2;
        }

        if (battle.player != null)
        {
            Undo.RecordObject(battle.player, "Bind Player Battle HUD");
            battle.player.hpSlider = controller.battleRightHPSlider;
            battle.player.hpText = controller.battleRightHPText;
            EditorUtility.SetDirty(battle.player);
            changed += 2;
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
