using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ARBookPresentationSceneSetup
{
    private const string SceneName = "PokemonGame_ARBook";
    private const string PresentationLayerName = "Presentation";
    private const string AutoRunKey = "ARBookPresentationSceneSetup_20260615_6";
    private const string BackgroundMaterialPath =
        "Assets/Materials/ARBookPresentationBackground.mat";
    private const string ChineseFontPath =
        "Assets/Fonts/SimplifiedChinese/SourceHanSansSC-Normal SDF.asset";
    private const string PlayerControllerPath =
        "Assets/Animations/Hilda_Regular_00.controller";
    private const string PlayerCinematicControllerPath =
        "Assets/Animations/Hilda_Regular_00_Cinematic.controller";

    [InitializeOnLoadMethod]
    private static void ScheduleFirstSetup()
    {
        if (SessionState.GetBool(AutoRunKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoRunKey, true);
        EditorApplication.delayCall += RunAutomaticSetup;
    }

    [MenuItem("ARBook/演出系统/创建或修复战斗与对话舞台")]
    public static void BuildOrRepair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("请先退出运行模式，再创建演出舞台。");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("当前没有可编辑的场景。");
            return;
        }

        int presentationLayer = LayerMask.NameToLayer(PresentationLayerName);
        if (presentationLayer < 0)
        {
            Debug.LogError("缺少 Presentation 层。请先在“标签和层”中创建。");
            return;
        }

        GameObject root = EnsureRoot("PresentationSystem");
        GameObject battleStage = EnsureChild(root.transform, "BattleStage");
        GameObject dialogueStage = EnsureChild(root.transform, "DialogueStage");

        GameObject battleCameraObject =
            EnsureChild(battleStage.transform, "BattleCamera");
        Camera battleCamera = EnsureCamera(battleCameraObject, presentationLayer);

        GameObject dialogueCameraObject =
            EnsureChild(dialogueStage.transform, "DialogueCamera");
        Camera dialogueCamera =
            EnsureCamera(dialogueCameraObject, presentationLayer);
        dialogueCameraObject.transform.localPosition =
            new Vector3(0f, 1.25f, -6f);
        dialogueCameraObject.transform.localRotation = Quaternion.identity;

        Transform battleLookTarget =
            EnsureChild(battleStage.transform, "CameraLookTarget").transform;
        battleLookTarget.gameObject.SetActive(true);
        battleLookTarget.localPosition = new Vector3(0f, 0.8f, 0f);

        GameObject leftCreature =
            EnsureChild(battleStage.transform, "LeftCreatureAnchor");
        GameObject rightTrainer =
            EnsureChild(battleStage.transform, "RightTrainerAnchor");
        leftCreature.SetActive(true);
        rightTrainer.SetActive(true);
        leftCreature.transform.localPosition = new Vector3(-1.7f, 0.05f, 2.8f);
        rightTrainer.transform.localPosition =
            new Vector3(3.05f, -1.25f, -1.15f);
        leftCreature.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
        rightTrainer.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);

        GameObject leftDialogue =
            EnsureChild(dialogueStage.transform, "LeftActorAnchor");
        GameObject rightDialogue =
            EnsureChild(dialogueStage.transform, "RightActorAnchor");
        leftDialogue.SetActive(true);
        rightDialogue.SetActive(true);
        leftDialogue.transform.localPosition = new Vector3(-1.35f, -1.2f, 0f);
        rightDialogue.transform.localPosition = new Vector3(1.35f, -1.2f, 0f);
        leftDialogue.transform.localRotation = Quaternion.identity;
        rightDialogue.transform.localRotation = Quaternion.identity;

        Renderer battleBackground = EnsureBackground(
            battleStage.transform,
            "FrozenBackground",
            presentationLayer);
        Renderer dialogueBackground = EnsureBackground(
            dialogueStage.transform,
            "FrozenBackground",
            presentationLayer);
        battleBackground.gameObject.SetActive(true);
        dialogueBackground.gameObject.SetActive(true);

        GameObject battleBackgroundController =
            EnsureChild(root.transform, "BattleBackgroundController");
        ARBookFrozenBackground battleFrozen =
            EnsureComponent<ARBookFrozenBackground>(battleBackgroundController);
        battleFrozen.backgroundRenderer = battleBackground;
        battleFrozen.presentationCamera = battleCamera;
        battleFrozen.backgroundDistance = 20f;
        battleFrozen.fitQuadToCamera = true;

        GameObject dialogueBackgroundController =
            EnsureChild(root.transform, "DialogueBackgroundController");
        ARBookFrozenBackground dialogueFrozen =
            EnsureComponent<ARBookFrozenBackground>(
                dialogueBackgroundController);
        dialogueFrozen.backgroundRenderer = dialogueBackground;
        dialogueFrozen.presentationCamera = dialogueCamera;
        dialogueFrozen.backgroundDistance = 20f;
        dialogueFrozen.fitQuadToCamera = true;

        GameObject cameraRigObject =
            EnsureChild(root.transform, "BattleCameraRig");
        ARBookPresentationCameraRig cameraRig =
            EnsureComponent<ARBookPresentationCameraRig>(cameraRigObject);
        cameraRig.stageCamera = battleCamera;
        cameraRig.lookTarget = battleLookTarget;
        cameraRig.lookOffset = Vector3.zero;
        cameraRig.radius = 4.6f;
        cameraRig.height = 1.35f;
        cameraRig.endAngle = 180f;
        cameraRig.orbitDuration = 3f;
        cameraRig.introRadius = 1.6f;
        cameraRig.introStartAngle = 0f;
        cameraRig.introEndAngle = 180f;
        cameraRig.introStartHeight = 0.08f;
        cameraRig.introEndHeight = 0.92f;
        cameraRig.pullBackDuration = 0.9f;
        cameraRig.SnapToEnd();

        Canvas battleCanvas = EnsureCanvas(
            battleStage.transform,
            "BattleCanvas",
            battleCamera);
        battleCanvas.gameObject.SetActive(true);
        BattleUI battleUI = BuildBattleUI(battleCanvas.transform);

        Canvas dialogueCanvas = EnsureCanvas(
            dialogueStage.transform,
            "DialogueCanvas",
            dialogueCamera);
        DialogueUI dialogueUI = BuildDialogueUI(dialogueCanvas.transform);

        ARBookPresentationActor playerActor =
            EnsureComponent<ARBookPresentationActor>(rightTrainer);
        ARBookPresentationActor enemyActor =
            EnsureComponent<ARBookPresentationActor>(leftCreature);
        ARBookBattleCombatant player =
            EnsureComponent<ARBookBattleCombatant>(rightTrainer);
        ARBookBattleCombatant enemy =
            EnsureComponent<ARBookBattleCombatant>(leftCreature);
        player.displayName = "训练家";
        player.actor = playerActor;
        player.hpSlider = battleUI.rightSlider;
        player.hpText = battleUI.rightText;
        enemy.displayName = "精灵";
        enemy.actor = enemyActor;
        enemy.hpSlider = battleUI.leftSlider;
        enemy.hpText = battleUI.leftText;

        ARBookPresentationActor leftDialogueActor =
            EnsureComponent<ARBookPresentationActor>(leftDialogue);
        ARBookPresentationActor rightDialogueActor =
            EnsureComponent<ARBookPresentationActor>(rightDialogue);

        Camera arCamera = FindSceneObject("ARCamera")?.GetComponent<Camera>();

        GameObject battleControllerObject =
            EnsureChild(root.transform, "BattleController");
        ARBookPresentationSession battleSession =
            EnsureComponent<ARBookPresentationSession>(battleControllerObject);
        battleSession.arCamera = arCamera;
        battleSession.presentationCamera = battleCamera;
        battleSession.presentationRoot = battleStage;
        battleSession.frozenBackground = battleFrozen;

        ARBookBattleController battleController =
            EnsureComponent<ARBookBattleController>(battleControllerObject);
        battleController.session = battleSession;
        battleController.cameraRig = cameraRig;
        battleController.player = player;
        battleController.enemy = enemy;
        battleController.battleControlsRoot = battleUI.controls;
        battleController.messageText = battleUI.message;
        EnsureButtonListener(
            battleUI.attackButton,
            battleController.PlayerAttack);
        EnsureButtonListener(
            battleUI.exitButton,
            battleController.ExitBattle);

        GameObject dialogueControllerObject =
            EnsureChild(root.transform, "DialogueController");
        ARBookPresentationSession dialogueSession =
            EnsureComponent<ARBookPresentationSession>(
                dialogueControllerObject);
        dialogueSession.arCamera = arCamera;
        dialogueSession.presentationCamera = dialogueCamera;
        dialogueSession.presentationRoot = dialogueStage;
        dialogueSession.frozenBackground = dialogueFrozen;

        ARBookCinematicDialogueController dialogueController =
            EnsureComponent<ARBookCinematicDialogueController>(
                dialogueControllerObject);
        dialogueController.session = dialogueSession;
        dialogueController.leftActor = leftDialogueActor;
        dialogueController.rightActor = rightDialogueActor;
        dialogueController.dialogueUIRoot = dialogueUI.dialogueBox;
        dialogueController.speakerNameText = dialogueUI.speakerName;
        dialogueController.dialogueText = dialogueUI.dialogueText;
        dialogueController.leftSpeakerHighlight = dialogueUI.leftHighlight;
        dialogueController.rightSpeakerHighlight = dialogueUI.rightHighlight;
        dialogueController.activeSpeakerColor =
            new Color(1f, 1f, 1f, 0.12f);
        dialogueController.inactiveSpeakerColor =
            new Color(0f, 0f, 0f, 0.28f);
        EnsureButtonListener(
            dialogueUI.continueButton,
            dialogueController.ContinueDialogue);

        ARBookPresentationDirector director =
            EnsureComponent<ARBookPresentationDirector>(root);
        director.battleController = battleController;
        director.dialogueController = dialogueController;
        director.battleOpponentAnchor = leftCreature.transform;
        director.battlePlayerAnchor = rightTrainer.transform;
        director.dialogueLeftAnchor = leftDialogue.transform;
        director.dialogueRightAnchor = rightDialogue.transform;
        director.battleOpponentHeight = 1.35f;
        director.battlePlayerHeight = 4.2f;
        director.dialogueActorHeight = 3.6f;
        director.useUnlitBattleMaterials = true;
        director.useUnlitDialogueMaterials = true;
        director.battleOpponentYawCorrection = 0f;
        director.battlePlayerYawCorrection = 150f;
        director.playerPresentationController =
            EnsurePlayerCinematicController();

        if (dialogueController.lines == null ||
            dialogueController.lines.Length == 0)
        {
            dialogueController.lines =
                new ARBookCinematicDialogueController.DialogueLine[]
                {
                    new ARBookCinematicDialogueController.DialogueLine
                    {
                        speakerSide =
                            ARBookCinematicDialogueController.SpeakerSide.Left,
                        speakerName = "对话角色",
                        text = "请在 DialogueController 中修改这句对话。",
                        leftActorState = "Greeting",
                        rightActorState = "Idle"
                    },
                    new ARBookCinematicDialogueController.DialogueLine
                    {
                        speakerSide =
                            ARBookCinematicDialogueController.SpeakerSide.Right,
                        speakerName = "训练家",
                        text = "角色和动画配置完成后，在这里测试。",
                        leftActorState = "Idle",
                        rightActorState = "Speak"
                    }
                };
        }

        EnsureEventSystem();
        StyleMainGameUI();
        SetLayerRecursively(battleStage, presentationLayer);
        SetLayerRecursively(dialogueStage, presentationLayer);

        battleCamera.enabled = false;
        dialogueCamera.enabled = false;
        battleStage.SetActive(false);
        dialogueStage.SetActive(false);

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        Debug.Log(
            "战斗与对话基础舞台已创建/修复。下一步需要放入四个角色模型并确认镜头构图。");
    }

    private static void RunAutomaticSetup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid() && scene.isLoaded && scene.name == SceneName)
        {
            BuildOrRepair();
        }
    }

    private static GameObject EnsureRoot(string name)
    {
        GameObject existing = FindSceneObject(name);
        if (existing != null)
        {
            return existing;
        }

        GameObject created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, $"创建 {name}");
        return created;
    }

    private static GameObject EnsureChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, $"创建 {name}");
        created.transform.SetParent(parent, false);
        return created;
    }

    private static T EnsureComponent<T>(GameObject target)
        where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static Camera EnsureCamera(
        GameObject target,
        int presentationLayer)
    {
        target.SetActive(true);
        Camera camera = EnsureComponent<Camera>(target);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 1 << presentationLayer;
        camera.depth = 10f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 100f;

        AudioListener listener = target.GetComponent<AudioListener>();
        if (listener != null)
        {
            Undo.DestroyObjectImmediate(listener);
        }

        return camera;
    }

    private static Renderer EnsureBackground(
        Transform stage,
        string name,
        int presentationLayer)
    {
        Transform existing = stage.Find(name);
        GameObject background;
        if (existing == null)
        {
            background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Undo.RegisterCreatedObjectUndo(background, $"创建 {name}");
            background.name = name;
            background.transform.SetParent(stage, false);
        }
        else
        {
            background = existing.gameObject;
        }

        Collider collider = background.GetComponent<Collider>();
        if (collider != null)
        {
            Undo.DestroyObjectImmediate(collider);
        }

        background.layer = presentationLayer;
        Renderer renderer = background.GetComponent<Renderer>();
        if (renderer == null)
        {
            MeshFilter filter = EnsureComponent<MeshFilter>(background);
            GameObject temporary =
                GameObject.CreatePrimitive(PrimitiveType.Quad);
            filter.sharedMesh = temporary.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temporary);
            renderer = EnsureComponent<MeshRenderer>(background);
        }

        renderer.sharedMaterial = EnsureBackgroundMaterial();
        renderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private static Material EnsureBackgroundMaterial()
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(BackgroundMaterialPath);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        material = new Material(shader)
        {
            name = "ARBookPresentationBackground"
        };
        AssetDatabase.CreateAsset(material, BackgroundMaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static Canvas EnsureCanvas(
        Transform stage,
        string name,
        Camera camera)
    {
        Transform existing = stage.Find(name);
        GameObject canvasObject;
        if (existing != null &&
            existing.GetComponent<RectTransform>() == null)
        {
            int siblingIndex = existing.GetSiblingIndex();
            Undo.DestroyObjectImmediate(existing.gameObject);
            canvasObject = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(canvasObject, $"创建 {name}");
            canvasObject.transform.SetParent(stage, false);
            canvasObject.transform.SetSiblingIndex(siblingIndex);
        }
        else
        {
            canvasObject = EnsureChild(stage, name);
        }

        RectTransform rect = EnsureRectTransform(canvasObject);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Canvas canvas = EnsureComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        EnsureComponent<GraphicRaycaster>(canvasObject);
        return canvas;
    }

    private static BattleUI BuildBattleUI(Transform canvas)
    {
        TMP_Text message = EnsureText(
            canvas,
            "TopMessage",
            "战斗开始",
            42f,
            TextAlignmentOptions.Center,
            new Vector2(0.25f, 0.84f),
            new Vector2(0.75f, 0.96f));

        GameObject leftStatus = EnsurePanel(
            canvas,
            "LeftStatus",
            new Vector2(0.04f, 0.76f),
            new Vector2(0.34f, 0.91f),
            new Color(0.04f, 0.06f, 0.08f, 0.78f));
        TMP_Text leftText = EnsureText(
            leftStatus.transform,
            "NameAndHP",
            "精灵  100 / 100",
            27f,
            TextAlignmentOptions.Left,
            new Vector2(0.05f, 0.48f),
            new Vector2(0.95f, 0.92f));
        Slider leftSlider = EnsureSlider(
            leftStatus.transform,
            "HPSlider",
            new Vector2(0.05f, 0.14f),
            new Vector2(0.95f, 0.38f));

        GameObject rightStatus = EnsurePanel(
            canvas,
            "RightStatus",
            new Vector2(0.66f, 0.76f),
            new Vector2(0.96f, 0.91f),
            new Color(0.04f, 0.06f, 0.08f, 0.78f));
        TMP_Text rightText = EnsureText(
            rightStatus.transform,
            "NameAndHP",
            "训练家  100 / 100",
            27f,
            TextAlignmentOptions.Left,
            new Vector2(0.05f, 0.48f),
            new Vector2(0.95f, 0.92f));
        Slider rightSlider = EnsureSlider(
            rightStatus.transform,
            "HPSlider",
            new Vector2(0.05f, 0.14f),
            new Vector2(0.95f, 0.38f));

        GameObject controls = EnsureUIObject(
            canvas,
            "BattleControls",
            new Vector2(0.34f, 0.035f),
            new Vector2(0.66f, 0.19f));
        Button attack = EnsureButton(
            controls.transform,
            "AttackButton",
            "攻击",
            new Vector2(0f, 0f),
            new Vector2(0.78f, 1f),
            new Color(0.72f, 0.13f, 0.12f, 0.95f));
        Button exit = EnsureButton(
            controls.transform,
            "ExitButton",
            "退出",
            new Vector2(0.82f, 0f),
            new Vector2(1f, 1f),
            new Color(0.12f, 0.15f, 0.18f, 0.95f));

        return new BattleUI
        {
            message = message,
            leftText = leftText,
            rightText = rightText,
            leftSlider = leftSlider,
            rightSlider = rightSlider,
            controls = controls,
            attackButton = attack,
            exitButton = exit
        };
    }

    private static DialogueUI BuildDialogueUI(Transform canvas)
    {
        Image leftHighlight = EnsureImage(
            canvas,
            "LeftSpeakerHighlight",
            new Vector2(0f, 0.22f),
            new Vector2(0.5f, 1f),
            new Color(1f, 1f, 1f, 0.12f));
        Image rightHighlight = EnsureImage(
            canvas,
            "RightSpeakerHighlight",
            new Vector2(0.5f, 0.22f),
            new Vector2(1f, 1f),
            new Color(0f, 0f, 0f, 0.28f));

        GameObject dialogueBox = EnsurePanel(
            canvas,
            "DialogueBox",
            new Vector2(0.05f, 0.04f),
            new Vector2(0.95f, 0.27f),
            new Color(0.025f, 0.035f, 0.045f, 0.92f));
        TMP_Text speakerName = EnsureText(
            dialogueBox.transform,
            "SpeakerNameText",
            "角色名",
            31f,
            TextAlignmentOptions.Left,
            new Vector2(0.035f, 0.66f),
            new Vector2(0.75f, 0.94f));
        TMP_Text dialogueText = EnsureText(
            dialogueBox.transform,
            "DialogueText",
            "对话内容",
            27f,
            TextAlignmentOptions.TopLeft,
            new Vector2(0.035f, 0.12f),
            new Vector2(0.82f, 0.66f));
        Button continueButton = EnsureButton(
            dialogueBox.transform,
            "ContinueButton",
            "继续",
            new Vector2(0.84f, 0.16f),
            new Vector2(0.97f, 0.62f),
            new Color(0.12f, 0.48f, 0.58f, 0.96f));

        return new DialogueUI
        {
            dialogueBox = dialogueBox,
            speakerName = speakerName,
            dialogueText = dialogueText,
            leftHighlight = leftHighlight,
            rightHighlight = rightHighlight,
            continueButton = continueButton
        };
    }

    private static GameObject EnsurePanel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        Image image = EnsureImage(
            parent,
            name,
            anchorMin,
            anchorMax,
            color);
        return image.gameObject;
    }

    private static Image EnsureImage(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        GameObject target =
            EnsureUIObject(parent, name, anchorMin, anchorMax);
        Image image = EnsureComponent<Image>(target);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text EnsureText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject target =
            EnsureUIObject(parent, name, anchorMin, anchorMax);
        TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(target);
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        TMP_FontAsset chineseFont =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
        if (chineseFont != null)
        {
            text.font = chineseFont;
        }

        return text;
    }

    private static Slider EnsureSlider(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject target =
            EnsureUIObject(parent, name, anchorMin, anchorMax);
        Slider slider = EnsureComponent<Slider>(target);
        slider.transition = Selectable.Transition.None;

        Image background = EnsureImage(
            target.transform,
            "Background",
            Vector2.zero,
            Vector2.one,
            new Color(0.12f, 0.13f, 0.14f, 1f));
        Image fill = EnsureImage(
            target.transform,
            "Fill",
            Vector2.zero,
            Vector2.one,
            new Color(0.2f, 0.82f, 0.34f, 1f));
        RectTransform fillRect = fill.rectTransform;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        slider.targetGraphic = background;
        slider.fillRect = fillRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;
        return slider;
    }

    private static Button EnsureButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        GameObject target =
            EnsureUIObject(parent, name, anchorMin, anchorMax);
        Image image = EnsureComponent<Image>(target);
        image.color = color;
        image.raycastTarget = true;

        Button button = EnsureComponent<Button>(target);
        button.targetGraphic = image;

        EnsureText(
            target.transform,
            "Label",
            label,
            29f,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one);
        return button;
    }

    private static GameObject EnsureUIObject(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        Transform existing = parent.Find(name);
        GameObject target;
        if (existing != null)
        {
            target = existing.gameObject;
        }
        else
        {
            target = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(target, $"创建 {name}");
            target.transform.SetParent(parent, false);
        }

        RectTransform rect = EnsureRectTransform(target);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        return target;
    }

    private static RectTransform EnsureRectTransform(GameObject target)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect != null)
        {
            return rect;
        }

        Debug.LogError(
            $"{target.name} 不是 UI 对象，无法配置 RectTransform。",
            target);
        return null;
    }

    private static void EnsureButtonListener(
        Button button,
        UnityEngine.Events.UnityAction listener)
    {
        if (button == null || button.onClick.GetPersistentEventCount() > 0)
        {
            return;
        }

        UnityEventTools.AddPersistentListener(button.onClick, listener);
        EditorUtility.SetDirty(button);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>(
                FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "创建 EventSystem");
    }

    private static GameObject FindSceneObject(string name)
    {
        GameObject[] objects = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (GameObject target in objects)
        {
            if (target.scene == SceneManager.GetActiveScene() &&
                target.name == name)
            {
                return target;
            }
        }

        return null;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void StyleMainGameUI()
    {
        ARBookChapterHUDController hud =
            Object.FindFirstObjectByType<ARBookChapterHUDController>(
                FindObjectsInactive.Include);
        if (hud == null)
        {
            return;
        }

        Canvas canvas = hud.GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            return;
        }

        TMP_FontAsset font =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
        Color panelColor = new Color(0.025f, 0.035f, 0.045f, 0.88f);
        Color secondaryPanelColor =
            new Color(0.035f, 0.05f, 0.06f, 0.82f);
        Color cyan = new Color(0.12f, 0.55f, 0.64f, 0.98f);
        Color red = new Color(0.72f, 0.13f, 0.12f, 0.98f);

        GameObject questPanel = EnsurePanel(
            canvas.transform,
            "QuestHUDPanel",
            new Vector2(0.025f, 0.74f),
            new Vector2(0.45f, 0.965f),
            panelColor);
        questPanel.transform.SetAsFirstSibling();
        ReparentAndLayoutText(
            hud.questText,
            questPanel.transform,
            new Vector2(0.04f, 0.42f),
            new Vector2(0.96f, 0.93f),
            28f,
            TextAlignmentOptions.TopLeft,
            font);
        ReparentAndLayoutText(
            hud.challengeText,
            questPanel.transform,
            new Vector2(0.04f, 0.08f),
            new Vector2(0.96f, 0.4f),
            23f,
            TextAlignmentOptions.TopLeft,
            font);

        GameObject progressPanel = EnsurePanel(
            canvas.transform,
            "ChapterProgressPanel",
            new Vector2(0.66f, 0.81f),
            new Vector2(0.975f, 0.965f),
            secondaryPanelColor);
        progressPanel.transform.SetAsFirstSibling();
        ReparentAndLayoutText(
            hud.chapterProgressText,
            progressPanel.transform,
            new Vector2(0.05f, 0.12f),
            new Vector2(0.95f, 0.88f),
            23f,
            TextAlignmentOptions.TopLeft,
            font);

        Transform actionButtons = canvas.transform.Find("ActionButtons");
        if (actionButtons != null)
        {
            RectTransform actionRect =
                actionButtons.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0.68f, 0.045f);
            actionRect.anchorMax = new Vector2(0.975f, 0.175f);
            actionRect.offsetMin = Vector2.zero;
            actionRect.offsetMax = Vector2.zero;
            Image actionBackground =
                EnsureComponent<Image>(actionButtons.gameObject);
            actionBackground.color = secondaryPanelColor;
            actionBackground.raycastTarget = false;

            StyleExistingButton(
                actionButtons.Find("CaptureButton"),
                red,
                new Vector2(0.04f, 0.14f),
                new Vector2(0.48f, 0.86f),
                font);
            StyleExistingButton(
                actionButtons.Find("InteractButton"),
                cyan,
                new Vector2(0.52f, 0.14f),
                new Vector2(0.96f, 0.86f),
                font);
        }

        Transform oldDialogue = canvas.transform.Find("DialoguePanel");
        if (oldDialogue != null)
        {
            RectTransform dialogueRect =
                oldDialogue.GetComponent<RectTransform>();
            dialogueRect.anchorMin = new Vector2(0.05f, 0.04f);
            dialogueRect.anchorMax = new Vector2(0.95f, 0.27f);
            dialogueRect.offsetMin = Vector2.zero;
            dialogueRect.offsetMax = Vector2.zero;

            Image dialogueImage = oldDialogue.GetComponent<Image>();
            if (dialogueImage != null)
            {
                dialogueImage.color = panelColor;
            }

            TMP_Text speaker =
                oldDialogue.Find("SpeakerNameText")
                    ?.GetComponent<TMP_Text>();
            TMP_Text body =
                oldDialogue.Find("DialogueText")
                    ?.GetComponent<TMP_Text>();
            ReparentAndLayoutText(
                speaker,
                oldDialogue,
                new Vector2(0.035f, 0.66f),
                new Vector2(0.78f, 0.94f),
                29f,
                TextAlignmentOptions.Left,
                font);
            ReparentAndLayoutText(
                body,
                oldDialogue,
                new Vector2(0.035f, 0.12f),
                new Vector2(0.82f, 0.66f),
                25f,
                TextAlignmentOptions.TopLeft,
                font);
            StyleExistingButton(
                oldDialogue.Find("ContinueButton"),
                cyan,
                new Vector2(0.84f, 0.16f),
                new Vector2(0.97f, 0.62f),
                font);
        }
    }

    private static void ReparentAndLayoutText(
        TMP_Text text,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        TextAlignmentOptions alignment,
        TMP_FontAsset font)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        if (font != null)
        {
            text.font = font;
        }
    }

    private static void StyleExistingButton(
        Transform buttonTransform,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        TMP_FontAsset font)
    {
        if (buttonTransform == null)
        {
            return;
        }

        RectTransform rect = buttonTransform.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonTransform.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }

        TMP_Text label =
            buttonTransform.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontSize = 25f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            if (font != null)
            {
                label.font = font;
            }
        }
    }

    private static RuntimeAnimatorController EnsurePlayerCinematicController()
    {
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                PlayerCinematicControllerPath);
        if (controller != null)
        {
            return controller;
        }

        if (!AssetDatabase.CopyAsset(
                PlayerControllerPath,
                PlayerCinematicControllerPath))
        {
            Debug.LogError(
                $"Could not create {PlayerCinematicControllerPath}.");
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                PlayerControllerPath);
        }

        AssetDatabase.ImportAsset(
            PlayerCinematicControllerPath,
            ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            PlayerCinematicControllerPath);
    }

    private sealed class BattleUI
    {
        public TMP_Text message;
        public TMP_Text leftText;
        public TMP_Text rightText;
        public Slider leftSlider;
        public Slider rightSlider;
        public GameObject controls;
        public Button attackButton;
        public Button exitButton;
    }

    private sealed class DialogueUI
    {
        public GameObject dialogueBox;
        public TMP_Text speakerName;
        public TMP_Text dialogueText;
        public Image leftHighlight;
        public Image rightHighlight;
        public Button continueButton;
    }
}
