using UnityEngine;

public class ARBookPresentationActor : MonoBehaviour
{
    public Animator animator;
    public string idleState = "Idle";
    public string entryState = "BattleEntry";
    public string attackState = "Attack";
    public string hitState = "Hit";
    public string victoryState = "Victory";
    public string defeatState = "Defeat";
    public string speakState = "Speak";
    public string greetingState = "Greeting";
    [Header("Animator Parameters")]
    public string battleEntryTrigger = "BattleEntryTrigger";
    public string captureSuccessTrigger = "CaptureSuccessTrigger";
    public string greetingTrigger = "GreetingTrigger";
    public string speakTrigger = "SpeakTrigger";
    public string reactionParameter = "Reaction";
    [Min(0f)] public float crossFadeDuration = 0.15f;

    private void Awake()
    {
        ResolveAnimator();
    }

    public void PlayIdle()
    {
        PlayState(idleState);
    }

    public void PlayEntry()
    {
        PlayTriggerOrState(battleEntryTrigger, entryState);
    }

    public void PlayAttack()
    {
        PlayState(attackState);
    }

    public void PlayHit()
    {
        PlayState(hitState);
    }

    public void PlayVictory()
    {
        PlayState(victoryState);
    }

    public void PlayDefeat()
    {
        PlayState(defeatState);
    }

    public void PlaySpeak()
    {
        PlayTriggerOrState(speakTrigger, speakState);
    }

    public void PlayGreeting()
    {
        PlayTriggerOrState(greetingTrigger, greetingState);
    }

    public void PlayCaptureSuccess()
    {
        PlayTriggerOrState(captureSuccessTrigger, "CaptureSuccess");
    }

    public void PlayReaction(int reactionIndex)
    {
        ResolveAnimator();
        if (animator == null)
        {
            return;
        }

        int clamped = Mathf.Clamp(reactionIndex, 1, 3);
        if (HasParameter(
                reactionParameter,
                AnimatorControllerParameterType.Int))
        {
            animator.SetInteger(reactionParameter, clamped);
            StartCoroutine(ResetReactionParameter());
            return;
        }

        PlayState($"Reaction_0{clamped}");
    }

    public void PlayState(string stateName)
    {
        ResolveAnimator();
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            Debug.LogWarning(
                $"{name} animator does not contain state '{stateName}'.",
                this);
            return;
        }

        animator.CrossFadeInFixedTime(
            stateHash,
            Mathf.Max(0f, crossFadeDuration),
            0,
            0f);
    }

    private void ResolveAnimator()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    private void PlayTriggerOrState(
        string triggerParameter,
        string fallbackState)
    {
        ResolveAnimator();
        if (animator != null &&
            HasParameter(
                triggerParameter,
                AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(triggerParameter);
            return;
        }

        PlayState(fallbackState);
    }

    private System.Collections.IEnumerator ResetReactionParameter()
    {
        yield return null;
        if (animator != null &&
            HasParameter(
                reactionParameter,
                AnimatorControllerParameterType.Int))
        {
            animator.SetInteger(reactionParameter, 0);
        }
    }

    private bool HasParameter(
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        if (animator == null ||
            string.IsNullOrWhiteSpace(parameterName) ||
            animator.runtimeAnimatorController == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName &&
                parameters[i].type == parameterType)
            {
                return true;
            }
        }

        return false;
    }
}
