using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UIImage = UnityEngine.UI.Image;
using VuforiaObserverBehaviour = Vuforia.ObserverBehaviour;
using VuforiaStatus = Vuforia.Status;

public class ARBookGameShellController : MonoBehaviour
{
    [Serializable]
    public class CompanionDefinition
    {
        [Tooltip("收服存档 ID，需要和 ARBookInteractable.captureId 一致。")]
        public string captureId;
        [Tooltip("陪伴/背包 UI 显示名。")]
        public string displayName;
        [Tooltip("陪伴模式扫描的 SinglePokemon ImageTarget 名字。为空时默认使用 captureId。")]
        public string imageTargetName;
        [Tooltip("可选。手动绑定 UI 卡片图片 Sprite。优先级低于 Portrait Texture。")]
        public Sprite portrait;
        [Tooltip("推荐手动绑定这里：把 Vuforia/ImageTargetTextures/mcfAR 下对应 jpg 直接拖进来。")]
        public Texture2D portraitTexture;
        [Tooltip("可选。没有 SinglePokemon 场景模型时使用的陪伴模型预制体。")]
        public GameObject companionPrefab;
        [Tooltip("可选。没有 SinglePokemon 场景模型时使用的场景模型对象。")]
        public GameObject sceneObject;
    }

    [Header("Data")]
    public ARBookCollectionManager collectionManager;
    public ARBookChapterProgress chapterProgress;
    public ARBookDebugProgressResetter progressResetter;
    public ARBookChapterHUDController chapterHudController;
    public CompanionDefinition[] companions;

    [Header("UI Assets")]
    public TMP_FontAsset chineseFont;
    public Sprite playerAvatarSprite;
    public string playerName = "\u8bad\u7ec3\u5bb6";
    [Range(1, 999)] public int maxHP = 100;
    [Range(0, 999)] public int currentHP = 100;

    [Header("Runtime")]
    public bool showCoverOnStart = true;
    public bool hideLegacyChapterHudTexts = true;
    public Canvas rootCanvas;
    public Transform companionPlacementRoot;
    [Range(1, 6)] public int maxActiveCompanions = 3;
    public float companionSpacing = 0.85f;
    public Vector3 companionLocalOffset = new Vector3(0f, 0f, 1.2f);
    public Vector3 companionImageTargetLocalOffset = Vector3.zero;
    public bool hideCompanionUntilImageTracked = true;
    [Range(1, 25)] public int companionInteractionAffectionGain = 5;
    [Range(1, 20)] public int maxCompanionInteractionsPerGame = 5;
    public string singlePokemonRootName = "SinglePokemon";

    [Header("Scene UI References")]
    public RectTransform generatedRoot;
    public RectTransform homeRoot;
    public RectTransform hudRoot;
    public RectTransform companionRoot;
    public RectTransform companionGrid;
    public RectTransform backpackRoot;
    public RectTransform dialogueRoot;
    public RectTransform battleRoot;
    public RectTransform actionButtonsRoot;
    public TMP_Text startButtonText;
    public TMP_Text questText;
    public TMP_Text progressText;
    public TMP_Text capturedCountText;
    public TMP_Text companionDetailText;
    public TMP_Text dialogueSpeakerText;
    public TMP_Text dialogueBodyText;
    public TMP_Text battleMessageText;
    public TMP_Text battleLeftHPText;
    public TMP_Text battleRightHPText;
    public TMP_Text companionCameraStatusText;
    public UIImage hpFill;
    public UIImage companionMoodFill;
    public UIImage dialogueLeftHighlight;
    public UIImage dialogueRightHighlight;
    public Button startButton;
    public Button restartButton;
    public Button homeCompanionButton;
    public Button backpackButton;
    public Button hudCompanionButton;
    public Button homeButton;
    public Button placeButton;
    public Button affectionButton;
    public Button clearCompanionsButton;
    public Button closeCompanionButton;
    public Button closeBackpackButton;
    public Button dialogueContinueButton;
    public Button battleAttackButton;
    public Button battleExitButton;
    public Button companionCameraInteractButton;
    public Button companionReturnGameButton;
    public Button companionReturnHomeButton;
    public Slider battleLeftHPSlider;
    public Slider battleRightHPSlider;

    private const string StartedKey = "ARBookHasStarted";
    private const string CapturedIdsKey = "CapturedIds";
    private const string AffectionPrefix = "CompanionAffection_";
    private const string CompanionInteractionCountKey = "CompanionInteractionCount_CurrentGame";

    private GameObject runtimeEventSystem;

    private readonly HashSet<string> selectedCompanionIds = new HashSet<string>();
    private readonly Dictionary<string, GameObject> placedCompanions =
        new Dictionary<string, GameObject>();
    private string activeCompanionId;
    private VuforiaObserverBehaviour activeCompanionTarget;
    private GameObject activeSceneCompanionModel;
    private bool companionCameraModeActive;
    private bool singlePokemonCameraModeActive;
    private string selectedBackpackPartyId;
    private float nextCompanionTargetLookupTime;
    private float nextRefreshTime;
    private TMP_Text affectionButtonText;

    public bool IsHudVisible =>
        hudRoot != null &&
        hudRoot.gameObject.activeInHierarchy &&
        (homeRoot == null || !homeRoot.gameObject.activeInHierarchy) &&
        (companionRoot == null || !companionRoot.gameObject.activeInHierarchy) &&
        (backpackRoot == null || !backpackRoot.gameObject.activeInHierarchy) &&
        (dialogueRoot == null || !dialogueRoot.gameObject.activeInHierarchy) &&
        (battleRoot == null || !battleRoot.gameObject.activeInHierarchy);

    private void Reset()
    {
        ResetCatalogToDefault();
    }

    private void Awake()
    {
        ResolveReferences();
        maxActiveCompanions = 1;
        EnsureCatalog();
        EnsureCanvas();
        BindSceneInterface();
        if (generatedRoot == null || generatedRoot.childCount == 0)
        {
            RebuildSceneInterface();
        }
        else
        {
            EnsureIntegratedOverlayInterface();
            WireSceneButtons();
        }

        HideTransientUi();
        HideLegacyHud();
    }

    private void Start()
    {
        if (showCoverOnStart)
        {
            ShowHome();
        }
        else
        {
            BeginGame();
        }

        RefreshAll();
    }

    private void Update()
    {
        RefreshPlacedCompanionTracking();
        HandleCompanionScreenInput();

        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        RefreshHud();
        nextRefreshTime = Time.unscaledTime + 0.25f;
    }

    private void OnDestroy()
    {
        DestroyPlacedCompanions(true);

        if (runtimeEventSystem != null)
        {
            DestroyRuntimeObject(runtimeEventSystem, true);
            runtimeEventSystem = null;
        }
    }

