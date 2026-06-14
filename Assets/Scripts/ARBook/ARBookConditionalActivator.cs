using UnityEngine;
using UnityEngine.Events;

public class ARBookConditionalActivator : MonoBehaviour
{
    [Header("Conditions")]
    public ARBookConditionGroup conditions = new ARBookConditionGroup();
    public ARBookCollectionManager collectionManager;
    public ARBookChapterProgress chapterProgress;

    [Header("Evaluation")]
    public bool evaluateOnStart = true;
    public bool evaluateOnEnable = true;
    public bool evaluateRepeatedly;
    public float evaluateInterval = 0.5f;
    public bool onlyFireEventsOnStateChange = true;

    [Header("Targets")]
    public GameObject[] activateWhenMet;
    public GameObject[] deactivateWhenMet;
    public GameObject[] activateWhenNotMet;
    public GameObject[] deactivateWhenNotMet;
    public Behaviour[] enableWhenMet;
    public Behaviour[] disableWhenMet;

    [Header("Events")]
    public UnityEvent onConditionsMet;
    public UnityEvent onConditionsNotMet;

    private bool hasLastState;
    private bool lastState;
    private float nextEvaluateTime;

    private void Start()
    {
        if (evaluateOnStart)
        {
            Evaluate();
        }
    }

    private void OnEnable()
    {
        if (evaluateOnEnable)
        {
            Evaluate();
        }
    }

    private void Update()
    {
        if (!evaluateRepeatedly || Time.time < nextEvaluateTime)
        {
            return;
        }

        Evaluate();
        nextEvaluateTime = Time.time + Mathf.Max(0.1f, evaluateInterval);
    }

    [ContextMenu("Evaluate Conditions")]
    public void Evaluate()
    {
        ResolveReferences();
        bool met = conditions == null ||
                   conditions.IsMet(collectionManager, chapterProgress);

        ApplyState(met);
    }

    [ContextMenu("Log Conditions")]
    public void LogConditions()
    {
        ResolveReferences();
        string debugText = conditions == null
            ? "No condition group."
            : conditions.GetDebugText(collectionManager, chapterProgress);
        Debug.Log($"{name}\n{debugText}");
    }

    private void ApplyState(bool met)
    {
        SetActiveAll(activateWhenMet, met);
        SetActiveAll(deactivateWhenMet, !met);
        SetActiveAll(activateWhenNotMet, !met);
        SetActiveAll(deactivateWhenNotMet, met);
        SetEnabledAll(enableWhenMet, met);
        SetEnabledAll(disableWhenMet, !met);

        bool shouldFire = !onlyFireEventsOnStateChange ||
                          !hasLastState ||
                          lastState != met;
        hasLastState = true;
        lastState = met;

        if (!shouldFire)
        {
            return;
        }

        if (met)
        {
            onConditionsMet?.Invoke();
        }
        else
        {
            onConditionsNotMet?.Invoke();
        }
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
    }

    private static void SetActiveAll(GameObject[] targets, bool active)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].SetActive(active);
            }
        }
    }

    private static void SetEnabledAll(Behaviour[] targets, bool enabled)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].enabled = enabled;
            }
        }
    }
}
