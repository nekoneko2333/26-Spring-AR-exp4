using UnityEngine;
using UnityEngine.Events;

public abstract class ARBookChallenge : MonoBehaviour
{
    public int chapterId = 1;
    public string challengeId = "Main";
    public bool saveCompletion = true;
    public bool clearOnStartForDebug;
    public UnityEvent onChallengeCompleted;

    private bool completedThisSession;

    public bool IsCompleted
    {
        get
        {
            if (!saveCompletion)
            {
                return completedThisSession;
            }

            return completedThisSession ||
                   PlayerPrefs.GetInt(GetCompletionKey(), 0) == 1;
        }
    }

    protected virtual void Start()
    {
        if (clearOnStartForDebug)
        {
            ClearCompletion();
        }
    }

    public virtual void ClearCompletion()
    {
        completedThisSession = false;
        PlayerPrefs.DeleteKey(GetCompletionKey());
        PlayerPrefs.Save();
    }

    protected void CompleteChallenge()
    {
        if (IsCompleted)
        {
            return;
        }

        completedThisSession = true;

        if (saveCompletion)
        {
            PlayerPrefs.SetInt(GetCompletionKey(), 1);
            PlayerPrefs.Save();
        }

        onChallengeCompleted?.Invoke();
        OnCompleted();
    }

    protected virtual void OnCompleted()
    {
    }

    private string GetCompletionKey()
    {
        return $"ChallengeCompleted_{chapterId}_{challengeId}";
    }
}