    [ContextMenu("Reset Catalog To Default")]
    public void ResetCatalogToDefault()
    {
        companions = new[]
        {
            CreateCompanion("Bulbasaur", "Bulbasaur"),
            CreateCompanion("Talonflame", "Talonflame"),
            CreateCompanion("Axew", "Axew"),
            CreateCompanion("Pikachu", "Pikachu"),
            CreateCompanion("Meowth", "Meowth"),
            CreateCompanion("Infernape", "Infernape"),
            CreateCompanion("Squirtle", "Squirtle"),
            CreateCompanion("Jirachi", "Jirachi"),
            CreateCompanion("Sneasler", "Sneasler"),
            CreateCompanion("Zorua", "Zorua"),
            CreateCompanion("Zekrom", "Zekrom"),
            CreateCompanion("Zygarde10", "Zygarde10"),
            CreateCompanion("Toxtricity", "Toxtricity"),
            CreateCompanion("Scizor", "Scizor"),
            CreateCompanion("Mismagius", "Mismagius"),
            CreateCompanion("Mew", "Mew"),
            CreateCompanion("Manaphy", "Manaphy"),
            CreateCompanion("ElectrodeHisuian", "ElectrodeHisuian"),
            CreateCompanion("Dragapult", "Dragapult"),
            CreateCompanion("Celebi", "Celebi")
        };
    }
#if false
/*
        companions = new[]
        {
            CreateCompanion("Bulbasaur", "濡欒洐绉嶅瓙"),
            CreateCompanion("Talonflame", "鐑堢楣?),
            CreateCompanion("Axew", "鐗欑墮"),
            CreateCompanion("Pikachu", "鐨崱涓?),
            CreateCompanion("Meowth", "鍠靛柕"),
            CreateCompanion("Infernape", "鐑堢劙鐚?),
            CreateCompanion("Squirtle", "鏉板凹榫?),
            CreateCompanion("Jirachi", "鍩烘媺绁?),
            CreateCompanion("Sneasler", "鐙冩媺"),
            CreateCompanion("Zorua", "绱㈢綏浜?),
            CreateCompanion("Zekrom", "鎹峰厠缃楀"),
            CreateCompanion("Zygarde10", "鍩烘牸灏斿痉10%褰㈡€?),
            CreateCompanion("Toxtricity", "棰ゅ鸡铦捐瀳"),
            CreateCompanion("Scizor", "宸ㄩ挸铻宠瀭"),
            CreateCompanion("Mismagius", "姊﹀榄?),
            CreateCompanion("Mew", "姊﹀够"),
            CreateCompanion("Manaphy", "鐜涚撼闇?),
            CreateCompanion("ElectrodeHisuian", "闇归洺鐢电悆锛堟礂缈犵殑鏍峰瓙锛?),
            CreateCompanion("Dragapult", "澶氶緳宸撮瞾鎵?),
            CreateCompanion("Celebi", "鏃舵媺姣?)
        };
*/
        companions = new[]
        {
            CreateCompanion("Bulbasaur", "濡欒洐绉嶅瓙"),
            CreateCompanion("Talonflame", "鐑堢楣?),
            CreateCompanion("Axew", "鐗欑墮"),
            CreateCompanion("Pikachu", "鐨崱涓?),
            CreateCompanion("Meowth", "鍠靛柕"),
            CreateCompanion("Infernape", "鐑堢劙鐚?),
            CreateCompanion("Squirtle", "鏉板凹榫?),
            CreateCompanion("Jirachi", "鍩烘媺绁?),
            CreateCompanion("Sneasler", "鐙冩媺"),
            CreateCompanion("Zorua", "绱㈢綏浜?),
            CreateCompanion("Zekrom", "鎹峰厠缃楀"),
            CreateCompanion("Zygarde10", "鍩烘牸灏斿痉10%褰㈡€?),
            CreateCompanion("Toxtricity", "棰ゅ鸡铦捐瀳"),
            CreateCompanion("Scizor", "宸ㄩ挸铻宠瀭"),
            CreateCompanion("Mismagius", "姊﹀榄?),
            CreateCompanion("Mew", "姊﹀够"),
            CreateCompanion("Manaphy", "鐜涚撼闇?),
            CreateCompanion("ElectrodeHisuian", "闇归洺鐢电悆锛堟礂缈犵殑鏍峰瓙锛?),
            CreateCompanion("Dragapult", "澶氶緳宸撮瞾鎵?),
            CreateCompanion("Celebi", "鏃舵媺姣?)
        };
    }

#endif

    public void ShowHome()
    {
        ExitCompanionCameraMode();
        SetRootActive(homeRoot, true);
        SetRootActive(hudRoot, false);
        SetRootActive(companionRoot, false);
        SetRootActive(backpackRoot, false);
        HideTransientUi();
        HideActionButtons();
        RefreshHome();
    }

    public void BeginGame()
    {
        PlayerPrefs.SetInt(StartedKey, 1);
        PlayerPrefs.Save();
        ExitCompanionCameraMode();

        SetRootActive(homeRoot, false);
        SetRootActive(hudRoot, true);
        SetRootActive(companionRoot, false);
        SetRootActive(backpackRoot, false);
        HideTransientUi();
        RefreshAll();
    }

    public void RestartGame()
    {
        if (progressResetter != null)
        {
            progressResetter.ClearARBookProgress();
        }

        if (collectionManager != null)
        {
            collectionManager.ClearCollection();
        }

        ClearCompanionState();
        PlayerPrefs.DeleteKey(StartedKey);
        PlayerPrefs.Save();
        DespawnAllCompanions();
        ShowHome();
    }

    public void OpenCompanionMode()
    {
        SetRootActive(homeRoot, false);
        SetRootActive(hudRoot, false);
        SetRootActive(backpackRoot, false);
        SetRootActive(companionRoot, true);
        HideTransientUi();
        HideActionButtons();
        selectedCompanionIds.Clear();
        if (!string.IsNullOrWhiteSpace(activeCompanionId))
        {
            selectedCompanionIds.Add(activeCompanionId);
        }
        BuildCompanionGrid();
        RefreshCompanionDetail();
        ApplyCompanionCameraHud(false);
    }

    public void OpenCapturedSinglePokemonMode()
    {
        PlayerPrefs.SetInt(StartedKey, 1);
        PlayerPrefs.Save();
        DespawnAllCompanions();
        activeCompanionId = null;
        activeCompanionTarget = null;
        activeSceneCompanionModel = null;
        selectedCompanionIds.Clear();
        ApplyCompanionCameraHud(false);
        ApplySinglePokemonCameraHud(true);
        SetRootActive(homeRoot, false);
        SetRootActive(companionRoot, false);
        SetRootActive(backpackRoot, false);
        SetRootActive(hudRoot, true);
        HideTransientUi();
        HideActionButtons();
        EnsureMainCameraRendering();
        SetNonSinglePokemonTargetsActive(false);
        SetAllSinglePokemonTargetsActive();
        RefreshHud();
    }

    public void CloseCompanionMode()
    {
        ApplyCompanionCameraHud(false);
        SetNonSinglePokemonTargetsActive(true);
        SetRootActive(companionRoot, false);
        if (PlayerPrefs.GetInt(StartedKey, 0) == 1)
        {
            SetRootActive(hudRoot, true);
        }
        else
        {
            ShowHome();
        }
    }

    public void OpenBackpack()
    {
        OpenCompanionMode();
    }

    public void CloseBackpack()
    {
        SetRootActive(backpackRoot, false);
    }

    public void ApplyDefaultUiVisibility()
    {
        BindSceneInterface();
        ApplyCompanionCameraHud(false);
        SetRootActive(homeRoot, showCoverOnStart);
        SetRootActive(hudRoot, !showCoverOnStart);
        SetRootActive(companionRoot, false);
        SetRootActive(backpackRoot, false);
        HideTransientUi();
        HideActionButtons();
    }

    public void PlaceSelectedCompanions()
    {
        string captureId = GetSelectedCompanionId();
        if (string.IsNullOrWhiteSpace(captureId))
        {
            RefreshCompanionDetail();
            return;
        }

        CompanionDefinition definition = FindCompanion(captureId);
        if (definition == null)
        {
            RefreshCompanionDetail();
            return;
        }

        DestroyPlacedCompanions(false);
        activeCompanionId = captureId;
        activeCompanionTarget = FindCompanionImageTarget(definition);
        if (activeCompanionTarget == null)
        {
            companionDetailText.text =
                $"没有找到 {GetCompanionTargetName(definition)} 的单张识别图。";
            return;
        }

        SetSinglePokemonTargetsActive(activeCompanionTarget);
        SetNonSinglePokemonTargetsActive(false);
        GameObject instance = CreateCompanionInstance(definition, 0);
        if (instance != null)
        {
            placedCompanions[captureId] = instance;
            ConfigureCompanionInstance(instance, definition);
            RefreshPlacedCompanionTracking(true);
        }
        else
        {
            Debug.LogWarning(
                $"陪伴模式：{captureId} 找到了识别图 {activeCompanionTarget.TargetName}，但没有找到可显示的模型。",
                this);
        }

        RefreshCompanionDetail();
        CloseCompanionModeToCamera();
    }

    public void BeginSelectedCompanionMode()
    {
        AddAffectionToSelected();
    }

    private void CloseCompanionModeToCamera()
    {
        PlayerPrefs.SetInt(StartedKey, 1);
        PlayerPrefs.Save();
        SetRootActive(homeRoot, false);
        SetRootActive(companionRoot, false);
        SetRootActive(backpackRoot, false);
        SetRootActive(hudRoot, true);
        HideTransientUi();
        HideActionButtons();
        EnsureMainCameraRendering();
        ApplyCompanionCameraHud(true);
        RefreshHud();
    }

    public void ReturnFromCompanionToGame()
    {
        ExitCompanionCameraMode();
        SetRootActive(homeRoot, false);
        SetRootActive(companionRoot, false);
        SetRootActive(backpackRoot, false);
        SetRootActive(hudRoot, true);
        RefreshAll();
    }

    private void ExitCompanionCameraMode()
    {
        DespawnAllCompanions();
        activeCompanionId = null;
        activeCompanionTarget = null;
        activeSceneCompanionModel = null;
        selectedCompanionIds.Clear();
        ApplyCompanionCameraHud(false);
        ApplySinglePokemonCameraHud(false);
        SetNonSinglePokemonTargetsActive(true);
        EnsureMainCameraRendering();
        SetSinglePokemonTargetsActive(null);
    }

#if false
    private void PlaceSelectedCompanionsDisabled()
    {
        int placedCount = placedCompanions.Count;
        foreach (string captureId in selectedCompanionIds)
        {
            if (placedCount >= maxActiveCompanions)
            {
                break;
            }

            if (placedCompanions.ContainsKey(captureId))
            {
                continue;
            }

            CompanionDefinition definition = FindCompanion(captureId);
            GameObject instance = CreateCompanionInstance(definition, placedCount);
            if (instance == null)
            {
                continue;
            }

            placedCompanions[captureId] = instance;
            placedCount++;
        }

        RefreshCompanionDetail();
    }
#endif

    public void DespawnAllCompanions()
    {
        DestroyPlacedCompanions(false);
        ApplyCompanionCameraHud(false);
        SetNonSinglePokemonTargetsActive(true);
        SetSinglePokemonTargetsActive(null);
        RefreshCompanionDetail();
    }

    private void DestroyPlacedCompanions(bool immediate)
    {
        foreach (KeyValuePair<string, GameObject> pair in placedCompanions)
        {
            if (pair.Value == null)
            {
                continue;
            }

            if (pair.Value == activeSceneCompanionModel)
            {
                pair.Value.SetActive(false);
            }
            else
            {
                DestroyRuntimeObject(pair.Value, immediate);
            }
        }

        placedCompanions.Clear();
        activeCompanionId = null;
        activeCompanionTarget = null;
        activeSceneCompanionModel = null;
        SetSinglePokemonTargetsActive(null);
    }

    private static void DestroyRuntimeObject(GameObject target, bool immediate)
    {
        if (target == null)
        {
            return;
        }

        if (immediate || !Application.isPlaying)
        {
            DestroyImmediate(target);
        }
        else
        {
            Destroy(target);
        }
    }

    public void AddAffectionToSelected()
    {
        string captureId = GetSelectedCompanionId();
        if (string.IsNullOrWhiteSpace(captureId))
        {
            captureId = activeCompanionId;
        }

        if (!string.IsNullOrWhiteSpace(captureId))
        {
            if (!TryConsumeCompanionInteraction())
            {
                RefreshCompanionDetail();
                return;
            }

            AddAffection(captureId, companionInteractionAffectionGain);
            ARBookCompanionBattleRoster.AddMood(
                captureId,
                ARBookCompanionBattleRoster.InteractionMoodGain);
        }

        BuildCompanionGrid();
        RefreshCompanionDetail();
    }

    private static CompanionDefinition CreateCompanion(string captureId, string displayName)
    {
        return new CompanionDefinition
        {
            captureId = captureId,
            displayName = displayName,
            imageTargetName = ResolveDefaultImageTargetName(captureId)
        };
    }

    private static string ResolveDefaultImageTargetName(string captureId)
    {
        if (string.IsNullOrWhiteSpace(captureId))
        {
            return captureId;
        }

        if (string.Equals(captureId, "ElectrodeHisuian", StringComparison.OrdinalIgnoreCase))
        {
            return "electrode";
        }

        if (string.Equals(captureId, "Talonflame", StringComparison.OrdinalIgnoreCase))
        {
            return "GalarianZapdos";
        }

        if (string.Equals(captureId, "Mismagius", StringComparison.OrdinalIgnoreCase))
        {
            return "mismagius";
        }

        if (string.Equals(captureId, "Toxtricity", StringComparison.OrdinalIgnoreCase))
        {
            return "toxtricity";
        }

        if (string.Equals(captureId, "Scizor", StringComparison.OrdinalIgnoreCase))
        {
            return "scizor";
        }

        if (string.Equals(captureId, "Zorua", StringComparison.OrdinalIgnoreCase))
        {
            return "zorua";
        }

        if (string.Equals(captureId, "Zekrom", StringComparison.OrdinalIgnoreCase))
        {
            return "zekrom";
        }

        if (string.Equals(captureId, "Dragapult", StringComparison.OrdinalIgnoreCase))
        {
            return "dragapult";
        }

        if (string.Equals(captureId, "Celebi", StringComparison.OrdinalIgnoreCase))
        {
            return "celebi";
        }

        if (string.Equals(captureId, "Mew", StringComparison.OrdinalIgnoreCase))
        {
            return "mew";
        }

        if (string.Equals(captureId, "Manaphy", StringComparison.OrdinalIgnoreCase))
        {
            return "manaphy";
        }

        return captureId;
    }

    private void ResolveReferences()
    {
        if (collectionManager == null)
        {
            collectionManager = FindObjectOfType<ARBookCollectionManager>();
        }

        if (chapterProgress == null)
        {
            chapterProgress = FindObjectOfType<ARBookChapterProgress>();
        }

        if (progressResetter == null)
        {
            progressResetter = FindObjectOfType<ARBookDebugProgressResetter>(true);
        }

        if (chapterHudController == null)
        {
            chapterHudController = FindObjectOfType<ARBookChapterHUDController>(true);
        }

        if (companionPlacementRoot == null && Camera.main != null)
        {
            companionPlacementRoot = Camera.main.transform;
        }
    }

    private void EnsureCatalog()
    {
        if (companions == null || companions.Length == 0)
        {
            ResetCatalogToDefault();
        }
    }

    private void EnsureCanvas()
    {
        if (rootCanvas == null)
        {
            GameObject canvasObject = new GameObject("ARBookGameShellCanvas");
            canvasObject.transform.SetParent(transform, false);
            rootCanvas = canvasObject.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 80;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            runtimeEventSystem = new GameObject("ARBookGameShellEventSystem");
            runtimeEventSystem.transform.SetParent(transform, false);
            runtimeEventSystem.AddComponent<EventSystem>();
            runtimeEventSystem.AddComponent<StandaloneInputModule>();
        }

        Transform existingRoot =
            rootCanvas.transform.Find("ARBookGameShellGeneratedRoot");
        if (existingRoot != null)
        {
            generatedRoot = existingRoot.GetComponent<RectTransform>();
        }

        bool createdGeneratedRoot = false;
        if (generatedRoot == null)
        {
            generatedRoot = CreateRect(
                "ARBookGameShellGeneratedRoot",
                rootCanvas.transform);
            createdGeneratedRoot = true;
        }

        if (createdGeneratedRoot)
        {
            Stretch(generatedRoot, 0f, 0f, 0f, 0f);
            AddEditableBackground(generatedRoot);
        }
    }

    public void RebuildSceneInterface()
    {
        EnsureCanvas();
        BuildInterface();
        BindSceneInterface();
        WireSceneButtons();
    }

    public void EnsureIntegratedOverlayInterface()
    {
        EnsureCanvas();
        BindSceneInterface();

        if (dialogueRoot == null)
        {
            dialogueRoot = BuildDialogueOverlay();
        }

        if (battleRoot == null)
        {
            battleRoot = BuildBattleOverlay();
        }

        BindSceneInterface();
        WireSceneButtons();
    }

    private void BuildInterface()
    {
        ClearChildren(generatedRoot);
        homeRoot = BuildHome();
        hudRoot = BuildHud();
        companionRoot = BuildCompanionOverlay();
        backpackRoot = BuildBackpackOverlay();
        dialogueRoot = BuildDialogueOverlay();
        battleRoot = BuildBattleOverlay();
    }

    public void BindSceneInterface()
    {
        if (generatedRoot == null && rootCanvas != null)
        {
            Transform root = rootCanvas.transform.Find("ARBookGameShellGeneratedRoot");
            generatedRoot = root != null ? root.GetComponent<RectTransform>() : null;
        }

        if (generatedRoot == null)
        {
            return;
        }

        homeRoot = FindRect(generatedRoot, "Home") ?? homeRoot;
        hudRoot = FindRect(generatedRoot, "HUD") ?? hudRoot;
        companionRoot = FindRect(generatedRoot, "CompanionMode") ?? companionRoot;
        backpackRoot = FindRect(generatedRoot, "Backpack") ?? backpackRoot;
        dialogueRoot = FindRect(generatedRoot, "DialoguePanel", "DialogueCanvas", "DialogueBox") ?? dialogueRoot;
        battleRoot = FindRect(generatedRoot, "BattlePanel", "BattleCanvas", "BattleControls") ?? battleRoot;
        companionGrid = FindRect(companionRoot, "CompanionGrid") ?? companionGrid;
        startButton = FindButton(homeRoot, "StartButton") ?? startButton;
        restartButton = FindButton(homeRoot, "RestartButton") ?? restartButton;
        homeCompanionButton = FindButton(homeRoot, "CompanionButton", "HomeCompanionButton") ?? homeCompanionButton;
        backpackButton = FindButton(hudRoot, "BackpackButton", "BagButton") ?? backpackButton;
        hudCompanionButton = FindButton(hudRoot, "CompanionButton", "HUDCompanionButton") ?? hudCompanionButton;
        homeButton = FindButton(hudRoot, "HomeButton") ?? homeButton;
        placeButton = FindButton(companionRoot, "PlaceButton") ?? placeButton;
        affectionButton = FindButton(companionRoot, "AffectionButton", "InteractButton") ?? affectionButton;
        affectionButtonText = affectionButton != null
            ? affectionButton.GetComponentInChildren<TMP_Text>(true)
            : affectionButtonText;
        clearCompanionsButton = FindButton(companionRoot, "ClearButton", "ClearCompanionsButton") ?? clearCompanionsButton;
        closeCompanionButton = FindButton(companionRoot, "CloseButton", "CloseCompanionButton") ?? closeCompanionButton;
        closeBackpackButton = FindButton(backpackRoot, "CloseButton", "CloseBackpackButton") ?? closeBackpackButton;
        dialogueContinueButton = FindButton(dialogueRoot, "ContinueButton", "NextButton") ?? dialogueContinueButton;
        battleAttackButton = FindButton(battleRoot, "AttackButton") ?? battleAttackButton;
        battleExitButton = FindButton(battleRoot, "ExitButton", "CloseButton") ?? battleExitButton;
        companionCameraInteractButton =
            FindButton(hudRoot, "CompanionInteractButton", "CompanionCameraInteractButton") ??
            companionCameraInteractButton;
        companionReturnGameButton =
            FindButton(hudRoot, "CompanionReturnGameButton") ??
            companionReturnGameButton;
        companionReturnHomeButton =
            FindButton(hudRoot, "CompanionReturnHomeButton") ??
            FindButton(companionRoot, "CompanionReturnHomeButton") ??
            FindButton(generatedRoot, "CompanionReturnHomeButton") ??
            companionReturnHomeButton;
        startButtonText = startButton != null
            ? startButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        questText = FindText(hudRoot, "QuestText") ?? questText;
        progressText = FindText(hudRoot, "ProgressText") ?? progressText;
        capturedCountText = FindText(backpackRoot, "BackpackText") ?? capturedCountText;
        companionDetailText = FindText(companionRoot, "DetailText") ?? companionDetailText;
        companionCameraStatusText =
            FindText(hudRoot, "CompanionCameraStatusText", "CompanionMoodText") ??
            companionCameraStatusText;
        dialogueSpeakerText = FindText(dialogueRoot, "SpeakerNameText", "SpeakerName") ?? dialogueSpeakerText;
        dialogueBodyText = FindText(dialogueRoot, "DialogueText") ?? dialogueBodyText;
        battleMessageText = FindText(battleRoot, "BattleMessageText", "MessageText") ?? battleMessageText;
        battleLeftHPText = FindText(battleRoot, "LeftHPText", "EnemyHPText") ?? battleLeftHPText;
        battleRightHPText = FindText(battleRoot, "RightHPText", "PlayerHPText") ?? battleRightHPText;
        hpFill = FindComponentInNamedChild<UIImage>(hudRoot, "HPFill") ?? hpFill;
        companionMoodFill =
            FindComponentInNamedChild<UIImage>(hudRoot, "CompanionMoodFill") ??
            companionMoodFill;
        dialogueLeftHighlight = FindComponentInNamedChild<UIImage>(
            dialogueRoot,
            "LeftSpeakerHighlight") ?? dialogueLeftHighlight;
        dialogueRightHighlight = FindComponentInNamedChild<UIImage>(
            dialogueRoot,
            "RightSpeakerHighlight") ?? dialogueRightHighlight;
        battleLeftHPSlider = FindComponentInNamedChild<Slider>(
            battleRoot,
            "LeftHPSlider",
            "EnemyHPSlider") ?? battleLeftHPSlider;
        battleRightHPSlider = FindComponentInNamedChild<Slider>(
            battleRoot,
            "RightHPSlider",
            "PlayerHPSlider") ?? battleRightHPSlider;
    }

    private bool IsSceneInterfaceMissing()
    {
        return generatedRoot == null ||
               homeRoot == null ||
               hudRoot == null ||
               companionRoot == null ||
               backpackRoot == null ||
               questText == null ||
               progressText == null;
    }

    private void WireSceneButtons()
    {
        WireButton(startButton, BeginGame);
        WireButton(restartButton, RestartGame);
        WireButton(homeCompanionButton, OpenCapturedSinglePokemonMode);
        WireButton(backpackButton, OpenBackpack);
        WireButton(hudCompanionButton, OpenCapturedSinglePokemonMode);
        WireButton(homeButton, ShowHome);
        WireButton(placeButton, ToggleSelectedPartyMember);
        WireButton(affectionButton, AddAffectionToSelected);
        WireButton(companionCameraInteractButton, AddAffectionToSelected);
        WireButton(companionReturnGameButton, ReturnFromCompanionToGame);
        WireButton(companionReturnHomeButton, ShowHome);
        WireButton(clearCompanionsButton, CloseCompanionMode);
        WireButton(closeCompanionButton, CloseCompanionMode);
        WireButton(closeBackpackButton, CloseBackpack);
    }

    private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        if (HasPersistentClick(button, action))
        {
            return;
        }

        button.onClick.AddListener(action);
    }

    private static bool HasPersistentClick(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
        {
            return false;
        }

        object target = action.Target;
        string methodName = action.Method != null ? action.Method.Name : string.Empty;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == target &&
                button.onClick.GetPersistentMethodName(i) == methodName)
            {
                return true;
            }
        }

        return false;
    }

    private RectTransform BuildHome()
    {
        RectTransform root = CreateFullRoot("Home");
        AddEditableBackground(root);

        RectTransform panel = CreatePanel(
            "HomePanel",
            root,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(760f, 620f));

        TMP_Text title = CreateText(
            "Title",
            panel,
            "AR Book Adventure",
            48,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        SetAnchors(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        title.rectTransform.sizeDelta = new Vector2(690f, 110f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -76f);

        TMP_Text subtitle = CreateText(
            "Subtitle",
            panel,
            "打开相机，翻动实体书页，在不同地图中收服精灵。",
            24,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetAnchors(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        subtitle.rectTransform.sizeDelta = new Vector2(690f, 74f);
        subtitle.rectTransform.anchoredPosition = new Vector2(0f, -176f);

        Button start = CreateButton("StartButton", panel, "开始游戏", 32);
        startButtonText = start.GetComponentInChildren<TMP_Text>();
        SetButtonRect(start, new Vector2(0f, -275f), new Vector2(560f, 76f));

        Button restart = CreateButton("RestartButton", panel, "重新开始", 28);
        SetButtonRect(restart, new Vector2(0f, -365f), new Vector2(560f, 70f));

        Button companion = CreateButton("CompanionButton", panel, "陪伴模式", 28);
        SetButtonRect(companion, new Vector2(0f, -445f), new Vector2(560f, 70f));

        TMP_Text footer = CreateText(
            "Footer",
            panel,
            "保持书页完整入镜。识别地图后可以移动、互动、战斗和收服。",
            21,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetAnchors(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        footer.rectTransform.sizeDelta = new Vector2(690f, 88f);
        footer.rectTransform.anchoredPosition = new Vector2(0f, 64f);
        return root;
    }

#if false
    private RectTransform BuildHomeDisabled()
    {
        RectTransform root = CreateFullRoot("Home");
        UIImage shade = root.gameObject.AddComponent<UIImage>();
        shade.raycastTarget = false;

        RectTransform panel = CreatePanel(
            "HomePanel",
            root,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(760f, 620f));
        TMP_Text title = CreateText(
            "Title",
            panel,
            "璁板繂鍥鹃壌 AR 鍐掗櫓",
            48,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        SetAnchors(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        title.rectTransform.sizeDelta = new Vector2(690f, 110f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -76f);

        TMP_Text subtitle = CreateText(
            "Subtitle",
            panel,
            "鎵撳紑鐩告満锛岀炕鍔ㄥ疄浣撲功椤碉紝鍦ㄤ笉鍚屽湴鍥句腑鏀舵湇绮剧伒銆?,
            24,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetAnchors(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        subtitle.rectTransform.sizeDelta = new Vector2(690f, 74f);
        subtitle.rectTransform.anchoredPosition = new Vector2(0f, -176f);

        Button startButton = CreateButton("StartButton", panel, "寮€濮嬫父鎴?, 32);
        startButtonText = startButton.GetComponentInChildren<TMP_Text>();
        SetButtonRect(startButton, new Vector2(0f, -275f), new Vector2(560f, 76f));

        Button restartButton = CreateButton("RestartButton", panel, "娓呯┖瀛樻。 / 閲嶆柊寮€濮?, 28);
        SetButtonRect(restartButton, new Vector2(0f, -365f), new Vector2(560f, 70f));

            "陪伴模式",
        SetButtonRect(companionButton, new Vector2(0f, -445f), new Vector2(560f, 70f));

        TMP_Text footer = CreateText(
            "Footer",
            panel,
            "杩涘叆娓告垙鍚庝繚鎸佹憚鍍忓ご瀵瑰噯涔﹂〉锛岃瘑鍒换鎰忓湴鍥惧浘鍍忓悗鍗冲彲绉诲姩銆佷簰鍔ㄥ拰鏀舵湇銆?,
            21,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetAnchors(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        footer.rectTransform.sizeDelta = new Vector2(690f, 88f);
        footer.rectTransform.anchoredPosition = new Vector2(0f, 64f);
        return root;
    }

#endif

    private RectTransform BuildHud()
    {
        RectTransform root = CreateFullRoot("HUD");
        AddEditableBackground(root);

        RectTransform status = CreatePanel(
            "PlayerStatus",
            root,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(460f, 132f));
        status.anchoredPosition = new Vector2(28f, -28f);

        UIImage avatar = CreateImage("Avatar", status, playerAvatarSprite);
        RectTransform avatarRect = avatar.rectTransform;
        SetAnchors(avatarRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        avatarRect.sizeDelta = new Vector2(88f, 88f);
        avatarRect.anchoredPosition = new Vector2(62f, 0f);

        TMP_Text nameText = CreateText(
            "PlayerName",
            status,
            playerName,
            28,
            FontStyles.Bold,
            TextAlignmentOptions.Left);
        SetAnchors(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        nameText.rectTransform.offsetMin = new Vector2(122f, -56f);
        nameText.rectTransform.offsetMax = new Vector2(-26f, -18f);

        RectTransform hpBack = CreateRect("HPBack", status);
        SetAnchors(hpBack, new Vector2(0f, 0f), new Vector2(1f, 0f));
        hpBack.offsetMin = new Vector2(122f, 28f);
        hpBack.offsetMax = new Vector2(-26f, 58f);
        hpBack.gameObject.AddComponent<UIImage>().raycastTarget = false;

        hpFill = CreateImage("HPFill", hpBack, null);
        SetAnchors(hpFill.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f));
        hpFill.rectTransform.offsetMin = new Vector2(4f, 4f);
        hpFill.rectTransform.offsetMax = new Vector2(-4f, -4f);

        RectTransform taskPanel = CreatePanel(
            "TaskPanel",
            root,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(520f, 270f));
        taskPanel.anchoredPosition = new Vector2(-28f, -28f);

        questText = CreateText(
            "QuestText",
            taskPanel,
            "Waiting for map page",
            25,
            FontStyles.Bold,
            TextAlignmentOptions.TopLeft);
        SetAnchors(questText.rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 1f));
        questText.rectTransform.offsetMin = new Vector2(28f, 10f);
        questText.rectTransform.offsetMax = new Vector2(-28f, -22f);

        progressText = CreateText(
            "ProgressText",
            taskPanel,
            string.Empty,
            20,
            FontStyles.Normal,
            TextAlignmentOptions.BottomLeft);
        SetAnchors(progressText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.36f));
        progressText.rectTransform.offsetMin = new Vector2(28f, 18f);
        progressText.rectTransform.offsetMax = new Vector2(-28f, -8f);

        RectTransform buttons = CreateRect("HUDButtons", root);
        SetAnchors(buttons, new Vector2(1f, 0f), new Vector2(1f, 0f));
        buttons.sizeDelta = new Vector2(620f, 88f);
        buttons.anchoredPosition = new Vector2(-28f, 34f);
        AddEditableBackground(buttons);
        Button backpackButton = CreateButton("BackpackButton", buttons, "背包", 24);
        Button companionButton = CreateButton("CompanionButton", buttons, "陪伴", 24);
        Button homeButton = CreateButton("HomeButton", buttons, "首页", 24);

        companionCameraStatusText = CreateText(
            "CompanionCameraStatusText",
            root,
            string.Empty,
            18,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetAnchors(companionCameraStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        companionCameraStatusText.rectTransform.sizeDelta = new Vector2(520f, 46f);
        companionCameraStatusText.rectTransform.anchoredPosition = new Vector2(0f, -52f);

        RectTransform moodBack = CreateRect("CompanionMoodBack", root);
        SetAnchors(moodBack, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        moodBack.sizeDelta = new Vector2(520f, 24f);
        moodBack.anchoredPosition = new Vector2(0f, -88f);
        moodBack.gameObject.AddComponent<UIImage>().raycastTarget = false;
        companionMoodFill = CreateImage("CompanionMoodFill", moodBack, null);
        Stretch(companionMoodFill.rectTransform, 4f, 4f, 4f, 4f);

        companionReturnGameButton = CreateButton("CompanionReturnGameButton", root, "返回游戏", 22);
        SetButtonRect(companionReturnGameButton, new Vector2(-250f, 58f), new Vector2(190f, 68f));
        companionCameraInteractButton = CreateButton("CompanionInteractButton", root, "互动", 24);
        SetButtonRect(companionCameraInteractButton, new Vector2(0f, 58f), new Vector2(220f, 76f));
        companionReturnHomeButton = CreateButton("CompanionReturnHomeButton", root, "返回首页", 22);
        SetBottomRightButtonRect(companionReturnHomeButton, new Vector2(190f, 68f), new Vector2(34f, 34f));

        RectTransform hint = CreatePanel(
            "CameraHint",
            root,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(760f, 56f));
        hint.anchoredPosition = new Vector2(0f, 30f);
        TMP_Text hintText = CreateText(
            "HintText",
            hint,
            "Keep the page visible, then tap the walkable ground to move.",
            20,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        Stretch(hintText.rectTransform, 18f, 6f, 18f, 6f);
        return root;
    }

#if false
    private RectTransform BuildHudDisabled()
    {
        RectTransform root = CreateFullRoot("HUD");
        UIImage background = root.gameObject.AddComponent<UIImage>();
        background.raycastTarget = false;

        RectTransform status = CreatePanel(
            "PlayerStatus",
            root,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(460f, 132f));
        status.anchoredPosition = new Vector2(28f, -28f);

        UIImage avatar = CreateImage("Avatar", status, playerAvatarSprite);
        RectTransform avatarRect = avatar.rectTransform;
        SetAnchors(avatarRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        avatarRect.sizeDelta = new Vector2(88f, 88f);
        avatarRect.anchoredPosition = new Vector2(62f, 0f);

        TMP_Text nameText = CreateText(
            "PlayerName",
            status,
            playerName,
            28,
            FontStyles.Bold,
            TextAlignmentOptions.Left);
        SetAnchors(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        nameText.rectTransform.offsetMin = new Vector2(122f, -56f);
        nameText.rectTransform.offsetMax = new Vector2(-26f, -18f);

        RectTransform hpBack = CreateRect("HPBack", status);
        SetAnchors(hpBack, new Vector2(0f, 0f), new Vector2(1f, 0f));
        hpBack.offsetMin = new Vector2(122f, 28f);
        hpBack.offsetMax = new Vector2(-26f, 58f);
        UIImage hpBackImage = hpBack.gameObject.AddComponent<UIImage>();

        hpFill = CreateImage("HPFill", hpBack, null);
        SetAnchors(hpFill.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f));
        hpFill.rectTransform.offsetMin = new Vector2(4f, 4f);
        hpFill.rectTransform.offsetMax = new Vector2(-4f, -4f);

        RectTransform taskPanel = CreatePanel(
            "TaskPanel",
            root,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(520f, 270f));
        taskPanel.anchoredPosition = new Vector2(-28f, -28f);

        questText = CreateText(
            "QuestText",
            taskPanel,
            "等待识别地图书页",
            25,
            FontStyles.Bold,
            TextAlignmentOptions.TopLeft);
        SetAnchors(questText.rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 1f));
        questText.rectTransform.offsetMin = new Vector2(28f, 10f);
        questText.rectTransform.offsetMax = new Vector2(-28f, -22f);

        progressText = CreateText(
            "ProgressText",
            taskPanel,
            string.Empty,
            20,
            FontStyles.Normal,
            TextAlignmentOptions.BottomLeft);
        SetAnchors(progressText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.36f));
        progressText.rectTransform.offsetMin = new Vector2(28f, 18f);
        progressText.rectTransform.offsetMax = new Vector2(-28f, -8f);

        RectTransform buttons = CreateRect("HUDButtons", root);
        SetAnchors(buttons, new Vector2(1f, 0f), new Vector2(1f, 0f));
        buttons.sizeDelta = new Vector2(620f, 88f);
        buttons.anchoredPosition = new Vector2(-28f, 34f);
        AddEditableBackground(buttons);
        Button backpackButton = CreateButton("BackpackButton", buttons, "背包", 24);
        Button companionButton = CreateButton("CompanionButton", buttons, "陪伴", 24);
        Button homeButton = CreateButton("HomeButton", buttons, "首页", 24);

        SetButtonRect(backpackButton, new Vector2(-210f, 0f), new Vector2(190f, 68f));
        SetButtonRect(companionButton, new Vector2(0f, 0f), new Vector2(190f, 68f));
        SetButtonRect(homeButton, new Vector2(210f, 0f), new Vector2(190f, 68f));

        RectTransform hint = CreatePanel(
            "CameraHint",
            root,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(760f, 56f));
        hint.anchoredPosition = new Vector2(0f, 30f);
        TMP_Text hintText = CreateText(
            "HintText",
            hint,
            "调用相机翻书：保持书页完整入镜，识别地图后点击地面移动角色。",
            20,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        Stretch(hintText.rectTransform, 18f, 6f, 18f, 6f);

        return root;
    }

#endif

    private RectTransform BuildCompanionOverlay()
    {
        RectTransform root = CreateFullRoot("CompanionMode");
        UIImage shade = root.gameObject.AddComponent<UIImage>();
        shade.raycastTarget = false;

        RectTransform panel = CreatePanel(
            "CompanionPanel",
            root,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1360f, 780f));

        TMP_Text title = CreateText(
            "Title",
            panel,
            "宝可梦",
            38,
            FontStyles.Bold,
            TextAlignmentOptions.Left);
        SetAnchors(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        title.rectTransform.offsetMin = new Vector2(38f, -88f);
        title.rectTransform.offsetMax = new Vector2(-38f, -22f);

        companionGrid = CreateRect("CompanionGrid", panel);
        SetAnchors(companionGrid, new Vector2(0f, 0f), new Vector2(0.68f, 0.86f));
        companionGrid.offsetMin = new Vector2(38f, 116f);
        companionGrid.offsetMax = new Vector2(-18f, -92f);
        AddEditableBackground(companionGrid);

        RectTransform detail = CreatePanel(
            "CompanionDetail",
            panel,
            new Vector2(0.70f, 0.19f),
            new Vector2(1f, 0.86f),
            Vector2.zero);
        detail.offsetMin = new Vector2(8f, 116f);
        detail.offsetMax = new Vector2(-38f, -92f);

        companionDetailText = CreateText(
            "DetailText",
            detail,
            string.Empty,
            24,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft);
        SetAnchors(companionDetailText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        companionDetailText.rectTransform.sizeDelta = new Vector2(340f, 440f);
        companionDetailText.rectTransform.anchoredPosition = Vector2.zero;

        RectTransform actions = CreateRect("Actions", panel);
        SetAnchors(actions, new Vector2(0f, 0f), new Vector2(1f, 0f));
        actions.offsetMin = new Vector2(38f, 30f);
        actions.offsetMax = new Vector2(-38f, 98f);
        AddEditableBackground(actions);
        Button placeButton = CreateButton("PlaceButton", actions, "携带", 24);
        Button affectionButton = CreateButton("AffectionButton", actions, "陪伴", 24);
        Button clearButton = CreateButton("ClearButton", actions, "收回全部", 24);
        Button closeButton = CreateButton("CloseButton", actions, "返回", 24);

        return root;
    }

    private RectTransform BuildBackpackOverlay()
    {
        RectTransform root = CreateFullRoot("Backpack");
        UIImage shade = root.gameObject.AddComponent<UIImage>();
        shade.raycastTarget = false;

        RectTransform panel = CreatePanel(
            "BackpackPanel",
            root,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(760f, 520f));
        TMP_Text title = CreateText(
            "Title",
            panel,
            "背包",
            36,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        SetAnchors(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        title.rectTransform.sizeDelta = new Vector2(690f, 64f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -62f);

        capturedCountText = CreateText(
            "BackpackText",
            panel,
            string.Empty,
            24,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft);
        SetAnchors(capturedCountText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        capturedCountText.rectTransform.sizeDelta = new Vector2(690f, 300f);
        capturedCountText.rectTransform.anchoredPosition = new Vector2(0f, 10f);

        Button closeButton = CreateButton("CloseButton", panel, "关闭", 24);
        SetButtonRect(closeButton, new Vector2(0f, -190f), new Vector2(420f, 70f));

        return root;
    }

    private RectTransform BuildDialogueOverlay()
    {
        RectTransform root = CreateFullRoot("DialoguePanel");
        AddEditableBackground(root);

        RectTransform panel = CreatePanel(
            "DialogueBox",
            root,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            Vector2.zero);
        panel.offsetMin = new Vector2(80f, 42f);
        panel.offsetMax = new Vector2(-80f, 292f);

        dialogueLeftHighlight = CreateImage("LeftSpeakerHighlight", panel, null);
        SetAnchors(dialogueLeftHighlight.rectTransform, new Vector2(0f, 0f), new Vector2(0.08f, 1f));
        dialogueLeftHighlight.rectTransform.offsetMin = new Vector2(18f, 18f);
        dialogueLeftHighlight.rectTransform.offsetMax = new Vector2(-10f, -18f);

        dialogueRightHighlight = CreateImage("RightSpeakerHighlight", panel, null);
        SetAnchors(dialogueRightHighlight.rectTransform, new Vector2(0.92f, 0f), new Vector2(1f, 1f));
        dialogueRightHighlight.rectTransform.offsetMin = new Vector2(10f, 18f);
        dialogueRightHighlight.rectTransform.offsetMax = new Vector2(-18f, -18f);

        dialogueSpeakerText = CreateText(
            "SpeakerNameText",
            panel,
            "Speaker",
            28,
            FontStyles.Bold,
            TextAlignmentOptions.Left);
        SetAnchors(dialogueSpeakerText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        dialogueSpeakerText.rectTransform.offsetMin = new Vector2(120f, -72f);
        dialogueSpeakerText.rectTransform.offsetMax = new Vector2(-220f, -18f);

        dialogueBodyText = CreateText(
            "DialogueText",
            panel,
            "Dialogue text",
            24,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft);
        SetAnchors(dialogueBodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f));
        dialogueBodyText.rectTransform.offsetMin = new Vector2(120f, 34f);
        dialogueBodyText.rectTransform.offsetMax = new Vector2(-220f, -84f);

        dialogueContinueButton = CreateButton("ContinueButton", panel, "继续", 23);
        SetButtonRect(dialogueContinueButton, new Vector2(0f, 0f), new Vector2(180f, 62f));
        RectTransform buttonRect = dialogueContinueButton.GetComponent<RectTransform>();
        SetAnchors(buttonRect, new Vector2(1f, 0f), new Vector2(1f, 0f));
        buttonRect.anchoredPosition = new Vector2(-122f, 64f);

        root.gameObject.SetActive(false);
        return root;
    }

    private RectTransform BuildBattleOverlay()
    {
        RectTransform root = CreateFullRoot("BattlePanel");
        AddEditableBackground(root);

        RectTransform leftStatus = CreateBattleStatusPanel(
            "EnemyStatus",
            root,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(370f, -110f),
            "Enemy",
            out battleLeftHPSlider,
            out battleLeftHPText);

        RectTransform rightStatus = CreateBattleStatusPanel(
            "PlayerStatusBattle",
            root,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-370f, -110f),
            "Player",
            out battleRightHPSlider,
            out battleRightHPText);

        RectTransform controls = CreatePanel(
            "BattleControls",
            root,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            Vector2.zero);
        controls.offsetMin = new Vector2(72f, 32f);
        controls.offsetMax = new Vector2(-72f, 178f);

        battleMessageText = CreateText(
            "BattleMessageText",
            controls,
            "选择行动",
            26,
            FontStyles.Bold,
            TextAlignmentOptions.Left);
        SetAnchors(battleMessageText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f));
        battleMessageText.rectTransform.offsetMin = new Vector2(40f, 24f);
        battleMessageText.rectTransform.offsetMax = new Vector2(-560f, -24f);

        battleAttackButton = CreateButton("AttackButton", controls, "攻击", 32);
        SetButtonRect(battleAttackButton, new Vector2(0f, 0f), new Vector2(310f, 92f));
        RectTransform attackRect = battleAttackButton.GetComponent<RectTransform>();
        SetAnchors(attackRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        attackRect.anchoredPosition = new Vector2(260f, 0f);

        Button companionAButton = CreateButton("CompanionAButton", controls, "A 宝可梦攻击", 22);
        RectTransform companionARect = companionAButton.GetComponent<RectTransform>();
        SetAnchors(companionARect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        companionARect.sizeDelta = new Vector2(260f, 64f);
        companionARect.anchoredPosition = new Vector2(-100f, 38f);

        Button companionBButton = CreateButton("CompanionBButton", controls, "B 宝可梦攻击", 22);
        RectTransform companionBRect = companionBButton.GetComponent<RectTransform>();
        SetAnchors(companionBRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        companionBRect.sizeDelta = new Vector2(260f, 64f);
        companionBRect.anchoredPosition = new Vector2(-100f, -38f);

        battleExitButton = CreateButton("ExitButton", controls, "退出", 24);
        SetButtonRect(battleExitButton, new Vector2(0f, 0f), new Vector2(180f, 66f));
        RectTransform exitRect = battleExitButton.GetComponent<RectTransform>();
        SetAnchors(exitRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        exitRect.anchoredPosition = new Vector2(-120f, 0f);

        leftStatus.gameObject.SetActive(true);
        rightStatus.gameObject.SetActive(true);
        root.gameObject.SetActive(false);
        return root;
    }

    private RectTransform CreateBattleStatusPanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        string title,
        out Slider slider,
        out TMP_Text hpText)
    {
        RectTransform panel = CreatePanel(
            name,
            parent,
            anchorMin,
            anchorMax,
            new Vector2(560f, 150f));
        panel.anchoredPosition = position;

        TMP_Text nameText = CreateText(
            "NameText",
            panel,
            title,
            28,
            FontStyles.Bold,
            TextAlignmentOptions.Left);
        SetAnchors(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        nameText.rectTransform.offsetMin = new Vector2(30f, -62f);
        nameText.rectTransform.offsetMax = new Vector2(-30f, -18f);

        slider = CreateEditableSlider(name.Contains("Enemy") ? "LeftHPSlider" : "RightHPSlider", panel);
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        SetAnchors(sliderRect, new Vector2(0f, 0f), new Vector2(1f, 0f));
        sliderRect.offsetMin = new Vector2(30f, 48f);
        sliderRect.offsetMax = new Vector2(-30f, 82f);

        hpText = CreateText(
            name.Contains("Enemy") ? "LeftHPText" : "RightHPText",
            panel,
            "HP",
            24,
            FontStyles.Bold,
            TextAlignmentOptions.Right);
        SetAnchors(hpText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f));
        hpText.rectTransform.offsetMin = new Vector2(30f, 10f);
        hpText.rectTransform.offsetMax = new Vector2(-30f, 44f);

        return panel;
    }

    private void RefreshAll()
    {
        RefreshHome();
        RefreshHud();
        RefreshBackpack();
    }

    private void RefreshHome()
    {
        if (startButtonText == null)
        {
            return;
        }

        bool hasStarted = PlayerPrefs.GetInt(StartedKey, 0) == 1 ||
                          GetCapturedIds().Count > 0 ||
                          HasAnyCompletedChapter();
        startButtonText.text = hasStarted ? "继续游戏" : "开始游戏";
    }

#if false
    private void RefreshHomeDisabled()
    {
        if (startButtonText == null)
        {
            return;
        }

        bool hasStarted = PlayerPrefs.GetInt(StartedKey, 0) == 1 ||
                          GetCapturedIds().Count > 0 ||
                          HasAnyCompletedChapter();
        startButtonText.text = hasStarted ? "继续游戏" : "开始游戏";
    }

#endif

    private void RefreshHud()
    {
        ResolveReferences();

        if (hudRoot == null || !hudRoot.gameObject.activeSelf)
        {
            return;
        }

        if (hpFill != null)
        {
            float value = maxHP <= 0 ? 1f : Mathf.Clamp01((float)currentHP / maxHP);
            hpFill.rectTransform.anchorMax = new Vector2(value, 1f);
        }

        string currentQuestText = GetCurrentQuestText();
        HideLegacyHud();

        if (questText != null)
        {
            questText.text = currentQuestText;
        }

        if (progressText != null)
        {
            progressText.text = BuildProgressText();
        }

        SetNamedChildActive(hudRoot, "CompanionButton", false);
        SetNamedChildActive(hudRoot, "HUDCompanionButton", false);

        if (companionCameraModeActive)
        {
            RefreshCompanionCameraHud();
            return;
        }

        if (singlePokemonCameraModeActive)
        {
            ApplySinglePokemonCameraHud(true);
            return;
        }

        RefreshCompanionCameraHud();
    }

    private void ApplyCompanionCameraHud(bool active)
    {
        companionCameraModeActive = active;
        if (active)
        {
            singlePokemonCameraModeActive = false;
        }

        if (hudRoot == null)
        {
            return;
        }

        SetNamedChildActive(hudRoot, "PlayerStatus", !active);
        SetNamedChildActive(hudRoot, "TaskPanel", !active);
        SetNamedChildActive(hudRoot, "CameraHint", !active);
        SetNamedChildActive(hudRoot, "HUDButtons", true);
        SetNamedChildActive(hudRoot, "BackpackButton", !active);
        SetNamedChildActive(hudRoot, "CompanionButton", false);
        SetNamedChildActive(hudRoot, "HUDCompanionButton", false);
        SetNamedChildActive(hudRoot, "CompanionMoodBack", active);
        SetNamedChildActive(hudRoot, "CompanionMoodFill", active);

        if (companionCameraStatusText != null)
        {
            SetActiveIfChanged(companionCameraStatusText.gameObject, active);
        }

        if (companionCameraInteractButton != null)
        {
            SetActiveIfChanged(companionCameraInteractButton.gameObject, active);
        }

        if (companionReturnGameButton != null)
        {
            SetActiveIfChanged(companionReturnGameButton.gameObject, active);
        }

        if (companionReturnHomeButton != null)
        {
            SetActiveIfChanged(companionReturnHomeButton.gameObject, active);
        }

        RefreshCompanionCameraHud();
    }

    private void ApplySinglePokemonCameraHud(bool active)
    {
        singlePokemonCameraModeActive = active;
        if (active)
        {
            companionCameraModeActive = false;
        }

        if (hudRoot == null)
        {
            return;
        }

        if (!active)
        {
            if (companionReturnHomeButton != null)
            {
                SetActiveIfChanged(companionReturnHomeButton.gameObject, false);
            }

            return;
        }

        SetNamedChildActive(hudRoot, "PlayerStatus", false);
        SetNamedChildActive(hudRoot, "TaskPanel", false);
        SetNamedChildActive(hudRoot, "CameraHint", false);
        SetNamedChildActive(hudRoot, "HUDButtons", true);
        SetNamedChildActive(hudRoot, "BackpackButton", false);
        SetNamedChildActive(hudRoot, "CompanionButton", false);
        SetNamedChildActive(hudRoot, "HUDCompanionButton", false);
        SetNamedChildActive(hudRoot, "HomeButton", false);
        SetNamedChildActive(hudRoot, "CompanionMoodBack", false);
        SetNamedChildActive(hudRoot, "CompanionMoodFill", false);

        if (companionCameraStatusText != null)
        {
            SetActiveIfChanged(companionCameraStatusText.gameObject, false);
        }

        if (companionCameraInteractButton != null)
        {
            SetActiveIfChanged(companionCameraInteractButton.gameObject, false);
        }

        if (companionReturnGameButton != null)
        {
            SetActiveIfChanged(companionReturnGameButton.gameObject, false);
        }

        if (companionReturnHomeButton != null)
        {
            SetActiveIfChanged(companionReturnHomeButton.gameObject, active);
        }
    }

    private void RefreshCompanionCameraHud()
    {
        bool active = companionCameraModeActive &&
            !string.IsNullOrWhiteSpace(activeCompanionId) &&
            hudRoot != null &&
            hudRoot.gameObject.activeInHierarchy &&
            (companionRoot == null || !companionRoot.gameObject.activeInHierarchy);

        if (companionCameraStatusText != null)
        {
            SetActiveIfChanged(companionCameraStatusText.gameObject, active);
            if (active)
            {
                CompanionDefinition definition = FindCompanion(activeCompanionId);
                string display = definition != null
                    ? definition.displayName
                    : activeCompanionId;
                string targetName = definition != null
                    ? GetCompanionTargetName(definition)
                    : activeCompanionId;
                bool tracked = IsTracked(activeCompanionTarget);
                int mood = ARBookCompanionBattleRoster.GetMood(activeCompanionId);
                companionCameraStatusText.text =
                    tracked
                        ? $"{display}  心情 {mood}  好感 {GetAffection(activeCompanionId)}"
                        : $"{display}\n请扫描 {targetName} 图片";

                if (companionMoodFill != null)
                {
                    companionMoodFill.rectTransform.anchorMax =
                        new Vector2(Mathf.Clamp01(mood / 100f), 1f);
                }
            }
        }

        if (companionCameraInteractButton != null)
        {
            SetActiveIfChanged(companionCameraInteractButton.gameObject, active);
            companionCameraInteractButton.interactable =
                active && GetCompanionInteractionsRemaining() > 0;
        }

        if (companionReturnGameButton != null)
        {
            SetActiveIfChanged(companionReturnGameButton.gameObject, active);
        }

        if (companionReturnHomeButton != null)
        {
            SetActiveIfChanged(companionReturnHomeButton.gameObject, active);
        }
    }

    private static void SetActiveIfChanged(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private static void SetNamedChildActive(
        Transform root,
        string childName,
        bool active)
    {
        Transform child = FindDescendant(root, childName);
        if (child != null)
        {
            SetActiveIfChanged(child.gameObject, active);
        }
    }

    private static void EnsureMainCameraRendering()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        if (!mainCamera.gameObject.activeSelf)
        {
            mainCamera.gameObject.SetActive(true);
        }

        if (!mainCamera.enabled)
        {
            mainCamera.enabled = true;
        }
    }

    private void RefreshBackpack()
    {
        if (capturedCountText == null)
        {
            return;
        }

        List<string> captured = GetCapturedIds();
        int completedChapters = GetCompletedChapterCount();
        string[] party = ARBookCompanionBattleRoster.GetParty();
        string selected = string.IsNullOrWhiteSpace(selectedBackpackPartyId)
            ? "未选择"
            : selectedBackpackPartyId;
        capturedCountText.text =
            $"已收服精灵：{captured.Count}\n" +
            $"已探索地图：{completedChapters} / 5\n" +
            $"携带A：{(string.IsNullOrWhiteSpace(party[0]) ? "空" : party[0])}\n" +
            $"携带B：{(string.IsNullOrWhiteSpace(party[1]) ? "空" : party[1])}\n" +
            $"当前选择：{selected}\n\n" +
            BuildCapturedNameList(captured);

        RebuildBackpackPartyButtons(captured);
    }

    private void RebuildBackpackPartyButtons(List<string> captured)
    {
        if (backpackRoot == null)
        {
            return;
        }

        Transform oldRoot = FindDescendant(backpackRoot, "PartyButtons");
        if (oldRoot != null)
        {
            DestroyRuntimeObject(oldRoot.gameObject, false);
        }

        RectTransform buttonRoot = CreateRect("PartyButtons", backpackRoot);
        SetAnchors(buttonRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        buttonRoot.sizeDelta = new Vector2(690f, 190f);
        buttonRoot.anchoredPosition = new Vector2(0f, -86f);

        int visible = 0;
        for (int i = 0; i < captured.Count; i++)
        {
            string captureId = captured[i];
            CompanionDefinition definition = FindCompanion(captureId);
            string display = definition != null ? definition.displayName : captureId;
            bool selected = string.Equals(
                selectedBackpackPartyId,
                captureId,
                StringComparison.OrdinalIgnoreCase);
            Button button = CreateButton(
                $"Party_{captureId}",
                buttonRoot,
                selected ? $"已选 {display}" : $"选择 {display}",
                18);
            RectTransform rect = button.GetComponent<RectTransform>();
            SetAnchors(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            rect.sizeDelta = new Vector2(210f, 44f);
            rect.anchoredPosition = new Vector2(
                -230f + (visible % 3) * 230f,
                -24f - (visible / 3) * 52f);
            string id = captureId;
            button.onClick.AddListener(() => SelectBackpackPartyMember(id));
            visible++;
        }

        Button confirmButton = CreateButton(
            "ConfirmPartyButton",
            buttonRoot,
            BuildConfirmPartyButtonText(),
            20);
        RectTransform confirmRect = confirmButton.GetComponent<RectTransform>();
        SetAnchors(confirmRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        confirmRect.sizeDelta = new Vector2(280f, 50f);
        confirmRect.anchoredPosition = new Vector2(0f, 20f);
        confirmButton.onClick.AddListener(ConfirmBackpackPartyMember);
    }

    private void SelectBackpackPartyMember(string captureId)
    {
        selectedBackpackPartyId = captureId;
        RefreshBackpack();
    }

    private void ConfirmBackpackPartyMember()
    {
        if (string.IsNullOrWhiteSpace(selectedBackpackPartyId))
        {
            return;
        }

        ARBookCompanionBattleRoster.TogglePartyMember(selectedBackpackPartyId);
        RefreshBackpack();
    }

    private string BuildConfirmPartyButtonText()
    {
        if (string.IsNullOrWhiteSpace(selectedBackpackPartyId))
        {
            return "先选择宝可梦";
        }

        return ARBookCompanionBattleRoster.IsInParty(selectedBackpackPartyId)
            ? "确认取消携带"
            : "确认携带";
    }

    private void BuildCompanionGrid()
    {
        if (companionGrid == null)
        {
            return;
        }

        ClearChildren(companionGrid);
        int visibleCount = 0;
        if (companions == null)
        {
            companions = new CompanionDefinition[0];
        }

        for (int i = 0; i < companions.Length; i++)
        {
            CompanionDefinition definition = companions[i];
            if (definition == null || !IsCaptured(definition.captureId))
            {
                continue;
            }

            CreateCompanionCard(definition, visibleCount);
            visibleCount++;
        }

        if (visibleCount == 0)
        {
            TMP_Text empty = CreateText(
                "Empty",
                companionGrid,
                "\u8fd8\u6ca1\u6709\u5df2\u6536\u670d\u7684\u7cbe\u7075",
                24,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            SetAnchors(empty.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            empty.rectTransform.sizeDelta = new Vector2(520f, 180f);
            empty.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

#if false
    private void BuildCompanionGridDisabled()
    {
        if (companionGrid == null)
        {
            return;
        }

        ClearChildren(companionGrid);
        int visibleCount = 0;
        for (int i = 0; i < companions.Length; i++)
        {
            CompanionDefinition definition = companions[i];
            if (definition == null || !IsCaptured(definition.captureId))
            {
                continue;
            }

            CreateCompanionCard(definition, visibleCount);
            visibleCount++;
        }

        if (visibleCount == 0)
        {
            TMP_Text empty = CreateText(
                "Empty",
                companionGrid,
                "杩樻病鏈夊凡鏀舵湇鐨勭簿鐏?,
                24,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            SetAnchors(empty.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            empty.rectTransform.sizeDelta = new Vector2(520f, 180f);
            empty.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

#endif

    private void CreateCompanionCard(CompanionDefinition definition, int index)
    {
        bool isSelected = selectedCompanionIds.Contains(definition.captureId);
        RectTransform card = CreatePanel(
            $"Card_{definition.captureId}",
            companionGrid,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(178f, 210f));
        int column = index % 4;
        int row = index / 4;
        card.anchoredPosition = new Vector2(
            -300f + column * 196f,
            180f - row * 228f);
        UIImage cardImage = card.GetComponent<UIImage>();
        if (cardImage != null)
        {
            cardImage.raycastTarget = true;
            cardImage.color = isSelected
                ? new Color(1f, 0.92f, 0.36f, 0.95f)
                : new Color(0.12f, 0.16f, 0.22f, 0.88f);
        }

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = cardImage;
        button.transition = Selectable.Transition.None;
        string captureId = definition.captureId;
        button.onClick.AddListener(() => ToggleCompanionSelection(captureId));

        RectTransform portraitRect = CreatePortraitGraphic(
            "Portrait",
            card,
            definition);
        SetAnchors(portraitRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        portraitRect.sizeDelta = new Vector2(132f, 112f);
        portraitRect.anchoredPosition = new Vector2(0f, -64f);

        TMP_Text nameText = CreateText(
            "Name",
            card,
            definition.displayName,
            20,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        SetAnchors(nameText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f));
        nameText.rectTransform.offsetMin = new Vector2(10f, 44f);
        nameText.rectTransform.offsetMax = new Vector2(-10f, 74f);

        TMP_Text affectionText = CreateText(
            "Affection",
            card,
            BuildCompanionCardStats(definition.captureId),
            15,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetAnchors(affectionText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f));
        affectionText.rectTransform.offsetMin = new Vector2(10f, 34f);
        affectionText.rectTransform.offsetMax = new Vector2(-10f, 72f);

        DisableChildRaycasts(card, card.GetComponent<Graphic>());
    }

    private string BuildCompanionCardStats(string captureId)
    {
        string carried = ARBookCompanionBattleRoster.IsInParty(captureId)
            ? "已携带"
            : "未携带";
        return
            $"好感 {GetAffection(captureId)}  心情 {ARBookCompanionBattleRoster.GetMood(captureId)}\n" +
            $"攻击 {ARBookCompanionBattleRoster.GetAttack(captureId)}  {carried}";
    }

    private void ToggleCompanionSelection(string captureId)
    {
        selectedCompanionIds.Clear();
        selectedCompanionIds.Add(captureId);

        BuildCompanionGrid();
        RefreshCompanionDetail();
    }

    private void ToggleSelectedPartyMember()
    {
        string captureId = GetSelectedCompanionId();
        if (string.IsNullOrWhiteSpace(captureId))
        {
            RefreshCompanionDetail();
            return;
        }

        ARBookCompanionBattleRoster.TogglePartyMember(captureId);
        BuildCompanionGrid();
        RefreshCompanionDetail();
    }

    private void RefreshCompanionDetail()
    {
        RefreshPokemonPanelButtons();
        if (companionDetailText == null)
        {
            return;
        }

        if (selectedCompanionIds.Count == 0)
        {
            companionDetailText.text =
                "选择一个已收服的宝可梦。\n\n" +
                "携带：加入或移出战斗携带位，最多两个。\n" +
                "互动：增加好感和心情。\n" +
                "返回：关闭面板。";
            return;
        }

        string text = "已选择：\n";
        foreach (string captureId in selectedCompanionIds)
        {
            CompanionDefinition definition = FindCompanion(captureId);
            string display = definition != null ? definition.displayName : captureId;
            text +=
                $"- {display}  好感 {GetAffection(captureId)}  " +
                $"心情 {ARBookCompanionBattleRoster.GetMood(captureId)}  " +
                $"攻击 {ARBookCompanionBattleRoster.GetAttack(captureId)}\n";
        }

        string[] party = ARBookCompanionBattleRoster.GetParty();
        text += $"\n携带A：{(string.IsNullOrWhiteSpace(party[0]) ? "空" : party[0])}";
        text += $"\n携带B：{(string.IsNullOrWhiteSpace(party[1]) ? "空" : party[1])}";
        text += $"\n\n互动次数：{GetCompanionInteractionsRemaining()} / {maxCompanionInteractionsPerGame}";
        companionDetailText.text = text;
    }

    private void RefreshPokemonPanelButtons()
    {
        string captureId = GetSelectedCompanionId();
        SetButtonText(
            placeButton,
            !string.IsNullOrWhiteSpace(captureId) &&
            ARBookCompanionBattleRoster.IsInParty(captureId)
                ? "已携带"
                : "携带");
        SetButtonText(affectionButton, "互动");
        SetButtonText(clearCompanionsButton, "返回");
        SetButtonText(closeCompanionButton, "返回");

        if (placeButton != null)
        {
            placeButton.interactable = !string.IsNullOrWhiteSpace(captureId);
        }

        if (affectionButton != null)
        {
            affectionButton.interactable = !string.IsNullOrWhiteSpace(captureId);
        }

        if (clearCompanionsButton != null)
        {
            clearCompanionsButton.gameObject.SetActive(false);
        }
    }

    private static void SetButtonText(Button button, string text)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = text;
        }
    }

#if false
    private void RefreshCompanionDetailDisabled()
    {
        if (companionDetailText == null)
        {
            return;
        }

        if (selectedCompanionIds.Count == 0)
        {
            companionDetailText.text =
                "閫夋嫨涓€涓垨澶氫釜宸叉敹鏈嶇簿鐏点€俓n\n" +
                "鏀剧疆锛氭妸妯″瀷鐢熸垚鍒扮浉鏈哄墠鏂规垨浣犻厤缃殑鏀剧疆鏍硅妭鐐逛笅銆俓n" +
                "浜掑姩锛氭彁鍗囧ソ鎰熷害锛屽悗缁彲浠ユ帴鍔ㄧ敾鎴栦簨浠躲€?;
            return;
        }

        string text = "宸查€夋嫨锛歕n";
        foreach (string captureId in selectedCompanionIds)
        {
            CompanionDefinition definition = FindCompanion(captureId);
            string name = definition != null ? definition.displayName : captureId;
            text += $"- {name}  濂芥劅 {GetAffection(captureId)}\n";
        }

        text += $"\n褰撳墠鍦轰笂锛歿placedCompanions.Count} / {maxActiveCompanions}";
        companionDetailText.text = text;
    }

#endif

    private GameObject CreateCompanionInstance(CompanionDefinition definition, int index)
    {
        if (definition == null)
        {
            return null;
        }

        Transform parent = ResolveCompanionParent(definition);
        GameObject instance = ResolveSinglePokemonSceneModel(activeCompanionTarget);
        if (instance != null)
        {
            activeSceneCompanionModel = instance;
            instance.SetActive(!hideCompanionUntilImageTracked ||
                IsTracked(activeCompanionTarget));
            return instance;
        }

        GameObject source = definition.companionPrefab != null
            ? definition.companionPrefab
            : definition.sceneObject;
        if (source != null)
        {
            instance = Instantiate(source, parent);
            instance.SetActive(true);
        }

        if (instance == null)
        {
            Debug.LogWarning(
                $"Companion {definition.captureId} has no SinglePokemon model, prefab, or scene object.",
                this);
            return null;
        }

        instance.transform.SetParent(parent, false);
        bool useTargetSpace = activeCompanionTarget != null &&
            parent == activeCompanionTarget.transform;
        instance.transform.localPosition = useTargetSpace
            ? companionImageTargetLocalOffset
            : companionLocalOffset;
        instance.transform.localRotation = Quaternion.identity;
        return instance;
    }

    private string GetSelectedCompanionId()
    {
        foreach (string captureId in selectedCompanionIds)
        {
            return captureId;
        }

        if (!string.IsNullOrWhiteSpace(selectedBackpackPartyId))
        {
            return selectedBackpackPartyId;
        }

        return null;
    }

    private void ConfigureCompanionInstance(
        GameObject instance,
        CompanionDefinition definition)
    {
        if (instance == null || definition == null)
        {
            return;
        }

        ARVirtualPetController pet =
            instance.GetComponentInChildren<ARVirtualPetController>(true);
        if (pet == null)
        {
            pet = instance.AddComponent<ARVirtualPetController>();
        }

        pet.petId = definition.captureId;
        pet.onInteracted.RemoveListener(HandlePlacedCompanionInteracted);
        pet.onInteracted.AddListener(HandlePlacedCompanionInteracted);

        ARTouchTransform touchTransform =
            instance.GetComponent<ARTouchTransform>();
        if (touchTransform == null)
        {
            instance.AddComponent<ARTouchTransform>();
        }

        ARBookCompanionTapProxy tapProxy =
            instance.GetComponent<ARBookCompanionTapProxy>();
        if (tapProxy == null)
        {
            tapProxy = instance.AddComponent<ARBookCompanionTapProxy>();
        }

        tapProxy.petController = pet;
        EnsureCompanionColliders(instance);
    }

    private void HandlePlacedCompanionInteracted()
    {
        if (!string.IsNullOrWhiteSpace(activeCompanionId))
        {
            if (!TryConsumeCompanionInteraction())
            {
                RefreshCompanionDetail();
                return;
            }

            AddAffection(activeCompanionId, companionInteractionAffectionGain);
            ARBookCompanionBattleRoster.AddMood(
                activeCompanionId,
                ARBookCompanionBattleRoster.InteractionMoodGain);
            RefreshCompanionDetail();
        }
    }

    private void HandleCompanionScreenInput()
    {
        if (string.IsNullOrWhiteSpace(activeCompanionId) ||
            (companionRoot != null && companionRoot.gameObject.activeInHierarchy) ||
            (dialogueRoot != null && dialogueRoot.gameObject.activeInHierarchy) ||
            (battleRoot != null && battleRoot.gameObject.activeInHierarchy))
        {
            return;
        }

        if (!TryGetCompanionPointerDown(out Vector2 screenPosition))
        {
            return;
        }

        Camera rayCamera = Camera.main;
        if (rayCamera == null)
        {
            return;
        }

        Ray ray = rayCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return;
        }

        if (!placedCompanions.TryGetValue(activeCompanionId, out GameObject instance) ||
            instance == null ||
            (!hit.transform.IsChildOf(instance.transform) && hit.transform.gameObject != instance))
        {
            return;
        }

        ARVirtualPetController pet =
            hit.transform.GetComponentInParent<ARVirtualPetController>();
        if (pet == null)
        {
            pet = instance.GetComponentInChildren<ARVirtualPetController>(true);
        }

        if (pet == null)
        {
            return;
        }

        pet.Pet();
    }

    private static bool TryGetCompanionPointerDown(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began ||
                IsPointerOverUi(touch.fingerId))
            {
                return false;
            }

            screenPosition = touch.position;
            return true;
        }

        if (!Input.GetMouseButtonDown(0) ||
            IsPointerOverUi(-1))
        {
            return false;
        }

        screenPosition = Input.mousePosition;
        return true;
    }

    private static bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
    }

    private void AddAffection(string captureId, int amount)
    {
        if (string.IsNullOrWhiteSpace(captureId))
        {
            return;
        }

        int value = Mathf.Clamp(GetAffection(captureId) + amount, 0, 100);
        PlayerPrefs.SetInt(GetAffectionKey(captureId), value);
        PlayerPrefs.Save();
    }

    private int GetCompanionInteractionsUsed()
    {
        return PlayerPrefs.GetInt(CompanionInteractionCountKey, 0);
    }

    private int GetCompanionInteractionsRemaining()
    {
        return Mathf.Max(
            0,
            maxCompanionInteractionsPerGame - GetCompanionInteractionsUsed());
    }

    private bool TryConsumeCompanionInteraction()
    {
        if (GetCompanionInteractionsRemaining() <= 0)
        {
            if (companionDetailText != null)
            {
                companionDetailText.text = "本局互动次数已经用完。战斗后心情会慢慢恢复，也可以重新开始清空次数。";
            }

            RefreshCompanionInteractionButton();
            return false;
        }

        PlayerPrefs.SetInt(
            CompanionInteractionCountKey,
            GetCompanionInteractionsUsed() + 1);
        PlayerPrefs.Save();
        RefreshCompanionInteractionButton();
        return true;
    }

    private void RefreshCompanionInteractionButton()
    {
        if (companionCameraInteractButton != null)
        {
            TMP_Text label =
                companionCameraInteractButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text =
                    $"互动（{GetCompanionInteractionsRemaining()}/{maxCompanionInteractionsPerGame}）";
            }

            companionCameraInteractButton.interactable =
                !string.IsNullOrWhiteSpace(activeCompanionId) &&
                GetCompanionInteractionsRemaining() > 0;
        }
    }

    private Transform ResolveCompanionParent(CompanionDefinition definition)
    {
        VuforiaObserverBehaviour target = activeCompanionTarget;
        if (target == null)
        {
            target = FindCompanionImageTarget(definition);
            activeCompanionTarget = target;
        }

        if (target != null)
        {
            return target.transform;
        }

        return companionPlacementRoot != null
            ? companionPlacementRoot
            : transform;
    }

    private VuforiaObserverBehaviour FindCompanionImageTarget(
        CompanionDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        string targetName = GetCompanionTargetName(definition);
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        VuforiaObserverBehaviour[] observers =
            FindObjectsOfType<VuforiaObserverBehaviour>(true);
        for (int i = 0; i < observers.Length; i++)
        {
            VuforiaObserverBehaviour observer = observers[i];
            if (observer != null &&
                IsSinglePokemonObserver(observer) &&
                string.Equals(
                    NormalizeCompanionAssetName(observer.TargetName),
                    NormalizeCompanionAssetName(targetName),
                    StringComparison.OrdinalIgnoreCase))
            {
                return observer;
            }
        }

        return null;
    }

    private bool IsSinglePokemonObserver(VuforiaObserverBehaviour observer)
    {
        Transform root = FindSinglePokemonRoot();
        return root != null &&
               observer != null &&
               observer.transform.IsChildOf(root);
    }

    private Transform FindSinglePokemonRoot()
    {
        GameObject[] roots = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject candidate = roots[i];
            if (candidate != null &&
                candidate.scene.IsValid() &&
                string.Equals(
                    candidate.name,
                    singlePokemonRootName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return candidate.transform;
            }
        }

        return null;
    }

    private void SetSinglePokemonTargetsActive(
        VuforiaObserverBehaviour selectedTarget)
    {
        Transform root = FindSinglePokemonRoot();
        if (root == null)
        {
            return;
        }

        bool hasSelection = selectedTarget != null;
        root.gameObject.SetActive(hasSelection);

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            VuforiaObserverBehaviour observer =
                child.GetComponent<VuforiaObserverBehaviour>();
            child.gameObject.SetActive(observer != null &&
                observer == selectedTarget);
        }
    }

    private void SetAllSinglePokemonTargetsActive()
    {
        Transform root = FindSinglePokemonRoot();
        if (root == null)
        {
            return;
        }

        root.gameObject.SetActive(true);

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            VuforiaObserverBehaviour observer =
                child.GetComponent<VuforiaObserverBehaviour>();
            child.gameObject.SetActive(observer != null);
        }
    }

    private void SetNonSinglePokemonTargetsActive(bool active)
    {
        Transform singleRoot = FindSinglePokemonRoot();
        VuforiaObserverBehaviour[] observers =
            FindObjectsOfType<VuforiaObserverBehaviour>(true);
        for (int i = 0; i < observers.Length; i++)
        {
            VuforiaObserverBehaviour observer = observers[i];
            if (observer == null)
            {
                continue;
            }

            if (singleRoot != null && observer.transform.IsChildOf(singleRoot))
            {
                continue;
            }

            if (!observer.gameObject.activeSelf && active)
            {
                observer.gameObject.SetActive(true);
            }

            observer.enabled = active;
        }
    }

    private static GameObject ResolveSinglePokemonSceneModel(
        VuforiaObserverBehaviour target)
    {
        if (target == null)
        {
            return null;
        }

        for (int i = 0; i < target.transform.childCount; i++)
        {
            Transform child = target.transform.GetChild(i);
            if (child.GetComponentInChildren<Renderer>(true) != null)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static string GetCompanionTargetName(CompanionDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(definition.imageTargetName)
            ? ResolveDefaultImageTargetName(definition.captureId)
            : definition.imageTargetName;
    }

    private static string NormalizeCompanionAssetName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string normalized = value.Trim();
        if (normalized.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            normalized = System.IO.Path.GetFileNameWithoutExtension(normalized);
        }

        const string scaledSuffix = "_scaled";
        if (normalized.EndsWith(scaledSuffix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - scaledSuffix.Length);
        }

        return normalized;
    }

    private void RefreshPlacedCompanionTracking(bool forceLookup = false)
    {
        if (string.IsNullOrWhiteSpace(activeCompanionId) ||
            !placedCompanions.TryGetValue(activeCompanionId, out GameObject instance) ||
            instance == null)
        {
            return;
        }

        CompanionDefinition definition = FindCompanion(activeCompanionId);
        if (definition == null)
        {
            return;
        }

        if (forceLookup || (activeCompanionTarget == null &&
            Time.unscaledTime >= nextCompanionTargetLookupTime))
        {
            activeCompanionTarget = FindCompanionImageTarget(definition);
            nextCompanionTargetLookupTime = Time.unscaledTime + 1f;
        }

        if (activeCompanionTarget != null)
        {
            SetSinglePokemonTargetsActive(activeCompanionTarget);

            if (instance != activeSceneCompanionModel &&
                instance.transform.parent != activeCompanionTarget.transform)
            {
                instance.transform.SetParent(activeCompanionTarget.transform, false);
                instance.transform.localPosition = companionImageTargetLocalOffset;
                instance.transform.localRotation = Quaternion.identity;
            }

            bool tracked = IsTracked(activeCompanionTarget);
            instance.SetActive(!hideCompanionUntilImageTracked || tracked);
            RefreshCompanionCameraHud();
            return;
        }

        if (instance.transform.parent == null)
        {
            instance.transform.SetParent(
                companionPlacementRoot != null ? companionPlacementRoot : transform,
                false);
        }

        instance.SetActive(!hideCompanionUntilImageTracked);
        RefreshCompanionCameraHud();
    }

    private static void EnsureCompanionColliders(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (instance.GetComponent<Collider>() != null)
        {
            return;
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            instance.AddComponent<SphereCollider>();
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 center = instance.transform.InverseTransformPoint(bounds.center);
        Vector3 size = bounds.size;
        float radius = Mathf.Max(size.x, size.y, size.z) * 0.35f;
        SphereCollider collider = instance.AddComponent<SphereCollider>();
        collider.center = center;
        collider.radius = Mathf.Max(0.05f, radius);
    }

    private string GetCurrentQuestText()
    {
        ARBookQuestTracker tracker = FindActiveQuestTracker();
        if (tracker != null)
        {
            tracker.RefreshUI();
            return BuildQuestTrackerText(tracker);
        }

        ARBookChapterObjectiveManager objective = FindActiveObjectiveManager();
        if (objective != null)
        {
            objective.RefreshUI();
            int count = Mathf.Min(
                objective.CollectedCount,
                objective.requiredCollectibleCount);
            string state = objective.IsObjectiveCompleted
                ? "\u5df2\u5b8c\u6210"
                : $"{count} / {objective.requiredCollectibleCount}";
            return $"\u5730\u56fe {objective.chapterId}\n[\u5f53\u524d] \u4efb\u52a1\u8fdb\u5ea6\uff1a{state}";
        }

        ARBookChallenge challenge = FindActiveChallenge();
        if (challenge != null)
        {
            return challenge.IsCompleted
                ? $"\u5730\u56fe {challenge.chapterId}\n[\u5b8c\u6210] \u673a\u5173\u5df2\u5b8c\u6210\uff0c\u5bfb\u627e\u5e76\u6536\u670d\u8fd9\u5f20\u5730\u56fe\u7684\u7cbe\u7075"
                : $"\u5730\u56fe {challenge.chapterId}\n[\u5f53\u524d] \u5b8c\u6210\u5730\u56fe\u673a\u5173\uff0c\u5e76\u5bfb\u627e\u53ef\u4ee5\u6536\u670d\u7684\u7cbe\u7075";
        }

        int activeMap = DetectActiveChapter();
        if (activeMap > 0)
        {
            return $"\u5730\u56fe {activeMap}\n[\u5f53\u524d] \u63a2\u7d22\u5730\u56fe\uff0c\u5bfb\u627e\u53ef\u4ee5\u5bf9\u8bdd\u548c\u6536\u670d\u7684\u7cbe\u7075";
        }

        return "\u7b49\u5f85\u8bc6\u522b\u5730\u56fe\u4e66\u9875\n[\u5f53\u524d] \u7ffb\u5f00\u4efb\u610f\u5730\u56fe\u56fe\u7247\u5f00\u59cb\u5192\u9669";
    }

    private string BuildProgressText()
    {
        int currentChapter = DetectActiveChapter();
        int completed = GetCompletedChapterCount();
        string current = currentChapter > 0
            ? $"\u5f53\u524d\u5730\u56fe\uff1a{currentChapter} / 5"
            : "\u5f53\u524d\u5730\u56fe\uff1a\u672a\u8bc6\u522b";
        return $"{current}\n\u5df2\u63a2\u7d22\u5730\u56fe\uff1a{completed} / 5\n\u8bc6\u522b\u4e66\u9875\u540e\uff0c\u4efb\u52a1\u680f\u4f1a\u81ea\u52a8\u66f4\u65b0\u3002";
    }

    private string BuildQuestTrackerText(ARBookQuestTracker tracker)
    {
        string title = tracker.chapterId == 1
            ? "\u7b2c\u4e00\u7ae0\uff1a\u68ee\u6797\u521d\u9047"
            : $"\u5730\u56fe {tracker.chapterId}";

        string stepText;
        switch (tracker.CurrentStep)
        {
            case ARBookQuestTracker.QuestStep.TalkToMentor:
                stepText = "\u4e0e\u5bfc\u5e08\u4ea4\u8c08";
                break;
            case ARBookQuestTracker.QuestStep.CollectFragments:
                stepText = BuildCollectFragmentsText(tracker);
                break;
            case ARBookQuestTracker.QuestStep.TalkToCreature:
                stepText = $"\u4e0e {GetTrackerCreatureName(tracker)} \u4ea4\u8c08";
                break;
            case ARBookQuestTracker.QuestStep.CaptureCreature:
                stepText = $"\u6536\u670d {GetTrackerCreatureName(tracker)}";
                break;
            case ARBookQuestTracker.QuestStep.ReachChapterEnd:
                stepText = "\u524d\u5f80\u5730\u56fe\u7ec8\u70b9";
                break;
            default:
                return $"{title}\n[\u5b8c\u6210] \u5730\u56fe\u4efb\u52a1\u5b8c\u6210";
        }

        return $"{title}\n[\u5f53\u524d] {stepText}";
    }

    private string BuildCollectFragmentsText(ARBookQuestTracker tracker)
    {
        ARBookChapterObjectiveManager objective = tracker.objectiveManager;
        if (objective == null)
        {
            return "\u6536\u96c6\u4efb\u52a1\u7269\u54c1";
        }

        int count = Mathf.Min(
            objective.CollectedCount,
            objective.requiredCollectibleCount);
        return $"\u6536\u96c6\u4efb\u52a1\u7269\u54c1 ({count} / {objective.requiredCollectibleCount})";
    }

    private static string GetTrackerCreatureName(ARBookQuestTracker tracker)
    {
        if (tracker.creature != null)
        {
            return tracker.creature.GetDisplayName();
        }

        return string.IsNullOrWhiteSpace(tracker.requiredCaptureId)
            ? "\u7cbe\u7075"
            : tracker.requiredCaptureId;
    }

#if false
    private string GetCurrentQuestTextDisabledForEncodingFallback()
    {
        ARBookQuestTracker tracker = FindActiveQuestTracker();
        if (tracker != null)
        {
            tracker.RefreshUI();
            return BuildQuestTrackerText(tracker);
        }

        ARBookChapterObjectiveManager objective = FindActiveObjectiveManager();
        if (objective != null)
        {
            objective.RefreshUI();
            int count = Mathf.Min(
                objective.CollectedCount,
                objective.requiredCollectibleCount);
            string state = objective.IsObjectiveCompleted
                ? "宸插畬鎴?
                : $"{count} / {objective.requiredCollectibleCount}";
            return $"鍦板浘 {objective.chapterId}\n[褰撳墠] 浠诲姟杩涘害锛歿state}";
        }

        ARBookChallenge challenge = FindActiveChallenge();
        if (challenge != null)
        {
            return challenge.IsCompleted
                ? $"鍦板浘 {challenge.chapterId}\n[瀹屾垚] 鏈哄叧宸插畬鎴愶紝瀵绘壘骞舵敹鏈嶈繖寮犲湴鍥剧殑绮剧伒"
                : $"鍦板浘 {challenge.chapterId}\n[褰撳墠] 瀹屾垚鍦板浘鏈哄叧锛屽苟瀵绘壘鍙互鏀舵湇鐨勭簿鐏?;
        }

        int activeMap = DetectActiveChapter();
        if (activeMap > 0)
        {
            return $"鍦板浘 {activeMap}\n[褰撳墠] 鎺㈢储鍦板浘锛屽鎵惧彲浠ュ璇濆拰鏀舵湇鐨勭簿鐏?;
        }

        return "绛夊緟璇嗗埆鍦板浘涔﹂〉\n[褰撳墠] 缈诲紑浠绘剰鍦板浘鍥剧墖寮€濮嬪啋闄?;
    }

#if false
    private string GetCurrentQuestTextDisabled()
    {
        ARBookQuestTracker tracker = FindActiveQuestTracker();
        if (tracker != null)
        {
            tracker.RefreshUI();
            if (tracker.questTMPText != null &&
                !string.IsNullOrWhiteSpace(tracker.questTMPText.text))
            {
                return tracker.questTMPText.text;
            }
        }

        ARBookChapterObjectiveManager objective = FindActiveObjectiveManager();
        if (objective != null)
        {
            objective.RefreshUI();
            return $"鍦板浘 {objective.chapterId}\n[褰撳墠] {objective.GetProgressText()}";
        }

        ARBookChallenge challenge = FindActiveChallenge();
        if (challenge != null)
        {
            return challenge.IsCompleted
                ? $"鍦板浘 {challenge.chapterId}\n[瀹屾垚] 鏈哄叧宸插畬鎴愶紝瀵绘壘骞舵敹鏈嶈繖寮犲湴鍥剧殑绮剧伒"
                : $"鍦板浘 {challenge.chapterId}\n[褰撳墠] 瀹屾垚鍦板浘鏈哄叧骞跺鎵惧彲鏀舵湇绮剧伒";
        }

        int activeMap = DetectActiveChapter();
        if (activeMap > 0)
        {
            return $"鍦板浘 {activeMap}\n[褰撳墠] 鎺㈢储鍦板浘锛屽鎵惧彲瀵硅瘽鍜屽彲鏀舵湇鐨勭簿鐏?;
        }

        return "绛夊緟璇嗗埆鍦板浘涔﹂〉\n[褰撳墠] 缈诲紑浠绘剰鍦板浘鍥剧墖寮€濮嬪啋闄?;
    }

#endif

    private string BuildProgressText()
    {
        int currentChapter = DetectActiveChapter();
        int completed = GetCompletedChapterCount();
        string current = currentChapter > 0
            ? $"褰撳墠鍦板浘锛歿currentChapter} / 5"
            : "褰撳墠鍦板浘锛氭湭璇嗗埆";
        return $"{current}\n宸叉帰绱㈠湴鍥撅細{completed} / 5\n璇嗗埆涔﹂〉鍚庯紝浠诲姟鏍忎細鑷姩鏇存柊銆?;
    }

    private string BuildQuestTrackerText(ARBookQuestTracker tracker)
    {
        string title = tracker.chapterId == 1
            ? "绗竴绔狅細妫灄鍒濋亣"
            : $"鍦板浘 {tracker.chapterId}";

        string stepText;
        switch (tracker.CurrentStep)
        {
            case ARBookQuestTracker.QuestStep.TalkToMentor:
                stepText = "涓庡甯堜氦璋?;
                break;
            case ARBookQuestTracker.QuestStep.CollectFragments:
                stepText = BuildCollectFragmentsText(tracker);
                break;
            case ARBookQuestTracker.QuestStep.TalkToCreature:
                stepText = $"涓?{GetTrackerCreatureName(tracker)} 浜よ皥";
                break;
            case ARBookQuestTracker.QuestStep.CaptureCreature:
                stepText = $"鏀舵湇 {GetTrackerCreatureName(tracker)}";
                break;
            case ARBookQuestTracker.QuestStep.ReachChapterEnd:
                stepText = "鍓嶅線鍦板浘缁堢偣";
                break;
            default:
                return $"{title}\n[瀹屾垚] 鍦板浘浠诲姟瀹屾垚";
        }

        return $"{title}\n[褰撳墠] {stepText}";
    }

    private string BuildCollectFragmentsText(ARBookQuestTracker tracker)
    {
        ARBookChapterObjectiveManager objective = tracker.objectiveManager;
        if (objective == null)
        {
            return "鏀堕泦浠诲姟鐗╁搧";
        }

        int count = Mathf.Min(
            objective.CollectedCount,
            objective.requiredCollectibleCount);
        return $"鏀堕泦浠诲姟鐗╁搧 ({count} / {objective.requiredCollectibleCount})";
    }

    private static string GetTrackerCreatureName(ARBookQuestTracker tracker)
    {
        if (tracker.creature != null)
        {
            return tracker.creature.GetDisplayName();
        }

        return string.IsNullOrWhiteSpace(tracker.requiredCaptureId)
            ? "绮剧伒"
            : tracker.requiredCaptureId;
    }

#endif

#if false
    private string BuildProgressTextDisabled()
    {
        int currentChapter = DetectActiveChapter();
        int completed = GetCompletedChapterCount();
        string current = currentChapter > 0
            ? $"褰撳墠鍦板浘锛歿currentChapter} / 5"
            : "褰撳墠鍦板浘锛氭湭璇嗗埆";
        return $"{current}\n宸叉帰绱㈠湴鍥撅細{completed} / 5\n鐩告満缈讳功鍚庯紝甯搁┗浠诲姟鏍忎細鑷姩鏇存柊銆?;
    }

#endif

    private int DetectActiveChapter()
    {
        if (TryDetectTrackedChapter(out int trackedChapterId))
        {
            return trackedChapterId;
        }

        if (HasVuforiaTargets())
        {
            return 0;
        }

        if (chapterHudController != null && chapterHudController.chapterRoots != null)
        {
            for (int i = 0; i < chapterHudController.chapterRoots.Length; i++)
            {
                GameObject root = chapterHudController.chapterRoots[i];
                if (root != null && root.activeInHierarchy)
                {
                    return i + 1;
                }
            }
        }

        ARBookQuestTracker tracker = FindActiveQuestTracker();
        if (tracker != null)
        {
            return tracker.chapterId;
        }

        ARBookChapterObjectiveManager objective = FindActiveObjectiveManager();
        if (objective != null)
        {
            return objective.chapterId;
        }

        ARBookChallenge challenge = FindActiveChallenge();
        if (challenge != null)
        {
            return challenge.chapterId;
        }

        ARBookChapterCompletionTrigger trigger = FindActiveCompletionTrigger();
        if (trigger != null)
        {
            return trigger.chapterId;
        }

        return 0;
    }

    private ARBookQuestTracker FindActiveQuestTracker()
    {
        ARBookQuestTracker[] trackers = FindObjectsOfType<ARBookQuestTracker>(true);
        ARBookQuestTracker tracked = FindTrackedComponent(trackers);
        if (tracked != null)
        {
            return tracked;
        }

        if (HasVuforiaTargets())
        {
            return null;
        }

        for (int i = 0; i < trackers.Length; i++)
        {
            if (trackers[i] != null && trackers[i].gameObject.activeInHierarchy)
            {
                return trackers[i];
            }
        }

        return null;
    }

    private ARBookChapterObjectiveManager FindActiveObjectiveManager()
    {
        ARBookChapterObjectiveManager[] managers =
            FindObjectsOfType<ARBookChapterObjectiveManager>(true);
        ARBookChapterObjectiveManager tracked = FindTrackedComponent(managers);
        if (tracked != null)
        {
            return tracked;
        }

        if (HasVuforiaTargets())
        {
            return null;
        }

        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null && managers[i].gameObject.activeInHierarchy)
            {
                return managers[i];
            }
        }

        return null;
    }

    private ARBookChallenge FindActiveChallenge()
    {
        ARBookChallenge[] challenges = FindObjectsOfType<ARBookChallenge>(true);
        ARBookChallenge tracked = FindTrackedComponent(challenges);
        if (tracked != null)
        {
            return tracked;
        }

        if (HasVuforiaTargets())
        {
            return null;
        }

        for (int i = 0; i < challenges.Length; i++)
        {
            if (challenges[i] != null && challenges[i].gameObject.activeInHierarchy)
            {
                return challenges[i];
            }
        }

        return null;
    }

    private ARBookChapterCompletionTrigger FindActiveCompletionTrigger()
    {
        ARBookChapterCompletionTrigger[] triggers =
            FindObjectsOfType<ARBookChapterCompletionTrigger>(true);
        ARBookChapterCompletionTrigger tracked = FindTrackedComponent(triggers);
        if (tracked != null)
        {
            return tracked;
        }

        if (HasVuforiaTargets())
        {
            return null;
        }

        for (int i = 0; i < triggers.Length; i++)
        {
            if (triggers[i] != null && triggers[i].gameObject.activeInHierarchy)
            {
                return triggers[i];
            }
        }

        return null;
    }

    private bool TryDetectTrackedChapter(out int chapterId)
    {
        chapterId = 0;
        if (chapterHudController == null || chapterHudController.chapterRoots == null)
        {
            return false;
        }

        for (int i = 0; i < chapterHudController.chapterRoots.Length; i++)
        {
            GameObject root = chapterHudController.chapterRoots[i];
            if (root != null && IsInsideTrackedTarget(root.transform))
            {
                chapterId = i + 1;
                return true;
            }
        }

        return false;
    }

    private T FindTrackedComponent<T>(T[] components) where T : Component
    {
        if (components == null)
        {
            return null;
        }

        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component != null && IsInsideTrackedTarget(component.transform))
            {
                return component;
            }
        }

        return null;
    }

    private static bool IsInsideTrackedTarget(Transform transform)
    {
        if (transform == null)
        {
            return false;
        }

        VuforiaObserverBehaviour observer =
            transform.GetComponentInParent<VuforiaObserverBehaviour>(true);
        return IsTracked(observer);
    }

    private static bool HasVuforiaTargets()
    {
        return FindObjectsOfType<VuforiaObserverBehaviour>(true).Length > 0;
    }

    private static bool IsTracked(VuforiaObserverBehaviour observer)
    {
        if (observer == null)
        {
            return false;
        }

        VuforiaStatus status = observer.TargetStatus.Status;
        return status == VuforiaStatus.TRACKED ||
               status == VuforiaStatus.EXTENDED_TRACKED;
    }

    private int GetCompletedChapterCount()
    {
        int count = 0;
        if (chapterProgress == null)
        {
            return count;
        }

        for (int i = 1; i <= 5; i++)
        {
            if (chapterProgress.IsChapterCompleted(i))
            {
                count++;
            }
        }

        return count;
    }

    private bool HasAnyCompletedChapter()
    {
        return GetCompletedChapterCount() > 0;
    }

    private List<string> GetCapturedIds()
    {
        List<string> ids = new List<string>();
        string raw = PlayerPrefs.GetString(CapturedIdsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ids;
        }

        string[] split = raw.Split(',');
        for (int i = 0; i < split.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(split[i]) && !ids.Contains(split[i]))
            {
                ids.Add(split[i]);
            }
        }

        return ids;
    }

    private string BuildCapturedNameList(List<string> capturedIds)
    {
        if (capturedIds == null || capturedIds.Count == 0)
        {
            return "暂无已收服精灵。";
        }

        string text = "已收服列表：\n";
        for (int i = 0; i < capturedIds.Count; i++)
        {
            CompanionDefinition definition = FindCompanion(capturedIds[i]);
            text += $"- {(definition != null ? definition.displayName : capturedIds[i])}\n";
        }

        return text;
    }

#if false
    private string BuildCapturedNameListDisabled(List<string> capturedIds)
    {
        if (capturedIds == null || capturedIds.Count == 0)
        {
            return "鏆傛棤宸叉敹鏈嶇簿鐏点€?;
        }

        string text = "宸叉敹鏈嶅垪琛細\n";
        for (int i = 0; i < capturedIds.Count; i++)
        {
            CompanionDefinition definition = FindCompanion(capturedIds[i]);
            text += $"- {(definition != null ? definition.displayName : capturedIds[i])}\n";
        }

        return text;
    }

#endif

    private bool IsCaptured(string captureId)
    {
        if (string.IsNullOrWhiteSpace(captureId))
        {
            return false;
        }

        return collectionManager != null
            ? collectionManager.IsCaptured(captureId)
            : PlayerPrefs.GetInt($"Captured_{captureId}", 0) == 1;
    }

    private CompanionDefinition FindCompanion(string captureId)
    {
        if (companions == null)
        {
            return null;
        }

        for (int i = 0; i < companions.Length; i++)
        {
            if (companions[i] != null && companions[i].captureId == captureId)
            {
                return companions[i];
            }
        }

        return null;
    }

    private int GetAffection(string captureId)
    {
        return PlayerPrefs.GetInt(GetAffectionKey(captureId), 0);
    }

    private string GetAffectionKey(string captureId)
    {
        return AffectionPrefix + captureId;
    }

    private void ClearCompanionState()
    {
        PlayerPrefs.DeleteKey(CompanionInteractionCountKey);
        ARBookCompanionBattleRoster.Clear();

        if (companions == null)
        {
            return;
        }

        for (int i = 0; i < companions.Length; i++)
        {
            if (companions[i] != null && !string.IsNullOrWhiteSpace(companions[i].captureId))
            {
                PlayerPrefs.DeleteKey(GetAffectionKey(companions[i].captureId));
            }
        }
    }

    private void HideLegacyHud()
    {
        if (!hideLegacyChapterHudTexts || chapterHudController == null)
        {
            return;
        }

        SetTextObjectActive(chapterHudController.questText, false);
        SetTextObjectActive(chapterHudController.chapterProgressText, false);
        SetTextObjectActive(chapterHudController.challengeText, false);
    }

    private void HideTransientUi()
    {
        SetRootActive(dialogueRoot, false);
        SetRootActive(battleRoot, false);
    }

    public void HideActionButtons()
    {
        SetRootActive(actionButtonsRoot, false);
    }

    private static void SetTextObjectActive(TMP_Text text, bool active)
    {
        if (text != null)
        {
            text.gameObject.SetActive(active);
        }
    }

    private bool IsChildOfRootCanvas(Transform target)
    {
        return generatedRoot != null && target.IsChildOf(generatedRoot);
    }

    private static RectTransform FindRect(Transform root, params string[] names)
    {
        return FindComponentInNamedChild<RectTransform>(root, names);
    }

    private static Button FindButton(Transform root, params string[] names)
    {
        return FindComponentInNamedChild<Button>(root, names);
    }

    private static TMP_Text FindText(Transform root, params string[] names)
    {
        return FindComponentInNamedChild<TMP_Text>(root, names);
    }

    private static T FindComponentInNamedChild<T>(Transform root, params string[] names)
        where T : Component
    {
        if (root == null || names == null)
        {
            return null;
        }

        for (int i = 0; i < names.Length; i++)
        {
            Transform child = FindDescendant(root, names[i]);
            T component = child != null ? child.GetComponent<T>() : null;
            if (component != null)
            {
                return component;
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

    private RectTransform CreateFullRoot(string name)
    {
        RectTransform rect = CreateRect(name, generatedRoot);
        Stretch(rect, 0f, 0f, 0f, 0f);
        return rect;
    }

    private RectTransform CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent);
        SetAnchors(rect, anchorMin, anchorMax);
        rect.sizeDelta = size;
        UIImage image = rect.gameObject.AddComponent<UIImage>();
        image.raycastTarget = false;
        return rect;
    }

    private TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        int fontSize,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        if (chineseFont != null)
        {
            text.font = chineseFont;
        }

        return text;
    }

    private UIImage CreateImage(string name, Transform parent, Sprite sprite)
    {
        RectTransform rect = CreateRect(name, parent);
        UIImage image = rect.gameObject.AddComponent<UIImage>();
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    private static UIImage AddEditableBackground(RectTransform rect)
    {
        if (rect == null)
        {
            return null;
        }

        UIImage image = rect.GetComponent<UIImage>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<UIImage>();
        }

        image.raycastTarget = false;
        return image;
    }

    private RectTransform CreatePortraitGraphic(
        string name,
        Transform parent,
        CompanionDefinition definition)
    {
        RectTransform rect = CreateRect(name, parent);
        UIImage background = rect.gameObject.AddComponent<UIImage>();
        background.color = new Color(0.18f, 0.21f, 0.28f, 1f);
        background.raycastTarget = false;

        Texture2D portraitTexture = ResolveCompanionPortraitTexture(definition);
        if (portraitTexture != null)
        {
            RectTransform imageRect = CreateRect("PortraitTexture", rect);
            Stretch(imageRect, 4f, 4f, 4f, 4f);
            RawImage rawImage = imageRect.gameObject.AddComponent<RawImage>();
            rawImage.texture = portraitTexture;
            rawImage.color = Color.white;
            rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            rawImage.raycastTarget = false;
            AspectRatioFitter fitter =
                imageRect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = portraitTexture.height > 0
                ? (float)portraitTexture.width / portraitTexture.height
                : 1f;
            imageRect.SetAsLastSibling();
            rect.SetAsLastSibling();
            return rect;
        }

        if (definition != null && definition.portrait != null)
        {
            RectTransform imageRect = CreateRect("PortraitSprite", rect);
            Stretch(imageRect, 0f, 0f, 0f, 0f);
            UIImage image = imageRect.gameObject.AddComponent<UIImage>();
            image.sprite = definition.portrait;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            AspectRatioFitter fitter =
                imageRect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = definition.portrait.rect.height > 0f
                ? definition.portrait.rect.width / definition.portrait.rect.height
                : 1f;
            imageRect.SetAsLastSibling();
            rect.SetAsLastSibling();
            return rect;
        }

        TMP_Text missing = CreateText(
            "MissingPortrait",
            rect,
            "未绑定图片",
            16,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        Stretch(missing.rectTransform, 6f, 6f, 6f, 6f);
        return rect;
    }

    private static Texture2D ResolveCompanionPortraitTexture(
        CompanionDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        if (definition.portraitTexture != null)
        {
            return definition.portraitTexture;
        }

#if UNITY_EDITOR
        string[] keys =
        {
            definition.imageTargetName,
            definition.captureId,
            definition.displayName,
            GetCompanionTargetName(definition)
        };
        const string textureFolder =
            "Assets/Editor/Vuforia/ImageTargetTextures/mcfAR";
        string[] guids = AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { textureFolder });
        for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
        {
            string normalizedKey = NormalizeCompanionAssetName(keys[keyIndex]);
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                continue;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!string.Equals(
                    NormalizeCompanionAssetName(name),
                    normalizedKey,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }
#endif

        return null;
    }

    private static void DisableChildRaycasts(RectTransform root, Graphic except)
    {
        if (root == null)
        {
            return;
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null && graphics[i] != except)
            {
                graphics[i].raycastTarget = false;
            }
        }
    }

    private Slider CreateEditableSlider(string name, Transform parent)
    {
        RectTransform root = CreatePanel(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(320f, 28f));
        UIImage background = root.GetComponent<UIImage>();
        background.raycastTarget = false;

        RectTransform fillArea = CreateRect("Fill Area", root);
        Stretch(fillArea, 6f, 4f, 6f, 4f);

        RectTransform fill = CreateRect("Fill", fillArea);
        Stretch(fill, 0f, 0f, 0f, 0f);
        UIImage fillImage = fill.gameObject.AddComponent<UIImage>();
        fillImage.raycastTarget = false;

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.fillRect = fill;
        slider.targetGraphic = background;
        return slider;
    }

    private Button CreateButton(string name, Transform parent, string label, int fontSize)
    {
        RectTransform rect = CreatePanel(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(260f, 68f));
        UIImage image = rect.GetComponent<UIImage>();
        image.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        TMP_Text text = CreateText(
            "Label",
            rect,
            label,
            fontSize,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        Stretch(text.rectTransform, 12f, 4f, 12f, 4f);
        ApplyDefaultButtonPlacement(name, parent, button);
        return button;
    }

    private static void ApplyDefaultButtonPlacement(
        string name,
        Transform parent,
        Button button)
    {
        if (button == null || parent == null)
        {
            return;
        }

        if (parent.name == "HUDButtons")
        {
            if (name == "BackpackButton")
            {
                SetButtonRect(button, new Vector2(-210f, 0f), new Vector2(190f, 68f));
            }
            else if (name == "CompanionButton")
            {
                SetButtonRect(button, new Vector2(0f, 0f), new Vector2(190f, 68f));
            }
            else if (name == "HomeButton")
            {
                SetButtonRect(button, new Vector2(210f, 0f), new Vector2(190f, 68f));
            }
        }
        else if (parent.name == "Actions")
        {
            if (name == "PlaceButton")
            {
                SetButtonRect(button, new Vector2(-390f, 0f), new Vector2(250f, 68f));
            }
            else if (name == "AffectionButton")
            {
                SetButtonRect(button, new Vector2(-130f, 0f), new Vector2(250f, 68f));
            }
            else if (name == "ClearButton")
            {
                SetButtonRect(button, new Vector2(130f, 0f), new Vector2(250f, 68f));
            }
            else if (name == "CloseButton")
            {
                SetButtonRect(button, new Vector2(390f, 0f), new Vector2(250f, 68f));
            }
        }
        else if (parent.name == "BackpackPanel" && name == "CloseButton")
        {
            SetButtonRect(button, new Vector2(0f, -190f), new Vector2(420f, 70f));
        }
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void SetButtonRect(
        Button button,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        SetAnchors(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }

    private static void SetBottomRightButtonRect(
        Button button,
        Vector2 size,
        Vector2 margin)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(-margin.x, margin.y);
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(
            Mathf.Lerp(min.x, max.x, 0.5f),
            Mathf.Lerp(min.y, max.y, 0.5f));
    }

    private static void Stretch(
        RectTransform rect,
        float left,
        float top,
        float right,
        float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetRootActive(RectTransform root, bool active)
    {
        if (root != null)
        {
            root.gameObject.SetActive(active);
        }
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
