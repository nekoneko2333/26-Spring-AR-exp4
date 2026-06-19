using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;

public class ARBookCompanionStaticSlotOverlay : MonoBehaviour
{
    [Serializable]
    public class SlotBinding
    {
        public Button button;
        public UIImage portraitImage;
    }

    private const int SlotsPerPage = 12;
    private const string CapturedIdsKey = "CapturedIds";
    private const string AffectionPrefix = "CompanionAffection_";

    [Header("Data")]
    public ARBookGameShellController controller;
    [Min(1)] public int carryLimit = 2;

    [Header("Existing UI References")]
    public RectTransform root;
    public SlotBinding[] slots = new SlotBinding[SlotsPerPage];
    public TMP_Text detailText;
    public TMP_Text carryCountText;
    public TMP_Text pageText;
    public Button prevButton;
    public Button nextButton;
    public Button carryButton;
    public TMP_Text carryButtonText;

    private readonly List<ARBookGameShellController.CompanionDefinition> captured =
        new List<ARBookGameShellController.CompanionDefinition>();
    private int pageIndex;
    private string selectedId;

    private void Awake()
    {
        ResolveController();
        BindRuntimeButtons();
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void BindExistingUiByName()
    {
        ResolveController();
        EnsureSlotArray();

        Transform searchRoot = root != null
            ? root
            : controller != null && controller.companionRoot != null
                ? controller.companionRoot
                : transform;

        if (root == null)
        {
            root = FindDescendant(searchRoot, "CompanionStaticSlotOverlay") as RectTransform;
        }

        Transform actualRoot = root != null ? root : searchRoot;
        Transform gridRoot = FindDescendant(actualRoot, "SlotGrid");
        Transform detailRoot = FindDescendant(actualRoot, "DetailPanel");

        detailText = detailText != null
            ? detailText
            : FindText(detailRoot, "DetailText") ?? FindText(actualRoot, "DetailText");
        carryCountText = carryCountText != null
            ? carryCountText
            : FindText(actualRoot, "CarryCountText");
        pageText = pageText != null
            ? pageText
            : FindText(actualRoot, "PageText");
        prevButton = prevButton != null
            ? prevButton
            : FindButton(actualRoot, "PrevPageButton", "CompanionPrevPageButton");
        nextButton = nextButton != null
            ? nextButton
            : FindButton(actualRoot, "NextPageButton", "CompanionNextPageButton");
        carryButton = carryButton != null
            ? carryButton
            : FindButton(detailRoot, "CarryButton", "PlaceButton") ??
              FindButton(actualRoot, "CarryButton", "PlaceButton");
        carryButtonText = carryButtonText != null
            ? carryButtonText
            : carryButton != null
                ? carryButton.GetComponentInChildren<TMP_Text>(true)
                : null;

        Transform slotSearchRoot = gridRoot != null ? gridRoot : actualRoot;
        for (int i = 0; i < SlotsPerPage; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = new SlotBinding();
            }

            Transform slot = FindDescendant(slotSearchRoot, $"StaticSlot_{i:00}") ??
                             FindDescendant(slotSearchRoot, $"Slot_{i:00}") ??
                             FindDescendant(slotSearchRoot, $"Slot{i + 1}");

            if (slot == null)
            {
                continue;
            }

            if (slots[i].button == null)
            {
                slots[i].button = slot.GetComponent<Button>() ??
                                  slot.GetComponentInChildren<Button>(true);
            }

            if (slots[i].portraitImage == null)
            {
                Transform portrait = FindDescendant(slot, "Portrait");
                slots[i].portraitImage = portrait != null
                    ? portrait.GetComponent<UIImage>()
                    : slot.GetComponentInChildren<UIImage>(true);
            }
        }

    }

    private void ResolveController()
    {
        if (controller == null)
        {
            controller = GetComponent<ARBookGameShellController>();
        }

        if (controller == null)
        {
            controller = FindObjectOfType<ARBookGameShellController>(true);
        }
    }

    private void EnsureSlotArray()
    {
        if (slots == null || slots.Length != SlotsPerPage)
        {
            SlotBinding[] next = new SlotBinding[SlotsPerPage];
            if (slots != null)
            {
                Array.Copy(slots, next, Mathf.Min(slots.Length, next.Length));
            }

            slots = next;
        }
    }

    private void BindRuntimeButtons()
    {
        EnsureSlotArray();

        for (int i = 0; i < slots.Length; i++)
        {
            SlotBinding slot = slots[i];
            if (slot == null || slot.button == null)
            {
                continue;
            }

            int slotIndex = i;
            slot.button.onClick.AddListener(() => SelectSlot(slotIndex));
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(PreviousPage);
            prevButton.onClick.AddListener(PreviousPage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextPage);
            nextButton.onClick.AddListener(NextPage);
        }

        if (carryButton != null)
        {
            carryButton.onClick.RemoveListener(ToggleCarrySelected);
            carryButton.onClick.AddListener(ToggleCarrySelected);
        }
    }

