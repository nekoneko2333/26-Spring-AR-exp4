using UnityEngine;

public class ARBookAudioCue : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop;
    public bool playOnAwake;
    public bool restartIfPlaying = true;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        ResolveAudioSource();
        ApplySourceSettings();

        if (playOnAwake)
        {
            Play();
        }
    }

    public void Play()
    {
        if (clip == null)
        {
            return;
        }

        ResolveAudioSource();
        if (audioSource == null)
        {
            return;
        }

        ApplySourceSettings();
        audioSource.clip = clip;

        if (restartIfPlaying || !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    public void PlayOneShot()
    {
        if (clip == null)
        {
            return;
        }

        ResolveAudioSource();
        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void ResolveAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void ApplySourceSettings()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.loop = loop;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;
    }
}
