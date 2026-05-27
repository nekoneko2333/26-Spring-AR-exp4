using System.Collections;
using UnityEngine;

public class ARTrackableEntity : MonoBehaviour
{
    public string entityId;
    public AREntityType entityType;
    public Transform modelRoot;
    public ARInteractionManager manager;
    public ARCharacterAgent characterAgent;
    public ARPokemonAgent pokemonAgent;
    public float lostConfirmDelay = 0.5f;

    public bool IsTracked { get; private set; }

    private Coroutine lostRoutine;

    private void Reset()
    {
        entityId = gameObject.name;
        modelRoot = transform.childCount == 1 ? transform.GetChild(0) : transform;
        characterAgent = GetComponentInChildren<ARCharacterAgent>(true);
        pokemonAgent = GetComponentInChildren<ARPokemonAgent>(true);
    }

    private void Awake()
    {
        if (manager == null)
        {
            manager = FindObjectOfType<ARInteractionManager>();
        }

        if (modelRoot == null)
        {
            modelRoot = transform.childCount == 1 ? transform.GetChild(0) : transform;
        }

        if (characterAgent == null)
        {
            characterAgent = GetComponentInChildren<ARCharacterAgent>(true);
        }

        if (pokemonAgent == null)
        {
            pokemonAgent = GetComponentInChildren<ARPokemonAgent>(true);
        }
    }

    public void NotifyTargetFound()
    {
        IsTracked = true;

        if (lostRoutine != null)
        {
            StopCoroutine(lostRoutine);
            lostRoutine = null;
        }

        if (pokemonAgent == null || !pokemonAgent.IsCaptured)
        {
            SetModelVisible(true);
        }

        if (manager != null)
        {
            manager.HandleTargetFound(this);
        }
    }

    public void NotifyTargetLost()
    {
        IsTracked = false;

        if (lostRoutine != null)
        {
            StopCoroutine(lostRoutine);
        }

        lostRoutine = StartCoroutine(ConfirmLost());
    }

    public void SetModelVisible(bool visible)
    {
        if (modelRoot != null)
        {
            modelRoot.gameObject.SetActive(visible);
        }
    }

    [ContextMenu("Simulate Target Found")]
    private void SimulateFound()
    {
        NotifyTargetFound();
    }

    [ContextMenu("Simulate Target Lost")]
    private void SimulateLost()
    {
        NotifyTargetLost();
    }

    private IEnumerator ConfirmLost()
    {
        yield return new WaitForSeconds(lostConfirmDelay);
        lostRoutine = null;

        if (manager != null)
        {
            manager.HandleTargetLost(this);
        }
    }
}
