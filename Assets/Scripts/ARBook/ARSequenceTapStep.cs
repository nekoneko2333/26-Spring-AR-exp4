using UnityEngine;

public class ARSequenceTapStep : MonoBehaviour
{
    public ARSequenceTapChallenge challenge;
    [Min(0)] public int stepIndex;

    private void OnMouseDown()
    {
        Tap();
    }

    public void Tap()
    {
        ResolveChallenge();

        if (challenge != null)
        {
            challenge.TapStep(stepIndex);
        }
    }

    private void ResolveChallenge()
    {
        if (challenge != null)
        {
            return;
        }

        challenge = GetComponentInParent<ARSequenceTapChallenge>(true);
        if (challenge == null)
        {
            challenge = FindObjectOfType<ARSequenceTapChallenge>();
        }
    }
}
