using UnityEngine;
using UnityEngine.Events;

public class ARBookCollectible : MonoBehaviour
{
    public string collectibleId = "Fragment_01";
    public ARBookChapterObjectiveManager objectiveManager;

    [Header("Collection")]
    public bool collectOnTap = true;
    public bool collectOnPlayerProximity;
    public float collectionRadius = 0.5f;
    public ARBookPlayerMover playerMover;
    public bool hideIfAlreadyCollected = true;

    [Header("Feedback")]
    public ParticleSystem collectEffect;
    public AudioSource audioSource;
    public AudioClip collectClip;
    public UnityEvent onCollected;

    private bool collected;

    private void Start()
    {
        ResolveObjectiveManager();

        if (objectiveManager != null && objectiveManager.IsCollected(collectibleId))
        {
            collected = true;
            if (hideIfAlreadyCollected)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (collected || !collectOnPlayerProximity)
        {
            return;
        }

        ResolvePlayerMover();
        if (playerMover != null &&
            playerMover.GetDistanceTo(transform.position) <= collectionRadius)
        {
            TryCollect();
        }
    }

    private void OnMouseDown()
    {
        if (collectOnTap)
        {
            TryCollect();
        }
    }

    public void TryCollect()
    {
        if (collected)
        {
            return;
        }

        ResolveObjectiveManager();
        if (objectiveManager == null)
        {
            Debug.LogWarning($"{name} cannot find an ARBookChapterObjectiveManager.");
            return;
        }

        if (!objectiveManager.Collect(collectibleId))
        {
            collected = objectiveManager.IsCollected(collectibleId);
            return;
        }

        collected = true;
        PlayFeedback();
        onCollected?.Invoke();

        if (hideIfAlreadyCollected)
        {
            gameObject.SetActive(false);
        }
    }

    private void PlayFeedback()
    {
        if (collectEffect != null)
        {
            collectEffect.transform.SetParent(null, true);
            collectEffect.Play();
        }

        if (audioSource != null && collectClip != null)
        {
            audioSource.PlayOneShot(collectClip);
        }
    }

    private void ResolveObjectiveManager()
    {
        if (objectiveManager != null)
        {
            return;
        }

        objectiveManager = GetComponentInParent<ARBookChapterObjectiveManager>(true);
        if (objectiveManager == null)
        {
            objectiveManager = FindObjectOfType<ARBookChapterObjectiveManager>();
        }
    }

    private void ResolvePlayerMover()
    {
        if (playerMover != null &&
            playerMover.isActiveAndEnabled &&
            playerMover.gameObject.activeInHierarchy)
        {
            return;
        }

        Transform parent = transform.parent;
        while (parent != null)
        {
            ARBookPlayerMover mover = parent.GetComponentInChildren<ARBookPlayerMover>(true);
            if (mover != null && mover.isActiveAndEnabled && mover.gameObject.activeInHierarchy)
            {
                playerMover = mover;
                return;
            }

            parent = parent.parent;
        }
    }
}
