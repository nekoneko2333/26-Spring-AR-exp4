using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;
using VuforiaObserverBehaviour = Vuforia.ObserverBehaviour;
using VuforiaStatus = Vuforia.Status;

public class ARBookGameShellController : MonoBehaviour
{
    [Serializable]
    public class CompanionDefinition
    {
        public string captureId;
        public string displayName;
        public string imageTargetName;
        public Sprite portrait;
        public Texture2D portraitTexture;
        public GameObject companionPrefab;
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
/*
    public string playerName = "训练家";
*/
/*
    public string playerName = "训练家";
*/
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
    public UIImage hpFill;
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
    public Slider battleLeftHPSlider;
    public Slider battleRightHPSlider;

    private const string StartedKey = "ARBookHasStarted";
    private const string CapturedIdsKey = "CapturedIds";
    private const string AffectionPrefix = "CompanionAffection_";

    private GameObject runtimeEventSystem;

    private readonly HashSet<string> selectedCompanionIds = new HashSet<string>();
    private readonly Dictionary<string, GameObject> placedCompanions =
        new Dictionary<string, GameObject>();
    private string activeCompanionId;
    private VuforiaObserverBehaviour activeCompanionTarget;
    private GameObject activeSceneCompanionModel;
    private float nextCompanionTargetLookupTime;
    private float nextRefreshTime;

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
            CreateCompanion("Bulbasaur", "妙蛙种子"),
            CreateCompanion("Talonflame", "烈箭鹰"),
            CreateCompanion("Axew", "牙牙"),
            CreateCompanion("Pikachu", "皮卡丘"),
            CreateCompanion("Meowth", "喵喵"),
            CreateCompanion("Infernape", "烈焰猴"),
            CreateCompanion("Squirtle", "杰尼龟"),
            CreateCompanion("Jirachi", "基拉祈"),
            CreateCompanion("Sneasler", "狃拉"),
            CreateCompanion("Zorua", "索罗亚"),
            CreateCompanion("Zekrom", "捷克罗姆"),
            CreateCompanion("Zygarde10", "基格尔德10%形态"),
            CreateCompanion("Toxtricity", "颤弦蝾螈"),
            CreateCompanion("Scizor", "巨钳螳螂"),
            CreateCompanion("Mismagius", "梦妖魔"),
            CreateCompanion("Mew", "梦幻"),
            CreateCompanion("Manaphy", "玛纳霏"),
            CreateCompanion("ElectrodeHisuian", "霹雳电球（洗翠的样子）"),
            CreateCompanion("Dragapult", "多龙巴鲁托"),
            CreateCompanion("Celebi", "时拉比")
        };
*/
        companions = new[]
        {
            CreateCompanion("Bulbasaur", "妙蛙种子"),
            CreateCompanion("Talonflame", "烈箭鹰"),
            CreateCompanion("Axew", "牙牙"),
            CreateCompanion("Pikachu", "皮卡丘"),
            CreateCompanion("Meowth", "喵喵"),
            CreateCompanion("Infernape", "烈焰猴"),
            CreateCompanion("Squirtle", "杰尼龟"),
            CreateCompanion("Jirachi", "基拉祈"),
            CreateCompanion("Sneasler", "狃拉"),
            CreateCompanion("Zorua", "索罗亚"),
            CreateCompanion("Zekrom", "捷克罗姆"),
            CreateCompanion("Zygarde10", "基格尔德10%形态"),
            CreateCompanion("Toxtricity", "颤弦蝾螈"),
            CreateCompanion("Scizor", "巨钳螳螂"),
            CreateCompanion("Mismagius", "梦妖魔"),
            CreateCompanion("Mew", "梦幻"),
            CreateCompanion("Manaphy", "玛纳霏"),
            CreateCompanion("ElectrodeHisuian", "霹雳电球（洗翠的样子）"),
            CreateCompanion("Dragapult", "多龙巴鲁托"),
            CreateCompanion("Celebi", "时拉比")
        };
    }

