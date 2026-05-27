using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARInteractionManager : MonoBehaviour
{
    public float captureDelay = 0.6f;
    public bool returnCharacterAfterCapture;

    public ARTrackableEntity SelectedCharacter { get; private set; }

    private readonly HashSet<string> capturedPokemonIds = new HashSet<string>();
    private ARInteractionState state = ARInteractionState.SelectingCharacter;
    private Coroutine interactionRoutine;

    public void HandleTargetFound(ARTrackableEntity entity)
    {
        if (entity == null)
        {
            return;
        }

        if (entity.entityType == AREntityType.Pokemon)
        {
            if (capturedPokemonIds.Contains(entity.entityId))
            {
                entity.SetModelVisible(false);
                return;
            }

            if (SelectedCharacter != null && state != ARInteractionState.ApproachingPokemon && state != ARInteractionState.Capturing)
            {
                StartCaptureFlow(entity);
            }
        }
    }

    public void HandleTargetLost(ARTrackableEntity entity)
    {
        if (entity == null)
        {
            return;
        }

        if (entity.entityType == AREntityType.Character && state == ARInteractionState.SelectingCharacter)
        {
            SelectCharacter(entity);
        }
    }

    public void SelectCharacter(ARTrackableEntity character)
    {
        if (character == null || character.entityType != AREntityType.Character)
        {
            return;
        }

        SelectedCharacter = character;
        state = ARInteractionState.CharacterSelected;
        Debug.Log($"Selected character: {character.entityId}");
    }

    public void ClearSelection()
    {
        SelectedCharacter = null;
        state = ARInteractionState.SelectingCharacter;
    }

    public void ResetCapturedPokemon()
    {
        capturedPokemonIds.Clear();
    }

    private void StartCaptureFlow(ARTrackableEntity pokemon)
    {
        if (interactionRoutine != null)
        {
            StopCoroutine(interactionRoutine);
        }

        interactionRoutine = StartCoroutine(CaptureFlow(pokemon));
    }

    private IEnumerator CaptureFlow(ARTrackableEntity pokemon)
    {
        var character = SelectedCharacter;
        if (character == null || pokemon == null || pokemon.modelRoot == null)
        {
            yield break;
        }

        var characterAgent = character.characterAgent;
        var pokemonAgent = pokemon.pokemonAgent;
        if (characterAgent == null || pokemonAgent == null)
        {
            Debug.LogWarning("Capture flow needs ARCharacterAgent on the selected character and ARPokemonAgent on the Pokemon.");
            yield break;
        }

        state = ARInteractionState.ApproachingPokemon;
        yield return characterAgent.MoveToCaptureRange(pokemon.modelRoot);

        state = ARInteractionState.Capturing;
        characterAgent.PlayCapture();
        yield return new WaitForSeconds(captureDelay);

        pokemonAgent.Capture();
        pokemon.SetModelVisible(false);
        capturedPokemonIds.Add(pokemon.entityId);

        if (returnCharacterAfterCapture)
        {
            characterAgent.ResetToStart();
        }

        state = ARInteractionState.CharacterSelected;
        interactionRoutine = null;
        Debug.Log($"Captured Pokemon: {pokemon.entityId}");
    }

    private enum ARInteractionState
    {
        SelectingCharacter,
        CharacterSelected,
        ApproachingPokemon,
        Capturing
    }
}