    private void Refresh()
    {
        if (controller == null)
        {
            return;
        }

        EnsureSlotArray();
        RefreshCaptured();
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(captured.Count / (float)SlotsPerPage));
        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);

        if (string.IsNullOrWhiteSpace(selectedId) && captured.Count > 0)
        {
            selectedId = captured[0].captureId;
        }

        for (int i = 0; i < SlotsPerPage; i++)
        {
            int capturedIndex = pageIndex * SlotsPerPage + i;
            ARBookGameShellController.CompanionDefinition definition =
                capturedIndex < captured.Count ? captured[capturedIndex] : null;
            UpdateSlot(i, definition);
        }

        bool hasMultiplePages = pageCount > 1;
        if (prevButton != null)
        {
            SetActive(prevButton.gameObject, hasMultiplePages);
            prevButton.interactable = pageIndex > 0;
        }

        if (nextButton != null)
        {
            SetActive(nextButton.gameObject, hasMultiplePages);
            nextButton.interactable = pageIndex < pageCount - 1;
        }

        if (pageText != null)
        {
            SetActive(pageText.gameObject, hasMultiplePages);
            pageText.text = $"{pageIndex + 1}/{pageCount}";
        }

        RefreshDetail();
    }

    private void RefreshCaptured()
    {
        captured.Clear();
        if (controller.companions == null)
        {
            return;
        }

        for (int i = 0; i < controller.companions.Length; i++)
        {
            ARBookGameShellController.CompanionDefinition definition = controller.companions[i];
            if (definition != null && IsCaptured(definition.captureId))
            {
                captured.Add(definition);
            }
        }
    }

    private void UpdateSlot(int slotIndex, ARBookGameShellController.CompanionDefinition definition)
    {
        SlotBinding slot = slots[slotIndex];
        if (slot == null)
        {
            return;
        }

        bool hasDefinition = definition != null;
        if (slot.button != null)
        {
            SetActive(slot.button.gameObject, hasDefinition);
            slot.button.interactable = hasDefinition;
        }

        if (slot.portraitImage != null)
        {
            slot.portraitImage.sprite = hasDefinition ? definition.portrait : null;
            slot.portraitImage.color =
                hasDefinition && definition.portrait != null ? Color.white : Color.clear;
            slot.portraitImage.preserveAspect = true;
        }
    }

    private void SelectSlot(int slotIndex)
    {
        int capturedIndex = pageIndex * SlotsPerPage + slotIndex;
        if (capturedIndex < 0 || capturedIndex >= captured.Count)
        {
            return;
        }

        selectedId = captured[capturedIndex].captureId;
        RefreshDetail();
    }

    private void ToggleCarrySelected()
    {
        if (string.IsNullOrWhiteSpace(selectedId))
        {
            return;
        }

        ARBookCompanionBattleRoster.TogglePartyMember(selectedId);
        RefreshDetail();
    }

    private void PreviousPage()
    {
        pageIndex = Mathf.Max(0, pageIndex - 1);
        Refresh();
    }

    private void NextPage()
    {
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(captured.Count / (float)SlotsPerPage));
        pageIndex = Mathf.Min(pageCount - 1, pageIndex + 1);
        Refresh();
    }

    private void RefreshDetail()
    {
        string[] party = ARBookCompanionBattleRoster.GetParty();
        int carriedCount = CountPartyMembers(party);
        if (carryCountText != null)
        {
            carryCountText.text = $"\u643a\u5e26 {carriedCount}/{carryLimit}";
        }

        ARBookGameShellController.CompanionDefinition selected = FindCompanion(selectedId);
        if (selected == null)
        {
            if (detailText != null)
            {
                detailText.text = captured.Count == 0
                    ? "\u8fd8\u6ca1\u6709\u6536\u670d\u5b9d\u53ef\u68a6"
                    : "\u9009\u62e9\u4e00\u53ea\u5b9d\u53ef\u68a6";
            }

            if (carryButton != null)
            {
                carryButton.interactable = false;
            }

            if (carryButtonText != null)
            {
                carryButtonText.text = "\u643a\u5e26";
            }

            return;
        }

        bool carried = ARBookCompanionBattleRoster.IsInParty(selected.captureId);
        string mood = ARBookCompanionBattleRoster.GetMood(selected.captureId).ToString();
        int affection = PlayerPrefs.GetInt(AffectionPrefix + selected.captureId, 0);
        if (detailText != null)
        {
            detailText.text =
                $"{selected.displayName}\n\n" +
                $"\u5fc3\u60c5\uff1a{mood}\n" +
                $"\u597d\u611f\uff1a{affection}\n" +
                $"\u72b6\u6001\uff1a{(carried ? "\u5df2\u643a\u5e26" : "\u672a\u643a\u5e26")}";
        }

        if (carryButton != null)
        {
            carryButton.interactable = true;
        }

        if (carryButtonText != null)
        {
            carryButtonText.text = carried ? "\u53d6\u6d88\u643a\u5e26" : "\u643a\u5e26";
        }
    }

    private bool IsCaptured(string captureId)
    {
        if (string.IsNullOrWhiteSpace(captureId))
        {
            return false;
        }

        if (controller.collectionManager != null)
        {
            return controller.collectionManager.IsCaptured(captureId);
        }

        return PlayerPrefs.GetInt($"Captured_{captureId}", 0) == 1 ||
               PlayerPrefs.GetString(CapturedIdsKey, string.Empty).Contains(captureId);
    }

    private ARBookGameShellController.CompanionDefinition FindCompanion(string captureId)
    {
        if (string.IsNullOrWhiteSpace(captureId) || controller.companions == null)
        {
            return null;
        }

        for (int i = 0; i < controller.companions.Length; i++)
        {
            ARBookGameShellController.CompanionDefinition definition = controller.companions[i];
            if (definition != null && definition.captureId == captureId)
            {
                return definition;
            }
        }

        return null;
    }

    private static int CountPartyMembers(string[] party)
    {
        int count = 0;
        if (party == null)
        {
            return count;
        }

        for (int i = 0; i < party.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(party[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static Transform FindDescendant(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == name)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform found = FindDescendant(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static TMP_Text FindText(Transform parent, string name)
    {
        Transform child = FindDescendant(parent, name);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Button FindButton(Transform parent, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = FindDescendant(parent, names[i]);
            Button button = child != null ? child.GetComponent<Button>() : null;
            if (button != null)
            {
                return button;
            }
        }

        return null;
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
        {
            go.SetActive(active);
        }
    }
}