#endif

    public void ShowHome()
    {
        SetSinglePokemonTargetsActive(null);
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
        if (string.IsNullOrWhiteSpace(activeCompanionId))
        {
            SetSinglePokemonTargetsActive(null);
        }

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
    }

    public void CloseCompanionMode()
    {
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
        SetRootActive(backpackRoot, true);
        SetRootActive(companionRoot, false);
        HideActionButtons();
        RefreshBackpack();
    }

    public void CloseBackpack()
    {
        SetRootActive(backpackRoot, false);
    }

    public void ApplyDefaultUiVisibility()
    {
        BindSceneInterface();
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
        GameObject instance = CreateCompanionInstance(definition, 0);
        if (instance != null)
        {
            placedCompanions[captureId] = instance;
            ConfigureCompanionInstance(instance, definition);
            RefreshPlacedCompanionTracking(true);
        }

        RefreshCompanionDetail();
        CloseCompanionModeToCamera();
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
        RefreshAll();
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
            AddAffection(captureId, companionInteractionAffectionGain);
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
        clearCompanionsButton = FindButton(companionRoot, "ClearButton", "ClearCompanionsButton") ?? clearCompanionsButton;
        closeCompanionButton = FindButton(companionRoot, "CloseButton", "CloseCompanionButton") ?? closeCompanionButton;
        closeBackpackButton = FindButton(backpackRoot, "CloseButton", "CloseBackpackButton") ?? closeBackpackButton;
        dialogueContinueButton = FindButton(dialogueRoot, "ContinueButton", "NextButton") ?? dialogueContinueButton;
        battleAttackButton = FindButton(battleRoot, "AttackButton") ?? battleAttackButton;
        battleExitButton = FindButton(battleRoot, "ExitButton", "CloseButton") ?? battleExitButton;
        startButtonText = startButton != null
            ? startButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        questText = FindText(hudRoot, "QuestText") ?? questText;
        progressText = FindText(hudRoot, "ProgressText") ?? progressText;
        capturedCountText = FindText(backpackRoot, "BackpackText") ?? capturedCountText;
        companionDetailText = FindText(companionRoot, "DetailText") ?? companionDetailText;
        dialogueSpeakerText = FindText(dialogueRoot, "SpeakerNameText", "SpeakerName") ?? dialogueSpeakerText;
        dialogueBodyText = FindText(dialogueRoot, "DialogueText") ?? dialogueBodyText;
        battleMessageText = FindText(battleRoot, "BattleMessageText", "MessageText") ?? battleMessageText;
        battleLeftHPText = FindText(battleRoot, "LeftHPText", "EnemyHPText") ?? battleLeftHPText;
        battleRightHPText = FindText(battleRoot, "RightHPText", "PlayerHPText") ?? battleRightHPText;
        hpFill = FindComponentInNamedChild<UIImage>(hudRoot, "HPFill") ?? hpFill;
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
        WireButton(homeCompanionButton, OpenCompanionMode);
        WireButton(backpackButton, OpenBackpack);
        WireButton(hudCompanionButton, OpenCompanionMode);
        WireButton(homeButton, ShowHome);
        WireButton(placeButton, PlaceSelectedCompanions);
        WireButton(affectionButton, AddAffectionToSelected);
        WireButton(clearCompanionsButton, DespawnAllCompanions);
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
            "Open the camera, turn the book pages, explore maps, and capture creatures.",
            24,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetAnchors(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        subtitle.rectTransform.sizeDelta = new Vector2(690f, 74f);
        subtitle.rectTransform.anchoredPosition = new Vector2(0f, -176f);

        Button start = CreateButton("StartButton", panel, "Start", 32);
        startButtonText = start.GetComponentInChildren<TMP_Text>();
        SetButtonRect(start, new Vector2(0f, -275f), new Vector2(560f, 76f));

        Button restart = CreateButton("RestartButton", panel, "Restart", 28);
        SetButtonRect(restart, new Vector2(0f, -365f), new Vector2(560f, 70f));

        Button companion = CreateButton("CompanionButton", panel, "Companion", 28);
        SetButtonRect(companion, new Vector2(0f, -445f), new Vector2(560f, 70f));

        TMP_Text footer = CreateText(
            "Footer",
            panel,
            "Keep the book page in camera view. Recognized maps enable movement, interaction, and capture.",
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
            "记忆图鉴 AR 冒险",
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

        Button startButton = CreateButton("StartButton", panel, "开始游戏", 32);
        startButtonText = startButton.GetComponentInChildren<TMP_Text>();
        SetButtonRect(startButton, new Vector2(0f, -275f), new Vector2(560f, 76f));

        Button restartButton = CreateButton("RestartButton", panel, "清空存档 / 重新开始", 28);
        SetButtonRect(restartButton, new Vector2(0f, -365f), new Vector2(560f, 70f));

        Button companionButton = CreateButton("CompanionButton", panel, "陪伴模式", 28);
        SetButtonRect(companionButton, new Vector2(0f, -445f), new Vector2(560f, 70f));

        TMP_Text footer = CreateText(
            "Footer",
            panel,
            "进入游戏后保持摄像头对准书页，识别任意地图图像后即可移动、互动和收服。",
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
        CreateButton("BackpackButton", buttons, "Bag", 24);
        CreateButton("CompanionButton", buttons, "Companion", 24);
        CreateButton("HomeButton", buttons, "Home", 24);

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
            "陪伴模式",
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
        Button placeButton = CreateButton("PlaceButton", actions, "放置选中", 24);
        Button affectionButton = CreateButton("AffectionButton", actions, "互动 + 好感", 24);
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

        dialogueContinueButton = CreateButton("ContinueButton", panel, "Continue", 23);
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
            "Choose action",
            26,
            FontStyles.Bold,
            TextAlignmentOptions.Left);
        SetAnchors(battleMessageText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f));
        battleMessageText.rectTransform.offsetMin = new Vector2(40f, 24f);
        battleMessageText.rectTransform.offsetMax = new Vector2(-560f, -24f);

        battleAttackButton = CreateButton("AttackButton", controls, "Attack", 32);
        SetButtonRect(battleAttackButton, new Vector2(0f, 0f), new Vector2(310f, 92f));
        RectTransform attackRect = battleAttackButton.GetComponent<RectTransform>();
        SetAnchors(attackRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        attackRect.anchoredPosition = new Vector2(260f, 0f);

        battleExitButton = CreateButton("ExitButton", controls, "Exit", 24);
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
        startButtonText.text = hasStarted ? "Continue" : "Start";
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
    }

    private void RefreshBackpack()
    {
        if (capturedCountText == null)
        {
            return;
        }

        List<string> captured = GetCapturedIds();
        int completedChapters = GetCompletedChapterCount();
        capturedCountText.text =
            $"已收服精灵：{captured.Count}\n" +
            $"已探索地图：{completedChapters} / 5\n\n" +
            BuildCapturedNameList(captured);
    }

    private void BuildCompanionGrid()
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
                "还没有已收服的精灵",
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
        portraitRect.sizeDelta = new Vector2(118f, 118f);
        portraitRect.anchoredPosition = new Vector2(0f, -70f);

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
            $"好感 {GetAffection(definition.captureId)}",
            17,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetAnchors(affectionText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f));
        affectionText.rectTransform.offsetMin = new Vector2(10f, 14f);
        affectionText.rectTransform.offsetMax = new Vector2(-10f, 40f);
        DisableChildRaycasts(card, card.GetComponent<Graphic>());
    }

    private void ToggleCompanionSelection(string captureId)
    {
        selectedCompanionIds.Clear();
        selectedCompanionIds.Add(captureId);

        BuildCompanionGrid();
        RefreshCompanionDetail();
        PlaceSelectedCompanions();
    }

    private void RefreshCompanionDetail()
    {
        if (companionDetailText == null)
        {
            return;
        }

        if (selectedCompanionIds.Count == 0)
        {
            companionDetailText.text =
                "\u9009\u62e9\u4e00\u4e2a\u5df2\u6536\u670d\u7684\u7cbe\u7075\u3002\n\n" +
                "\u786e\u8ba4\uff1a\u5173\u95ed\u7a97\u53e3\u5e76\u56de\u5230\u6444\u50cf\u5934\u753b\u9762\u3002\n" +
                "\u8bc6\u522b\uff1a\u5bf9\u51c6\u5bf9\u5e94\u5b9d\u53ef\u68a6\u56fe\u7247\u540e\u663e\u793a\u6a21\u578b\u3002\n" +
                "\u4e92\u52a8\uff1a\u70b9\u51fb\u6a21\u578b\u6216\u6309\u94ae\u63d0\u5347\u597d\u611f\u5ea6\u3002";
            return;
        }

        string text = "\u5df2\u9009\u62e9\uff1a\n";
        foreach (string captureId in selectedCompanionIds)
        {
            CompanionDefinition definition = FindCompanion(captureId);
            string display = definition != null ? definition.displayName : captureId;
            text += $"- {display}  \u597d\u611f {GetAffection(captureId)}\n";
        }

        text += $"\n\u5df2\u653e\u7f6e\uff1a{placedCompanions.Count} / 1";
        companionDetailText.text = text;
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
                "选择一个或多个已收服精灵。\n\n" +
                "放置：把模型生成到相机前方或你配置的放置根节点下。\n" +
                "互动：提升好感度，后续可以接动画或事件。";
            return;
        }

        string text = "已选择：\n";
        foreach (string captureId in selectedCompanionIds)
        {
            CompanionDefinition definition = FindCompanion(captureId);
            string name = definition != null ? definition.displayName : captureId;
            text += $"- {name}  好感 {GetAffection(captureId)}\n";
        }

        text += $"\n当前场上：{placedCompanions.Count} / {maxActiveCompanions}";
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
            AddAffection(activeCompanionId, companionInteractionAffectionGain);
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

            instance.SetActive(!hideCompanionUntilImageTracked ||
                IsTracked(activeCompanionTarget));
            return;
        }

        if (instance.transform.parent == null)
        {
            instance.transform.SetParent(
                companionPlacementRoot != null ? companionPlacementRoot : transform,
                false);
        }

        instance.SetActive(!hideCompanionUntilImageTracked);
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
                ? "已完成"
                : $"{count} / {objective.requiredCollectibleCount}";
            return $"地图 {objective.chapterId}\n[当前] 任务进度：{state}";
        }

        ARBookChallenge challenge = FindActiveChallenge();
        if (challenge != null)
        {
            return challenge.IsCompleted
                ? $"地图 {challenge.chapterId}\n[完成] 机关已完成，寻找并收服这张地图的精灵"
                : $"地图 {challenge.chapterId}\n[当前] 完成地图机关，并寻找可以收服的精灵";
        }

        int activeMap = DetectActiveChapter();
        if (activeMap > 0)
        {
            return $"地图 {activeMap}\n[当前] 探索地图，寻找可以对话和收服的精灵";
        }

        return "等待识别地图书页\n[当前] 翻开任意地图图片开始冒险";
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
            return $"地图 {objective.chapterId}\n[当前] {objective.GetProgressText()}";
        }

        ARBookChallenge challenge = FindActiveChallenge();
        if (challenge != null)
        {
            return challenge.IsCompleted
                ? $"地图 {challenge.chapterId}\n[完成] 机关已完成，寻找并收服这张地图的精灵"
                : $"地图 {challenge.chapterId}\n[当前] 完成地图机关并寻找可收服精灵";
        }

        int activeMap = DetectActiveChapter();
        if (activeMap > 0)
        {
            return $"地图 {activeMap}\n[当前] 探索地图，寻找可对话和可收服的精灵";
        }

        return "等待识别地图书页\n[当前] 翻开任意地图图片开始冒险";
    }

