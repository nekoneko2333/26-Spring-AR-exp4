using UnityEngine;

public class ARPokemonAgent : MonoBehaviour
{
    public Animator animator;
    public ParticleSystem capturedEffect;
    public AudioSource audioSource;
    public AudioClip capturedClip;
    public string capturedTriggerName = "Captured";

    public bool IsCaptured { get; private set; }

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    public void Capture()
    {
        if (IsCaptured)
        {
            return;
        }

        IsCaptured = true;

        if (animator != null && !string.IsNullOrWhiteSpace(capturedTriggerName))
        {
            animator.SetTrigger(capturedTriggerName);
        }

        if (capturedEffect != null)
        {
            capturedEffect.Play();
        }

        if (audioSource != null && capturedClip != null)
        {
            audioSource.PlayOneShot(capturedClip);
        }

        gameObject.SetActive(false);
    }

    public void ResetCapture()
    {
        IsCaptured = false;
        gameObject.SetActive(true);
    }
}
