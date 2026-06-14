using TMPro;
using UnityEngine;

public class ARBookQuestMarker : MonoBehaviour
{
    public ARBookQuestTracker questTracker;
    public ARBookQuestTracker.QuestStep visibleStep =
        ARBookQuestTracker.QuestStep.TalkToMentor;
    public GameObject markerRoot;
    public TMP_Text markerText;
    public TMP_FontAsset fontAsset;
    public Vector3 localOffset = new Vector3(0f, 1.5f, 0f);
    public float textSize = 8f;
    public float markerScale = 0.1f;
    public string symbol = "!";
    public Color markerColor = new Color(1f, 0.78f, 0.05f);
    public bool faceCamera = true;

    private void Start()
    {
        ResolveQuestTracker();
        CreateMarkerIfNeeded();

        if (markerText != null)
        {
            markerText.text = symbol;
            markerText.color = markerColor;
        }

        RefreshVisibility();
    }

    private void LateUpdate()
    {
        RefreshVisibility();

        if (!faceCamera || markerRoot == null || !markerRoot.activeInHierarchy)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            markerRoot.transform.rotation = Quaternion.LookRotation(
                markerRoot.transform.position - mainCamera.transform.position,
                Vector3.up);
        }
    }

    private void RefreshVisibility()
    {
        ResolveQuestTracker();

        if (markerRoot != null)
        {
            markerRoot.SetActive(
                questTracker != null &&
                questTracker.IsCurrentStep(visibleStep));
        }
    }

    private void CreateMarkerIfNeeded()
    {
        if (markerRoot != null && markerText != null)
        {
            return;
        }

        markerRoot = new GameObject("QuestMarker");
        markerRoot.transform.SetParent(transform, false);
        markerRoot.transform.localPosition = localOffset;
        markerRoot.transform.localScale = Vector3.one * markerScale;

        TextMeshPro text = markerRoot.AddComponent<TextMeshPro>();
        text.text = symbol;
        text.fontSize = textSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = markerColor;
        text.fontStyle = FontStyles.Bold;

        if (fontAsset != null)
        {
            text.font = fontAsset;
        }

        markerText = text;
    }

    private void ResolveQuestTracker()
    {
        if (questTracker != null)
        {
            return;
        }

        questTracker = GetComponentInParent<ARBookQuestTracker>(true);
        if (questTracker == null)
        {
            questTracker = FindObjectOfType<ARBookQuestTracker>();
        }
    }
}