#endif

    private string BuildProgressText()
    {
        int currentChapter = DetectActiveChapter();
        int completed = GetCompletedChapterCount();
        string current = currentChapter > 0
            ? $"当前地图：{currentChapter} / 5"
            : "当前地图：未识别";
        return $"{current}\n已探索地图：{completed} / 5\n识别书页后，任务栏会自动更新。";
    }

    private string BuildQuestTrackerText(ARBookQuestTracker tracker)
    {
        string title = tracker.chapterId == 1
            ? "第一章：森林初遇"
            : $"地图 {tracker.chapterId}";

        string stepText;
        switch (tracker.CurrentStep)
        {
            case ARBookQuestTracker.QuestStep.TalkToMentor:
                stepText = "与导师交谈";
                break;
            case ARBookQuestTracker.QuestStep.CollectFragments:
                stepText = BuildCollectFragmentsText(tracker);
                break;
            case ARBookQuestTracker.QuestStep.TalkToCreature:
                stepText = $"与 {GetTrackerCreatureName(tracker)} 交谈";
                break;
            case ARBookQuestTracker.QuestStep.CaptureCreature:
                stepText = $"收服 {GetTrackerCreatureName(tracker)}";
                break;
            case ARBookQuestTracker.QuestStep.ReachChapterEnd:
                stepText = "前往地图终点";
                break;
            default:
                return $"{title}\n[完成] 地图任务完成";
        }

        return $"{title}\n[当前] {stepText}";
    }

    private string BuildCollectFragmentsText(ARBookQuestTracker tracker)
    {
        ARBookChapterObjectiveManager objective = tracker.objectiveManager;
        if (objective == null)
        {
            return "收集任务物品";
        }

        int count = Mathf.Min(
            objective.CollectedCount,
            objective.requiredCollectibleCount);
        return $"收集任务物品 ({count} / {objective.requiredCollectibleCount})";
    }

    private static string GetTrackerCreatureName(ARBookQuestTracker tracker)
    {
        if (tracker.creature != null)
        {
            return tracker.creature.GetDisplayName();
        }

        return string.IsNullOrWhiteSpace(tracker.requiredCaptureId)
            ? "精灵"
            : tracker.requiredCaptureId;
    }

#endif

#if false
    private string BuildProgressTextDisabled()
    {
        int currentChapter = DetectActiveChapter();
        int completed = GetCompletedChapterCount();
        string current = currentChapter > 0
            ? $"当前地图：{currentChapter} / 5"
            : "当前地图：未识别";
        return $"{current}\n已探索地图：{completed} / 5\n相机翻书后，常驻任务栏会自动更新。";
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
            return "No captured creatures.";
        }

        string text = "Captured:\n";
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

        if (definition != null && definition.portrait != null)
        {
            RectTransform imageRect = CreateRect("PortraitSprite", rect);
            Stretch(imageRect, 0f, 0f, 0f, 0f);
            UIImage image = imageRect.gameObject.AddComponent<UIImage>();
            image.sprite = definition.portrait;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        if (definition != null && definition.portraitTexture != null)
        {
            RectTransform imageRect = CreateRect("PortraitTexture", rect);
            Stretch(imageRect, 0f, 0f, 0f, 0f);
            RawImage rawImage = imageRect.gameObject.AddComponent<RawImage>();
            rawImage.texture = definition.portraitTexture;
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;
            return rect;
        }

        Debug.LogWarning(
            $"Companion portrait is not assigned for {definition?.captureId}.",
            this);
        return rect;
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
