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
    public string playerName = "训练家";
    [Range(1, 999)] public int maxHP = 100;
    [Range(0, 999)] public int currentHP = 100;

    [Header("Runtime")]
    public bool showCoverOnStart = true;
    public bool hideLegacyChapterHudTexts = true;
    public bool themeExistingUiOnStart = true;
    public Canvas rootCanvas;
    public Transform companionPlacementRoot;
    [Range(1, 6)] public int maxActiveCompanions = 3;
    public float companionSpacing = 0.85f;
    public Vector3 companionLocalOffset = new Vector3(0f, 0f, 1.2f);

    [Header("Scene UI References")]
    public RectTransform generatedRoot;
    public RectTransform homeRoot;
    public RectTransform hudRoot;
    public RectTransform companionRoot;
    public RectTransform companionGrid;
    public RectTransform backpackRoot;
    public TMP_Text startButtonText;
    public TMP_Text questText;
    public TMP_Text progressText;
    public TMP_Text capturedCountText;
    public TMP_Text companionDetailText;
    public UIImage hpFill;
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

    private const string StartedKey = "ARBookHasStarted";
    private const string CapturedIdsKey = "CapturedIds";
    private const string AffectionPrefix = "CompanionAffection_";

    private GameObject runtimeEventSystem;

    private readonly HashSet<string> selectedCompanionIds = new HashSet<string>();
    private readonly Dictionary<string, GameObject> placedCompanions =
        new Dictionary<string, GameObject>();
    private float nextRefreshTime;

    private static readonly Color Navy = new Color(0.04f, 0.12f, 0.20f, 0.94f);
    private static readonly Color NavyLight = new Color(0.08f, 0.20f, 0.32f, 0.92f);
    private static readonly Color Gold = new Color(0.95f, 0.72f, 0.28f, 1f);
    private static readonly Color GoldDark = new Color(0.55f, 0.34f, 0.08f, 1f);
    private static readonly Color Paper = new Color(0.98f, 0.94f, 0.82f, 1f);
    private static readonly Color TextColor = new Color(0.96f, 0.98f, 1f, 1f);

    private void Reset()
    {
        ResetCatalogToDefault();
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureCatalog();
        EnsureCanvas();
        BindSceneInterface();
        if (IsSceneInterfaceMissing())
        {
            RebuildSceneInterface();
        }
        else
        {
            WireSceneButtons();
        }

        ApplyExistingUiTheme();
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

    public void ShowHome()
    {
        SetRootActive(homeRoot, true);
        SetRootActive(hudRoot, false);
        SetRootActive(companionRoot, false);
        SetRootActive(backpackRoot, false);
        RefreshHome();
    }

    public void BeginGame()
    {
        PlayerPrefs.SetInt(StartedKey, 1);
        PlayerPrefs.Save();
        SetRootActive(homeRoot, false);
        SetRootActive(hudRoot, true);
        SetRootActive(companionRoot, false);
        SetRootActive(backpackRoot, false);
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
        selectedCompanionIds.Clear();
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
        RefreshBackpack();
    }

    public void CloseBackpack()
    {
        SetRootActive(backpackRoot, false);
    }

    public void PlaceSelectedCompanions()
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

    public void DespawnAllCompanions()
    {
        DestroyPlacedCompanions(false);
        RefreshCompanionDetail();
    }

    private void DestroyPlacedCompanions(bool immediate)
    {
        foreach (KeyValuePair<string, GameObject> pair in placedCompanions)
        {
            if (pair.Value != null)
            {
                DestroyRuntimeObject(pair.Value, immediate);
            }
        }

        placedCompanions.Clear();
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
        foreach (string captureId in selectedCompanionIds)
        {
            int value = Mathf.Clamp(GetAffection(captureId) + 5, 0, 100);
            PlayerPrefs.SetInt(GetAffectionKey(captureId), value);
        }

        PlayerPrefs.Save();
        BuildCompanionGrid();
        RefreshCompanionDetail();
    }

    private static CompanionDefinition CreateCompanion(string captureId, string displayName)
    {
        return new CompanionDefinition
        {
            captureId = captureId,
            displayName = displayName
        };
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

        if (generatedRoot == null)
        {
            generatedRoot = CreateRect(
                "ARBookGameShellGeneratedRoot",
                rootCanvas.transform);
        }

        Stretch(generatedRoot, 0f, 0f, 0f, 0f);
    }

    public void RebuildSceneInterface()
    {
        EnsureCanvas();
        BuildInterface();
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

        homeRoot = FindRect(generatedRoot, "Home");
        hudRoot = FindRect(generatedRoot, "HUD");
        companionRoot = FindRect(generatedRoot, "CompanionMode");
        backpackRoot = FindRect(generatedRoot, "Backpack");
        companionGrid = FindRect(companionRoot, "CompanionGrid");
        startButton = FindButton(homeRoot, "StartButton");
        restartButton = FindButton(homeRoot, "RestartButton");
        homeCompanionButton = FindButton(homeRoot, "CompanionButton");
        backpackButton = FindButton(hudRoot, "BackpackButton");
        hudCompanionButton = FindButton(hudRoot, "CompanionButton");
        homeButton = FindButton(hudRoot, "HomeButton");
        placeButton = FindButton(companionRoot, "PlaceButton");
        affectionButton = FindButton(companionRoot, "AffectionButton");
        clearCompanionsButton = FindButton(companionRoot, "ClearButton");
        closeCompanionButton = FindButton(companionRoot, "CloseButton");
        closeBackpackButton = FindButton(backpackRoot, "CloseButton");
        startButtonText = startButton != null
            ? startButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        questText = FindText(hudRoot, "QuestText");
        progressText = FindText(hudRoot, "ProgressText");
        capturedCountText = FindText(backpackRoot, "BackpackText");
        companionDetailText = FindText(companionRoot, "DetailText");
        hpFill = FindComponentInNamedChild<UIImage>(hudRoot, "HPFill");
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
        button.onClick.AddListener(action);
    }

    private RectTransform BuildHome()
    {
        RectTransform root = CreateFullRoot("Home");
        UIImage shade = root.gameObject.AddComponent<UIImage>();
        shade.color = new Color(0.01f, 0.03f, 0.06f, 0.88f);

        RectTransform panel = CreatePanel(
            "HomePanel",
            root,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(760f, 620f));
        AddVerticalLayout(panel, 20f, 36, 36, 34, 34);

        TMP_Text title = CreateText(
            "Title",
            panel,
            "记忆图鉴 AR 冒险",
            48,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        SetLayout(title.rectTransform, 110f);

        TMP_Text subtitle = CreateText(
            "Subtitle",
            panel,
            "打开相机，翻动实体书页，在不同地图中收服精灵。",
            24,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        subtitle.color = Paper;
        SetLayout(subtitle.rectTransform, 74f);

        Button startButton = CreateButton("StartButton", panel, "开始游戏", 32);
        startButtonText = startButton.GetComponentInChildren<TMP_Text>();
        SetLayout(startButton.GetComponent<RectTransform>(), 76f);

        Button restartButton = CreateButton("RestartButton", panel, "清空存档 / 重新开始", 28);
        SetLayout(restartButton.GetComponent<RectTransform>(), 70f);

        Button companionButton = CreateButton("CompanionButton", panel, "陪伴模式", 28);
        SetLayout(companionButton.GetComponent<RectTransform>(), 70f);

        TMP_Text footer = CreateText(
            "Footer",
            panel,
            "进入游戏后保持摄像头对准书页，识别任意地图图像后即可移动、互动和收服。",
            21,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        footer.color = new Color(0.84f, 0.90f, 0.96f, 1f);
        SetLayout(footer.rectTransform, 88f);
        return root;
    }

    private RectTransform BuildHud()
    {
        RectTransform root = CreateFullRoot("HUD");

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
        AddOutline(avatar.gameObject, Gold, new Vector2(3f, -3f));

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
        hpBackImage.color = new Color(0.02f, 0.05f, 0.07f, 0.95f);
        AddOutline(hpBack.gameObject, GoldDark, new Vector2(2f, -2f));

        hpFill = CreateImage("HPFill", hpBack, null);
        SetAnchors(hpFill.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f));
        hpFill.rectTransform.offsetMin = new Vector2(4f, 4f);
        hpFill.rectTransform.offsetMax = new Vector2(-4f, -4f);
        hpFill.color = new Color(0.25f, 0.90f, 0.15f, 1f);

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
        progressText.color = Paper;
        SetAnchors(progressText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.36f));
        progressText.rectTransform.offsetMin = new Vector2(28f, 18f);
        progressText.rectTransform.offsetMax = new Vector2(-28f, -8f);

        RectTransform buttons = CreateRect("HUDButtons", root);
        SetAnchors(buttons, new Vector2(1f, 0f), new Vector2(1f, 0f));
        buttons.sizeDelta = new Vector2(620f, 88f);
        buttons.anchoredPosition = new Vector2(-28f, 34f);
        HorizontalLayoutGroup layout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;

        Button backpackButton = CreateButton("BackpackButton", buttons, "背包", 24);
        Button companionButton = CreateButton("CompanionButton", buttons, "陪伴", 24);
        Button homeButton = CreateButton("HomeButton", buttons, "首页", 24);

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

    private RectTransform BuildCompanionOverlay()
    {
        RectTransform root = CreateFullRoot("CompanionMode");
        UIImage shade = root.gameObject.AddComponent<UIImage>();
        shade.color = new Color(0f, 0f, 0f, 0.72f);

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
        GridLayoutGroup grid = companionGrid.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(178f, 210f);
        grid.spacing = new Vector2(18f, 18f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

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
        Stretch(companionDetailText.rectTransform, 24f, 24f, 24f, 24f);

        RectTransform actions = CreateRect("Actions", panel);
        SetAnchors(actions, new Vector2(0f, 0f), new Vector2(1f, 0f));
        actions.offsetMin = new Vector2(38f, 30f);
        actions.offsetMax = new Vector2(-38f, 98f);
        HorizontalLayoutGroup layout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;

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
        shade.color = new Color(0f, 0f, 0f, 0.58f);

        RectTransform panel = CreatePanel(
            "BackpackPanel",
            root,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(760f, 520f));
        AddVerticalLayout(panel, 18f, 30, 30, 30, 30);

        TMP_Text title = CreateText(
            "Title",
            panel,
            "背包",
            36,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        SetLayout(title.rectTransform, 64f);

        capturedCountText = CreateText(
            "BackpackText",
            panel,
            string.Empty,
            24,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft);
        SetLayout(capturedCountText.rectTransform, 300f);

        Button closeButton = CreateButton("CloseButton", panel, "关闭", 24);
        SetLayout(closeButton.GetComponent<RectTransform>(), 70f);

        return root;
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

            CreateCompanionCard(definition);
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
            SetLayout(empty.rectTransform, 180f);
        }
    }

    private void CreateCompanionCard(CompanionDefinition definition)
    {
        RectTransform card = CreatePanel(
            $"Card_{definition.captureId}",
            companionGrid,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(178f, 210f));
        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<UIImage>();
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
        affectionText.color = Paper;
        SetAnchors(affectionText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f));
        affectionText.rectTransform.offsetMin = new Vector2(10f, 14f);
        affectionText.rectTransform.offsetMax = new Vector2(-10f, 40f);

        if (selectedCompanionIds.Contains(definition.captureId))
        {
            AddOutline(card.gameObject, Color.white, new Vector2(4f, -4f));
        }
    }

    private void ToggleCompanionSelection(string captureId)
    {
        if (selectedCompanionIds.Contains(captureId))
        {
            selectedCompanionIds.Remove(captureId);
        }
        else
        {
            selectedCompanionIds.Add(captureId);
        }

        BuildCompanionGrid();
        RefreshCompanionDetail();
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

    private GameObject CreateCompanionInstance(CompanionDefinition definition, int index)
    {
        if (definition == null)
        {
            return null;
        }

        Transform parent = companionPlacementRoot != null
            ? companionPlacementRoot
            : transform;
        GameObject instance = null;

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
            return null;
        }

        instance.transform.SetParent(parent, false);
        float offset = (index - (maxActiveCompanions - 1) * 0.5f) * companionSpacing;
        instance.transform.localPosition = companionLocalOffset + new Vector3(offset, 0f, 0f);
        instance.transform.localRotation = Quaternion.identity;
        return instance;
    }

    private string GetCurrentQuestText()
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

    private string BuildProgressText()
    {
        int currentChapter = DetectActiveChapter();
        int completed = GetCompletedChapterCount();
        string current = currentChapter > 0
            ? $"当前地图：{currentChapter} / 5"
            : "当前地图：未识别";
        return $"{current}\n已探索地图：{completed} / 5\n相机翻书后，常驻任务栏会自动更新。";
    }

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

    private static void SetTextObjectActive(TMP_Text text, bool active)
    {
        if (text != null)
        {
            text.gameObject.SetActive(active);
        }
    }

    private void ApplyExistingUiTheme()
    {
        if (!themeExistingUiOnStart)
        {
            return;
        }

        TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null || IsChildOfRootCanvas(texts[i].transform))
            {
                continue;
            }

            if (chineseFont != null)
            {
                texts[i].font = chineseFont;
            }

            texts[i].color = TextColor;
        }

        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || IsChildOfRootCanvas(buttons[i].transform))
            {
                continue;
            }

            StyleButton(buttons[i]);
        }
    }

    private bool IsChildOfRootCanvas(Transform target)
    {
        return generatedRoot != null && target.IsChildOf(generatedRoot);
    }

    private static RectTransform FindRect(Transform root, string name)
    {
        return FindComponentInNamedChild<RectTransform>(root, name);
    }

    private static Button FindButton(Transform root, string name)
    {
        return FindComponentInNamedChild<Button>(root, name);
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        return FindComponentInNamedChild<TMP_Text>(root, name);
    }

    private static T FindComponentInNamedChild<T>(Transform root, string name)
        where T : Component
    {
        Transform child = FindDescendant(root, name);
        return child != null ? child.GetComponent<T>() : null;
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
        image.color = Navy;
        AddOutline(rect.gameObject, Gold, new Vector2(2.5f, -2.5f));
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
        text.color = TextColor;
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
        image.color = sprite != null ? Color.white : NavyLight;
        image.preserveAspect = true;
        return image;
    }

    private RectTransform CreatePortraitGraphic(
        string name,
        Transform parent,
        CompanionDefinition definition)
    {
        RectTransform rect = CreateRect(name, parent);
        if (definition != null && definition.portrait != null)
        {
            UIImage image = rect.gameObject.AddComponent<UIImage>();
            image.sprite = definition.portrait;
            image.color = Color.white;
            image.preserveAspect = true;
            return rect;
        }

        if (definition != null && definition.portraitTexture != null)
        {
            RawImage rawImage = rect.gameObject.AddComponent<RawImage>();
            rawImage.texture = definition.portraitTexture;
            rawImage.color = Color.white;
            return rect;
        }

        UIImage fallback = rect.gameObject.AddComponent<UIImage>();
        fallback.color = new Color(0.14f, 0.26f, 0.34f, 1f);
        return rect;
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
        image.color = Gold;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        StyleButton(button);

        TMP_Text text = CreateText(
            "Label",
            rect,
            label,
            fontSize,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        text.color = new Color(0.12f, 0.08f, 0.02f, 1f);
        Stretch(text.rectTransform, 12f, 4f, 12f, 4f);
        return button;
    }

    private void StyleButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        UIImage image = button.GetComponent<UIImage>();
        if (image != null)
        {
            image.color = Gold;
            if (image.GetComponent<Outline>() == null)
            {
                AddOutline(image.gameObject, GoldDark, new Vector2(2f, -2f));
            }
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Gold;
        colors.highlightedColor = new Color(1f, 0.82f, 0.36f, 1f);
        colors.pressedColor = new Color(0.72f, 0.48f, 0.12f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.35f, 0.32f, 0.28f, 0.7f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
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

    private static void SetLayout(RectTransform rect, float preferredHeight)
    {
        LayoutElement element = rect.gameObject.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = rect.gameObject.AddComponent<LayoutElement>();
        }

        element.preferredHeight = preferredHeight;
        element.minHeight = preferredHeight;
    }

    private static void AddVerticalLayout(
        RectTransform rect,
        float spacing,
        int left,
        int right,
        int top,
        int bottom)
    {
        VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(left, right, top, bottom);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = target.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = distance;
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
