using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ARBookPresentationSession : MonoBehaviour
{
    public Camera arCamera;
    public Camera presentationCamera;
    public GameObject presentationRoot;
    public ARBookFrozenBackground frozenBackground;
    public GameObject[] hideDuringPresentation;
    public UnityEvent onPresentationEntered;
    public UnityEvent onPresentationExited;

    public bool IsActive { get; private set; }

    private readonly Dictionary<GameObject, bool> previousStates =
        new Dictionary<GameObject, bool>();

    public IEnumerator Enter()
    {
        if (IsActive)
        {
            yield break;
        }

        CacheAndHideObjects();

        if (frozenBackground != null)
        {
            yield return frozenBackground.Capture();
        }

        if (presentationRoot != null)
        {
            presentationRoot.SetActive(true);
        }

        if (frozenBackground != null &&
            frozenBackground.backgroundRenderer != null)
        {
            frozenBackground.backgroundRenderer.gameObject.SetActive(true);
        }

        if (presentationCamera != null)
        {
            presentationCamera.gameObject.SetActive(true);
            presentationCamera.enabled = true;
        }

        if (arCamera != null)
        {
            arCamera.enabled = false;
        }

        IsActive = true;
        onPresentationEntered?.Invoke();
    }

    public void Exit()
    {
        if (!IsActive)
        {
            return;
        }

        if (presentationCamera != null)
        {
            presentationCamera.enabled = false;
        }

        if (presentationRoot != null)
        {
            presentationRoot.SetActive(false);
        }

        if (arCamera != null)
        {
            arCamera.enabled = true;
        }

        RestoreObjects();

        if (frozenBackground != null)
        {
            frozenBackground.Release();
        }

        IsActive = false;
        onPresentationExited?.Invoke();
    }

    private void CacheAndHideObjects()
    {
        previousStates.Clear();
        if (hideDuringPresentation == null)
        {
            return;
        }

        for (int i = 0; i < hideDuringPresentation.Length; i++)
        {
            GameObject target = hideDuringPresentation[i];
            if (target == null)
            {
                continue;
            }

            previousStates[target] = target.activeSelf;
            target.SetActive(false);
        }
    }

    private void RestoreObjects()
    {
        foreach (KeyValuePair<GameObject, bool> state in previousStates)
        {
            if (state.Key != null)
            {
                state.Key.SetActive(state.Value);
            }
        }

        previousStates.Clear();
    }
}
