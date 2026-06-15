using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ARBookCinematicDialogueController : MonoBehaviour
{
    public enum SpeakerSide
    {
        Left,
        Right
    }

    [Serializable]
    public class DialogueLine
    {
        public SpeakerSide speakerSide;
        public string speakerName;
        [TextArea(2, 5)] public string text;
        public string leftActorState;
        public string rightActorState;
    }

    public ARBookPresentationSession session;
    public ARBookPresentationActor leftActor;
    public ARBookPresentationActor rightActor;
    public DialogueLine[] lines;

    [Header("UI")]
    public GameObject dialogueUIRoot;
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;
    public Image leftSpeakerHighlight;
    public Image rightSpeakerHighlight;
    public Color activeSpeakerColor = Color.white;
    public Color inactiveSpeakerColor = new Color(1f, 1f, 1f, 0.45f);

    [Header("Random Listener Reactions")]
    [Min(1)] public int minimumLinesBeforeReaction = 1;
    [Min(1)] public int maximumLinesBeforeReaction = 2;

    [Header("Events")]
    public UnityEvent onDialogueStarted;
    public UnityEvent onDialogueCompleted;

    public bool IsRunning { get; private set; }

    private int currentLineIndex = -1;
    private Coroutine startRoutine;
    private int linesUntilReaction;
    private int lastReactionIndex;

    [ContextMenu("Begin Dialogue")]
    public void BeginDialogue()
    {
        if (IsRunning || startRoutine != null)
        {
            return;
        }

        startRoutine = StartCoroutine(BeginDialogueRoutine());
    }

    public void ContinueDialogue()
    {
        if (!IsRunning)
        {
            return;
        }

        currentLineIndex++;
        if (lines == null || currentLineIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowLine(lines[currentLineIndex]);
    }

    public void EndDialogue()
    {
        if (!IsRunning && startRoutine == null)
        {
            return;
        }

        if (dialogueUIRoot != null)
        {
            dialogueUIRoot.SetActive(false);
        }

        leftActor?.PlayIdle();
        rightActor?.PlayIdle();
        session?.Exit();

        IsRunning = false;
        startRoutine = null;
        currentLineIndex = -1;
        onDialogueCompleted?.Invoke();
    }

    private IEnumerator BeginDialogueRoutine()
    {
        if (dialogueUIRoot != null)
        {
            dialogueUIRoot.SetActive(false);
        }

        if (session != null)
        {
            yield return session.Enter();
        }

        if (dialogueUIRoot != null)
        {
            dialogueUIRoot.SetActive(true);
        }

        IsRunning = true;
        currentLineIndex = -1;
        ScheduleNextReaction();
        startRoutine = null;
        onDialogueStarted?.Invoke();
        ContinueDialogue();
    }

    private void ShowLine(DialogueLine line)
    {
        if (line == null)
        {
            return;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
        }

        if (dialogueText != null)
        {
            dialogueText.text = line.text;
        }

        if (!string.IsNullOrWhiteSpace(line.leftActorState))
        {
            leftActor?.PlayState(line.leftActorState);
        }

        if (!string.IsNullOrWhiteSpace(line.rightActorState))
        {
            rightActor?.PlayState(line.rightActorState);
        }

        bool leftSpeaking = line.speakerSide == SpeakerSide.Left;
        linesUntilReaction--;
        if (linesUntilReaction <= 0)
        {
            int reaction = GetNextReaction();
            if (leftSpeaking)
            {
                rightActor?.PlayReaction(reaction);
            }
            else
            {
                leftActor?.PlayReaction(reaction);
            }

            ScheduleNextReaction();
        }

        if (leftSpeakerHighlight != null)
        {
            leftSpeakerHighlight.color =
                leftSpeaking ? activeSpeakerColor : inactiveSpeakerColor;
        }

        if (rightSpeakerHighlight != null)
        {
            rightSpeakerHighlight.color =
                leftSpeaking ? inactiveSpeakerColor : activeSpeakerColor;
        }
    }

    private void ScheduleNextReaction()
    {
        int minimum = Mathf.Max(1, minimumLinesBeforeReaction);
        int maximum = Mathf.Max(minimum, maximumLinesBeforeReaction);
        linesUntilReaction =
            UnityEngine.Random.Range(minimum, maximum + 1);
    }

    private int GetNextReaction()
    {
        int reaction = UnityEngine.Random.Range(1, 4);
        if (reaction == lastReactionIndex)
        {
            reaction = reaction % 3 + 1;
        }

        lastReactionIndex = reaction;
        return reaction;
    }
}
