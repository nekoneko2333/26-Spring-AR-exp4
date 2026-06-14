using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARViewAlignmentChallenge : ARBookChallenge
{
    [Header("Alignment")]
    public Transform alignmentTarget;
    public Vector3 expectedViewDirection = Vector3.forward;
    [Range(1f, 45f)] public float angleTolerance = 15f;
    public float requiredStableSeconds = 1f;

    [Header("UI")]
    public TMP_Text progressTMPText;
    public Text progressText;
    public string searchingText = "移动手机，寻找正确观察角度";
    public string holdingText = "保持观察：{0:0.0}s";
    public string completedText = "时间符号已对齐";
    public bool showProgressOnlyWhenTargetVisible = true;

    [Header("Anamorphic Text")]
    public bool createAnamorphicText;
    public string[] anamorphicTextParts = { "时", "间", "碎片" };
    public TMP_FontAsset anamorphicFont;
    public Color anamorphicTextColor = new Color(0.1f, 0.95f, 1f, 1f);
    public float anamorphicTextSize = 1.2f;
    public float anamorphicHorizontalSpacing = 0.58f;
    public float anamorphicDepthSpacing = 0.45f;
    public Vector3 anamorphicLocalOffset = new Vector3(0f, 1.25f, 0f);
    public bool hideAnamorphicTextOnCompleted;

    [Header("Feedback")]
    public DialogueManager dialogueManager;
    public string speakerName = "Celebi";
    [TextArea(2, 4)] public string completedDialogue = "时间碎片重新重合了。";
    public ParticleSystem completedEffect;
    public AudioSource audioSource;
    public AudioClip completedClip;
    public bool vibrateOnSuccess = true;

    private float stableTimer;
    private Renderer[] targetRenderers;
    private GameObject anamorphicRoot;

    protected override void Start()
    {
        base.Start();
        ResolveDialogueManager();
        CacheTargetRenderers();
        CreateAnamorphicTextIfNeeded();
        RefreshUI();
    }

    private void Update()
    {
        if (IsCompleted)
        {
            SetAnamorphicTextVisible(!hideAnamorphicTextOnCompleted);
            RefreshUI();
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null || alignmentTarget == null || !IsTargetVisible())
        {
            stableTimer = 0f;
            SetAnamorphicTextVisible(false);
            if (!showProgressOnlyWhenTargetVisible)
            {
                RefreshUI();
            }
            return;
        }

        SetAnamorphicTextVisible(true);

        Vector3 directionFromTargetToCamera =
            (mainCamera.transform.position - alignmentTarget.position).normalized;
        Vector3 expectedWorldDirection =
            alignmentTarget.TransformDirection(expectedViewDirection.normalized);
        float angle = Vector3.Angle(expectedWorldDirection, directionFromTargetToCamera);

        if (angle <= angleTolerance)
        {
            stableTimer += Time.deltaTime;
            if (stableTimer >= requiredStableSeconds)
            {
                CompleteChallenge();
            }
        }
        else
        {
            stableTimer = 0f;
        }

        RefreshUI();
    }

    protected override void OnCompleted()
    {
        if (completedEffect != null)
        {
            completedEffect.Play();
        }

        if (audioSource != null && completedClip != null)
        {
            audioSource.PlayOneShot(completedClip);
        }

        if (vibrateOnSuccess)
        {
            Handheld.Vibrate();
        }

        ResolveDialogueManager();
        if (dialogueManager != null && !string.IsNullOrWhiteSpace(completedDialogue))
        {
            dialogueManager.ShowDialogue(speakerName, completedDialogue);
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        string text;
        if (IsCompleted)
        {
            text = completedText;
        }
        else if (stableTimer > 0f)
        {
            text = string.Format(holdingText, stableTimer);
        }
        else
        {
            text = searchingText;
        }

        if (progressTMPText != null)
        {
            progressTMPText.text = text;
        }

        if (progressText != null)
        {
            progressText.text = text;
        }
    }

    private void ResolveDialogueManager()
    {
        if (dialogueManager != null)
        {
            return;
        }

        dialogueManager = DialogueManager.Instance != null
            ? DialogueManager.Instance
            : FindObjectOfType<DialogueManager>();
    }

    private void CacheTargetRenderers()
    {
        if (alignmentTarget == null)
        {
            targetRenderers = null;
            return;
        }

        targetRenderers = alignmentTarget.GetComponentsInChildren<Renderer>(true);
    }

    private void CreateAnamorphicTextIfNeeded()
    {
        if (!createAnamorphicText || alignmentTarget == null || anamorphicRoot != null)
        {
            return;
        }

        Vector3 viewDirection = expectedViewDirection.sqrMagnitude > 0f
            ? expectedViewDirection.normalized
            : Vector3.forward;
        Vector3 localUp = Mathf.Abs(Vector3.Dot(viewDirection, Vector3.up)) > 0.95f
            ? Vector3.forward
            : Vector3.up;
        Vector3 localRight = Vector3.Cross(localUp, viewDirection).normalized;
        localUp = Vector3.Cross(viewDirection, localRight).normalized;

        anamorphicRoot = new GameObject("AnamorphicText");
        anamorphicRoot.transform.SetParent(alignmentTarget, false);
        anamorphicRoot.transform.localPosition = anamorphicLocalOffset;
        anamorphicRoot.transform.localRotation = Quaternion.identity;
        anamorphicRoot.transform.localScale = Vector3.one;

        int partCount = anamorphicTextParts != null ? anamorphicTextParts.Length : 0;
        float centerIndex = (partCount - 1) * 0.5f;

        for (int i = 0; i < partCount; i++)
        {
            if (string.IsNullOrWhiteSpace(anamorphicTextParts[i]))
            {
                continue;
            }

            float offsetIndex = i - centerIndex;
            GameObject partObject = new GameObject($"AnamorphicTextPart_{i + 1}");
            partObject.transform.SetParent(anamorphicRoot.transform, false);
            partObject.transform.localPosition =
                localRight * (offsetIndex * anamorphicHorizontalSpacing) +
                viewDirection * (offsetIndex * anamorphicDepthSpacing);
            partObject.transform.localRotation = Quaternion.LookRotation(viewDirection, localUp);

            TextMeshPro text = partObject.AddComponent<TextMeshPro>();
            text.text = anamorphicTextParts[i];
            text.fontSize = anamorphicTextSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = anamorphicTextColor;
            text.enableWordWrapping = false;
            text.fontStyle = FontStyles.Bold;

            if (anamorphicFont != null)
            {
                text.font = anamorphicFont;
            }
        }

        SetAnamorphicTextVisible(false);
    }

    private void SetAnamorphicTextVisible(bool visible)
    {
        if (anamorphicRoot != null && anamorphicRoot.activeSelf != visible)
        {
            anamorphicRoot.SetActive(visible);
        }
    }

    private bool IsTargetVisible()
    {
        if (!showProgressOnlyWhenTargetVisible)
        {
            return true;
        }

        if (alignmentTarget == null || !alignmentTarget.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            CacheTargetRenderers();
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null &&
                targetRenderers[i].enabled &&
                targetRenderers[i].isVisible)
            {
                return true;
            }
        }

        return false;
    }
}
