using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ARBookAudioAutoBinder
{
    private const string AudioRootName = "ARBookAudio";
    private static readonly string[] AudioFolders = { "Assets/Audio", "Assets/Audios" };

    [MenuItem("ARBook/Tools/Auto Bind Audio Cues")]
    public static void AutoBindAudioCues()
    {
        AudioClip uiClick = LoadClip("默认UI点击音效");
        AudioClip challengeSuccess = LoadClip("挑战成功音效");
        AudioClip challengeFailure = LoadClip("挑战错误音效");
        AudioClip chapterEnd = LoadClip("章节结束音效");
        AudioClip defaultBgm = LoadClip("常驻bgm");
        AudioClip battleBgm = LoadClip("战斗bgm");

        GameObject audioRoot = FindOrCreateAudioRoot();
        ARBookAudioCue uiClickCue =
            FindOrCreateCue(audioRoot, "DefaultUIClick", uiClick, false, false, 0.85f);
        ARBookAudioCue challengeSuccessCue =
            FindOrCreateCue(audioRoot, "ChallengeSuccess", challengeSuccess, false, false, 1f);
        ARBookAudioCue challengeFailureCue =
            FindOrCreateCue(audioRoot, "ChallengeFailure", challengeFailure, false, false, 1f);
        ARBookAudioCue chapterEndCue =
            FindOrCreateCue(audioRoot, "ChapterEnd", chapterEnd, false, false, 1f);
        ARBookAudioCue defaultBgmCue =
            FindOrCreateCue(audioRoot, "DefaultBGM", defaultBgm, true, true, 0.45f);
        ARBookAudioCue battleBgmCue =
            FindOrCreateCue(audioRoot, "BattleBGM", battleBgm, true, false, 0.5f);

        int changed = 0;
        changed += BindButtons(uiClickCue);
        changed += BindChallenges(challengeSuccess, challengeFailure);
        changed += BindCollectibles(challengeSuccess);
        changed += BindObjectiveEvents(chapterEndCue);
        changed += BindMovementEvents(challengeSuccessCue, challengeFailureCue);
        changed += BindActivationEvents(challengeSuccessCue, chapterEndCue);
        changed += BindBattleBgm(defaultBgmCue, battleBgmCue);
        changed += BindPresentationBgm(defaultBgmCue, battleBgmCue);

        EditorUtility.SetDirty(audioRoot);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"ARBook audio auto binding complete. Updated {changed} bindings.");
    }

    private static int BindButtons(ARBookAudioCue uiClickCue)
    {
        if (uiClickCue == null)
        {
            return 0;
        }

        int changed = 0;
        Button[] buttons = Object.FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            Undo.RecordObject(button, "Bind UI click audio");
            changed += AddPersistentListener(button.onClick, uiClickCue, nameof(ARBookAudioCue.PlayOneShot));
            EditorUtility.SetDirty(button);
        }

        return changed;
    }

    private static int BindChallenges(AudioClip success, AudioClip failure)
    {
        int changed = 0;

        ARSequenceTapChallenge[] sequenceChallenges =
            Object.FindObjectsOfType<ARSequenceTapChallenge>(true);
        for (int i = 0; i < sequenceChallenges.Length; i++)
        {
            ARSequenceTapChallenge challenge = sequenceChallenges[i];
            Undo.RecordObject(challenge, "Bind sequence challenge audio");
            changed += SetClip(challenge.successClip, success, value => challenge.successClip = value);
            changed += SetClip(challenge.failureClip, failure, value => challenge.failureClip = value);
            changed += EnsureAudioSource(challenge.gameObject, challenge.audioSource, value => challenge.audioSource = value);
            EditorUtility.SetDirty(challenge);
        }

        ARViewAlignmentChallenge[] alignmentChallenges =
            Object.FindObjectsOfType<ARViewAlignmentChallenge>(true);
        for (int i = 0; i < alignmentChallenges.Length; i++)
        {
            ARViewAlignmentChallenge challenge = alignmentChallenges[i];
            Undo.RecordObject(challenge, "Bind alignment challenge audio");
            changed += SetClip(challenge.completedClip, success, value => challenge.completedClip = value);
            changed += EnsureAudioSource(challenge.gameObject, challenge.audioSource, value => challenge.audioSource = value);
            EditorUtility.SetDirty(challenge);
        }

        return changed;
    }

    private static int BindCollectibles(AudioClip collectClip)
    {
        int changed = 0;
        ARBookCollectible[] collectibles = Object.FindObjectsOfType<ARBookCollectible>(true);
        for (int i = 0; i < collectibles.Length; i++)
        {
            ARBookCollectible collectible = collectibles[i];
            Undo.RecordObject(collectible, "Bind collectible audio");
            changed += SetClip(collectible.collectClip, collectClip, value => collectible.collectClip = value);
            changed += EnsureAudioSource(collectible.gameObject, collectible.audioSource, value => collectible.audioSource = value);
            EditorUtility.SetDirty(collectible);
        }

        return changed;
    }

    private static int BindObjectiveEvents(ARBookAudioCue chapterEndCue)
    {
        int changed = 0;

        ARBookChapterObjectiveManager[] objectives =
            Object.FindObjectsOfType<ARBookChapterObjectiveManager>(true);
        for (int i = 0; i < objectives.Length; i++)
        {
            ARBookChapterObjectiveManager objective = objectives[i];
            Undo.RecordObject(objective, "Bind objective completion audio");
            changed += AddPersistentListener(objective.onObjectiveCompleted, chapterEndCue, nameof(ARBookAudioCue.PlayOneShot));
            EditorUtility.SetDirty(objective);
        }

        ARBookFinaleController[] finales = Object.FindObjectsOfType<ARBookFinaleController>(true);
        for (int i = 0; i < finales.Length; i++)
        {
            ARBookFinaleController finale = finales[i];
            Undo.RecordObject(finale, "Bind finale audio");
            changed += AddPersistentListener(finale.onFinaleCompleted, chapterEndCue, nameof(ARBookAudioCue.PlayOneShot));
            EditorUtility.SetDirty(finale);
        }

        return changed;
    }

    private static int BindMovementEvents(ARBookAudioCue successCue, ARBookAudioCue failureCue)
    {
        int changed = 0;
        ARBookConditionalMover[] movers = Object.FindObjectsOfType<ARBookConditionalMover>(true);
        for (int i = 0; i < movers.Length; i++)
        {
            ARBookConditionalMover mover = movers[i];
            Undo.RecordObject(mover, "Bind mover audio");
            changed += AddPersistentListener(mover.onMoveCompleted, successCue, nameof(ARBookAudioCue.PlayOneShot));
            changed += AddPersistentListener(mover.onMoveBlocked, failureCue, nameof(ARBookAudioCue.PlayOneShot));
            EditorUtility.SetDirty(mover);
        }

        return changed;
    }

    private static int BindActivationEvents(ARBookAudioCue stepCue, ARBookAudioCue completedCue)
    {
        int changed = 0;
        ARBookActivationSequence[] sequences =
            Object.FindObjectsOfType<ARBookActivationSequence>(true);
        for (int i = 0; i < sequences.Length; i++)
        {
            ARBookActivationSequence sequence = sequences[i];
            Undo.RecordObject(sequence, "Bind activation sequence audio");

            if (sequence.steps != null)
            {
                for (int stepIndex = 0; stepIndex < sequence.steps.Length; stepIndex++)
                {
                    ARBookActivationSequence.Step step = sequence.steps[stepIndex];
                    if (step != null)
                    {
                        changed += AddPersistentListener(step.onStep, stepCue, nameof(ARBookAudioCue.PlayOneShot));
                    }
                }
            }

            changed += AddPersistentListener(sequence.onSequenceCompleted, completedCue, nameof(ARBookAudioCue.PlayOneShot));
            EditorUtility.SetDirty(sequence);
        }

        return changed;
    }

    private static int BindBattleBgm(ARBookAudioCue defaultBgmCue, ARBookAudioCue battleBgmCue)
    {
        int changed = 0;
        ARBookBattleController[] battles = Object.FindObjectsOfType<ARBookBattleController>(true);
        for (int i = 0; i < battles.Length; i++)
        {
            ARBookBattleController battle = battles[i];
            Undo.RecordObject(battle, "Bind battle BGM events");
            changed += AddPersistentListener(battle.onBattleStarted, defaultBgmCue, nameof(ARBookAudioCue.Stop));
            changed += AddPersistentListener(battle.onBattleStarted, battleBgmCue, nameof(ARBookAudioCue.Play));
            changed += AddPersistentListener(battle.onPlayerVictory, battleBgmCue, nameof(ARBookAudioCue.Stop));
            changed += AddPersistentListener(battle.onPlayerVictory, defaultBgmCue, nameof(ARBookAudioCue.Play));
            changed += AddPersistentListener(battle.onPlayerDefeat, battleBgmCue, nameof(ARBookAudioCue.Stop));
            changed += AddPersistentListener(battle.onPlayerDefeat, defaultBgmCue, nameof(ARBookAudioCue.Play));
            changed += AddPersistentListener(battle.onBattleExited, battleBgmCue, nameof(ARBookAudioCue.Stop));
            changed += AddPersistentListener(battle.onBattleExited, defaultBgmCue, nameof(ARBookAudioCue.Play));
            EditorUtility.SetDirty(battle);
        }

        return changed;
    }

    private static int BindPresentationBgm(ARBookAudioCue defaultBgmCue, ARBookAudioCue battleBgmCue)
    {
        int changed = 0;
        ARBookPresentationSession[] sessions =
            Object.FindObjectsOfType<ARBookPresentationSession>(true);
        for (int i = 0; i < sessions.Length; i++)
        {
            ARBookPresentationSession session = sessions[i];
            Undo.RecordObject(session, "Bind presentation audio events");
            changed += AddPersistentListener(session.onPresentationEntered, defaultBgmCue, nameof(ARBookAudioCue.Stop));
            changed += AddPersistentListener(session.onPresentationExited, battleBgmCue, nameof(ARBookAudioCue.Stop));
            changed += AddPersistentListener(session.onPresentationExited, defaultBgmCue, nameof(ARBookAudioCue.Play));
            EditorUtility.SetDirty(session);
        }

        return changed;
    }

    private static GameObject FindOrCreateAudioRoot()
    {
        GameObject root = GameObject.Find(AudioRootName);
        if (root != null)
        {
            return root;
        }

        root = new GameObject(AudioRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create ARBook audio root");
        return root;
    }

    private static ARBookAudioCue FindOrCreateCue(
        GameObject root,
        string cueName,
        AudioClip clip,
        bool loop,
        bool playOnAwake,
        float volume)
    {
        Transform child = root.transform.Find(cueName);
        GameObject cueObject;
        if (child == null)
        {
            cueObject = new GameObject(cueName);
            Undo.RegisterCreatedObjectUndo(cueObject, $"Create audio cue {cueName}");
            cueObject.transform.SetParent(root.transform, false);
        }
        else
        {
            cueObject = child.gameObject;
        }

        ARBookAudioCue cue = cueObject.GetComponent<ARBookAudioCue>();
        if (cue == null)
        {
            cue = Undo.AddComponent<ARBookAudioCue>(cueObject);
        }

        AudioSource source = cueObject.GetComponent<AudioSource>();
        if (source == null)
        {
            source = Undo.AddComponent<AudioSource>(cueObject);
        }

        Undo.RecordObject(cue, $"Configure audio cue {cueName}");
        Undo.RecordObject(source, $"Configure audio source {cueName}");
        cue.audioSource = source;
        cue.clip = clip;
        cue.loop = loop;
        cue.playOnAwake = playOnAwake;
        cue.volume = volume;
        cue.restartIfPlaying = !loop;
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.volume = volume;
        source.spatialBlend = 0f;
        EditorUtility.SetDirty(cue);
        EditorUtility.SetDirty(source);
        return cue;
    }

    private static AudioClip LoadClip(string clipName)
    {
        string[] guids = AssetDatabase.FindAssets($"{clipName} t:AudioClip", AudioFolders);
        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning($"Audio clip not found: {clipName}");
            return null;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static int AddPersistentListener(UnityEvent unityEvent, Object target, string methodName)
    {
        if (unityEvent == null || target == null || string.IsNullOrWhiteSpace(methodName))
        {
            return 0;
        }

        for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
        {
            if (unityEvent.GetPersistentTarget(i) == target &&
                unityEvent.GetPersistentMethodName(i) == methodName)
            {
                return 0;
            }
        }

        ARBookAudioCue cue = target as ARBookAudioCue;
        if (cue == null)
        {
            return 0;
        }

        switch (methodName)
        {
            case nameof(ARBookAudioCue.Play):
                UnityEventTools.AddPersistentListener(unityEvent, cue.Play);
                break;
            case nameof(ARBookAudioCue.PlayOneShot):
                UnityEventTools.AddPersistentListener(unityEvent, cue.PlayOneShot);
                break;
            case nameof(ARBookAudioCue.Stop):
                UnityEventTools.AddPersistentListener(unityEvent, cue.Stop);
                break;
            default:
                return 0;
        }

        return 1;
    }

    private static int SetClip(AudioClip current, AudioClip target, System.Action<AudioClip> assign)
    {
        if (target == null || current == target)
        {
            return 0;
        }

        assign(target);
        return 1;
    }

    private static int EnsureAudioSource(GameObject owner, AudioSource current, System.Action<AudioSource> assign)
    {
        if (owner == null && current == null)
        {
            return 0;
        }

        AudioSource source = current;
        if (source == null)
        {
            source = owner.GetComponent<AudioSource>();
        }

        if (source == null)
        {
            source = Undo.AddComponent<AudioSource>(owner);
        }

        Undo.RecordObject(source, "Configure audio source");
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        EditorUtility.SetDirty(source);

        if (current == source)
        {
            return 0;
        }

        assign(source);
        return 1;
    }
}
