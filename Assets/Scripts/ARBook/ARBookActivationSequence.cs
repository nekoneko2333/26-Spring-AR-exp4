using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ARBookActivationSequence : MonoBehaviour
{
    [Serializable]
    public class Step
    {
        [Min(0f)] public float delay;
        public GameObject[] activate;
        public GameObject[] deactivate;
        public UnityEvent onStep;
    }

    public Step[] steps;
    public bool playOnlyOnce = true;
    public ARBookChallenge restoreFromChallenge;
    public bool restoreCompletedStateOnStart = true;
    public UnityEvent onSequenceCompleted;

    private Coroutine sequenceRoutine;
    private bool hasPlayed;

    private void Start()
    {
        if (restoreCompletedStateOnStart &&
            restoreFromChallenge != null &&
            restoreFromChallenge.IsCompleted)
        {
            ApplyFinalState();
        }
    }

    [ContextMenu("Play Sequence")]
    public void Play()
    {
        if ((playOnlyOnce && hasPlayed) || sequenceRoutine != null)
        {
            return;
        }

        sequenceRoutine = StartCoroutine(PlayRoutine());
    }

    [ContextMenu("Restart Sequence")]
    public void Restart()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = null;
        hasPlayed = false;
        Play();
    }

    [ContextMenu("Apply Final State")]
    public void ApplyFinalState()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        if (steps != null)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                Step step = steps[i];
                if (step == null)
                {
                    continue;
                }

                SetActive(step.activate, true);
                SetActive(step.deactivate, false);
            }
        }

        hasPlayed = true;
    }

    private IEnumerator PlayRoutine()
    {
        if (steps != null)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                Step step = steps[i];
                if (step == null)
                {
                    continue;
                }

                if (step.delay > 0f)
                {
                    yield return new WaitForSeconds(step.delay);
                }

                SetActive(step.activate, true);
                SetActive(step.deactivate, false);
                step.onStep?.Invoke();
            }
        }

        hasPlayed = true;
        sequenceRoutine = null;
        onSequenceCompleted?.Invoke();
    }

    private static void SetActive(GameObject[] targets, bool active)
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
}
