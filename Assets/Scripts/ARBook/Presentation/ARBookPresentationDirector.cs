using System;
using System.Collections.Generic;
using UnityEngine;

public class ARBookPresentationDirector : MonoBehaviour
{
    public static ARBookPresentationDirector Instance { get; private set; }

    [Header("Controllers")]
    public ARBookBattleController battleController;
    public ARBookCinematicDialogueController dialogueController;

    [Header("Battle Anchors")]
    public Transform battleOpponentAnchor;
    public Transform battlePlayerAnchor;

    [Header("Dialogue Anchors")]
    public Transform dialogueLeftAnchor;
    public Transform dialogueRightAnchor;

    [Header("Automatic Model Size")]
    [Min(0.1f)] public float battleOpponentHeight = 2.2f;
    [Min(0.1f)] public float battlePlayerHeight = 2.4f;
    [Min(0.1f)] public float dialogueActorHeight = 3.6f;
    public bool useUnlitBattleMaterials = true;
    public bool useUnlitDialogueMaterials = true;
    public float battleOpponentYawCorrection = 180f;
    public float battlePlayerYawCorrection = 150f;

    [Header("Animator Overrides")]
    public RuntimeAnimatorController playerPresentationController;

    private GameObject battleOpponentClone;
    private GameObject battlePlayerClone;
    private GameObject dialogueLeftClone;
    private GameObject dialogueRightClone;
    private Action captureOnVictory;
    private GameObject[] battleOriginalHideList;
    private GameObject[] dialogueOriginalHideList;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "场景中存在多个 ARBookPresentationDirector。",
                this);
        }

        Instance = this;
        RegisterCompletionCallbacks();
    }

    private void OnEnable()
    {
        RegisterCompletionCallbacks();
    }

    private void OnDisable()
    {
        UnregisterCompletionCallbacks();
    }

    public static bool TryBeginDialogue(ARBookInteractable interactable)
    {
        if (interactable == null ||
            interactable.GetComponentInChildren<Renderer>(true) == null)
        {
            return false;
        }

        ARBookPresentationDirector director = Instance != null
            ? Instance
            : FindObjectOfType<ARBookPresentationDirector>(true);
        return director != null && director.BeginDialogue(interactable);
    }

    public bool BeginDialogue(ARBookInteractable interactable)
    {
        if (interactable == null ||
            dialogueController == null ||
            dialogueController.IsRunning)
        {
            return false;
        }

        GameObject targetSource = ResolveInteractableModel(interactable);
        GameObject playerSource = ResolveActivePlayerModel();
        if (targetSource == null || playerSource == null)
        {
            return false;
        }

        CleanupDialogueModels();
        SetAnchorActive(dialogueLeftAnchor);
        SetAnchorActive(dialogueRightAnchor);
        dialogueLeftClone = CreatePresentationClone(
            targetSource,
            dialogueLeftAnchor,
            dialogueActorHeight,
            "DialogueTarget",
            dialogueController.session != null
                ? dialogueController.session.presentationCamera
                : null);
        dialogueRightClone = CreatePresentationClone(
            playerSource,
            dialogueRightAnchor,
            dialogueActorHeight,
            "DialoguePlayer",
            dialogueController.session != null
                ? dialogueController.session.presentationCamera
                : null);

        if (dialogueLeftClone == null || dialogueRightClone == null)
        {
            CleanupDialogueModels();
            return false;
        }

        if (useUnlitDialogueMaterials)
        {
            ConvertToUnlit(dialogueLeftClone);
            ConvertToUnlit(dialogueRightClone);
        }

        ARBookPresentationActor leftActor =
            EnsureActor(dialogueLeftClone);
        ARBookPresentationActor rightActor =
            EnsureActor(dialogueRightClone);
        ApplyAnimatorController(
            leftActor,
            interactable.presentationAnimatorController);
        ApplyAnimatorController(
            rightActor,
            playerPresentationController);
        dialogueController.leftActor = leftActor;
        dialogueController.rightActor = rightActor;
        dialogueController.lines = BuildDialogueLines(interactable);

        dialogueOriginalHideList = dialogueController.session != null
            ? dialogueController.session.hideDuringPresentation
            : null;
        AddModelsToSessionHideList(
            dialogueController.session,
            targetSource,
            playerSource);
        dialogueController.BeginDialogue();
        return true;
    }

    public bool BeginCaptureBattle(
        ARBookInteractable interactable,
        Action onVictory)
    {
        if (interactable == null ||
            battleController == null ||
            battleController.IsRunning ||
            battleController.IsBusy)
        {
            return false;
        }

        GameObject targetSource = ResolveInteractableModel(interactable);
        GameObject playerSource = ResolveActivePlayerModel();
        if (targetSource == null || playerSource == null)
        {
            return false;
        }

        CleanupBattleModels();
        SetAnchorActive(battleOpponentAnchor);
        SetAnchorActive(battlePlayerAnchor);
        battleOpponentClone = CreatePresentationClone(
            targetSource,
            battleOpponentAnchor,
            battleOpponentHeight,
            "BattleOpponent");
        battlePlayerClone = CreatePresentationClone(
            playerSource,
            battlePlayerAnchor,
            battlePlayerHeight,
            "BattlePlayer");

        if (battleOpponentClone == null || battlePlayerClone == null)
        {
            CleanupBattleModels();
            return false;
        }

        if (useUnlitBattleMaterials)
        {
            ApplyShadowlessMaterials(battleOpponentClone);
            ApplyShadowlessMaterials(battlePlayerClone);
        }

        ARBookPresentationActor opponentActor =
            EnsureActor(battleOpponentClone);
        ARBookPresentationActor playerActor =
            EnsureActor(battlePlayerClone);
        ApplyAnimatorController(
            opponentActor,
            interactable.presentationAnimatorController);
        ApplyAnimatorController(
            playerActor,
            playerPresentationController);

        battleController.enemy.actor = opponentActor;
        battleController.enemy.displayName = interactable.GetDisplayName();
        battleController.player.actor = playerActor;
        battleController.player.displayName = "训练家";
        battleController.cameraRig?.SetIntroTarget(
            battlePlayerClone.transform);
        battleController.SetIntroOpponent(battleOpponentClone);
        battleController.cameraRig?.SnapToEnd();
        ApplyBattleFacing(
            battleOpponentClone,
            battleController.session?.presentationCamera,
            battleOpponentYawCorrection);
        ApplyBattleFacing(
            battlePlayerClone,
            battleController.session?.presentationCamera,
            battlePlayerYawCorrection);
        captureOnVictory = onVictory;

        battleOriginalHideList = battleController.session != null
            ? battleController.session.hideDuringPresentation
            : null;
        AddModelsToSessionHideList(
            battleController.session,
            targetSource,
            playerSource);
        battleController.BeginBattle();
        return true;
    }

    private void HandleBattleFinished(bool playerWon)
    {
        Action victoryCallback = captureOnVictory;
        captureOnVictory = null;
        RestoreSessionHideList(
            battleController != null ? battleController.session : null,
            ref battleOriginalHideList);
        CleanupBattleModels();

        if (playerWon)
        {
            victoryCallback?.Invoke();
        }
    }

    private void HandleDialogueCompleted()
    {
        RestoreSessionHideList(
            dialogueController != null ? dialogueController.session : null,
            ref dialogueOriginalHideList);
        CleanupDialogueModels();
    }

    private void RegisterCompletionCallbacks()
    {
        if (battleController != null)
        {
            battleController.BattleFinished -= HandleBattleFinished;
            battleController.BattleFinished += HandleBattleFinished;
        }

        if (dialogueController != null)
        {
            dialogueController.onDialogueCompleted.RemoveListener(
                HandleDialogueCompleted);
            dialogueController.onDialogueCompleted.AddListener(
                HandleDialogueCompleted);
        }
    }

    private void UnregisterCompletionCallbacks()
    {
        if (battleController != null)
        {
            battleController.BattleFinished -= HandleBattleFinished;
        }

        if (dialogueController != null)
        {
            dialogueController.onDialogueCompleted.RemoveListener(
                HandleDialogueCompleted);
        }
    }

    private static GameObject ResolveInteractableModel(
        ARBookInteractable interactable)
    {
        if (interactable.presentationModelRoot != null)
        {
            return interactable.presentationModelRoot;
        }

        // The interacted object is the authoritative character root.
        return interactable.gameObject;
    }

    private static GameObject ResolveActivePlayerModel()
    {
        ARBookPlayerMover[] movers =
            FindObjectsOfType<ARBookPlayerMover>(true);
        for (int i = 0; i < movers.Length; i++)
        {
            ARBookPlayerMover mover = movers[i];
            if (mover == null ||
                !mover.enabled ||
                !mover.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (mover.visibleModel != null)
            {
                return mover.visibleModel;
            }

            Animator animator = FindVisibleAnimator(mover.gameObject);
            if (animator != null)
            {
                return animator.gameObject;
            }
        }

        return null;
    }

    private static GameObject CreatePresentationClone(
        GameObject source,
        Transform anchor,
        float desiredHeight,
        string cloneName,
        Camera facingCamera = null)
    {
        if (source == null || anchor == null)
        {
            return null;
        }

        GameObject clone = Instantiate(source, anchor, false);
        clone.name = cloneName;
        clone.SetActive(true);
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localRotation = Quaternion.identity;

        RemoveGameplayComponents(clone);
        SetLayerRecursively(clone, anchor.gameObject.layer);
        NormalizeModel(clone, anchor, desiredHeight);
        FaceCamera(clone, facingCamera);
        return clone;
    }

    private static Animator FindVisibleAnimator(GameObject root)
    {
        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator candidate = animators[i];
            if (!candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            Renderer[] renderers =
                candidate.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                if (renderers[j].enabled &&
                    renderers[j].gameObject.activeInHierarchy)
                {
                    return candidate;
                }
            }
        }

        return animators.Length > 0 ? animators[0] : null;
    }

    private static void FaceCamera(GameObject model, Camera camera)
    {
        if (model == null || camera == null)
        {
            return;
        }

        Vector3 direction = camera.transform.position - model.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            model.transform.rotation =
                Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private static void ConvertToUnlit(GameObject model)
    {
        Shader unlitShader = Shader.Find("Unlit/Texture");
        if (unlitShader == null)
        {
            return;
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Material[] sourceMaterials = renderer.materials;
            Material[] unlitMaterials = new Material[sourceMaterials.Length];
            for (int j = 0; j < sourceMaterials.Length; j++)
            {
                Material source = sourceMaterials[j];
                Material unlit = new Material(unlitShader)
                {
                    name = $"{source.name}_PresentationUnlit",
                    mainTexture = ResolveMainTexture(source),
                    color = ResolveMainColor(source)
                };
                unlitMaterials[j] = unlit;
            }

            renderer.materials = unlitMaterials;
        }
    }

    private static void ApplyShadowlessMaterials(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Material[] materials = renderer.materials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty("_EmissionColor"))
                {
                    Color baseColor = ResolveMainColor(material);
                    material.SetColor(
                        "_EmissionColor",
                        baseColor * 0.35f);
                    material.EnableKeyword("_EMISSION");
                }

                if (material.HasProperty("_EmissionMap"))
                {
                    Texture mainTexture = ResolveMainTexture(material);
                    if (mainTexture != null)
                    {
                        material.SetTexture("_EmissionMap", mainTexture);
                    }
                }
            }
        }
    }

    private static void ApplyBattleFacing(
        GameObject model,
        Camera camera,
        float yawCorrection)
    {
        if (model == null || camera == null)
        {
            return;
        }

        Vector3 direction = camera.transform.position - model.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        model.transform.rotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up) *
            Quaternion.Euler(0f, yawCorrection, 0f);
    }

    private static Texture ResolveMainTexture(Material material)
    {
        string[] textureProperties =
        {
            "_MainTex",
            "_BaseMap",
            "_BaseColorMap"
        };

        for (int i = 0; i < textureProperties.Length; i++)
        {
            if (material.HasProperty(textureProperties[i]))
            {
                Texture texture = material.GetTexture(textureProperties[i]);
                if (texture != null)
                {
                    return texture;
                }
            }
        }

        return null;
    }

    private static Color ResolveMainColor(Material material)
    {
        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        return Color.white;
    }

    private static void RemoveGameplayComponents(GameObject clone)
    {
        MonoBehaviour[] behaviours =
            clone.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null &&
                !(behaviours[i] is ARBookPresentationActor))
            {
                Destroy(behaviours[i]);
            }
        }

        Collider[] colliders =
            clone.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        Rigidbody[] rigidbodies =
            clone.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Destroy(rigidbodies[i]);
        }

        Camera[] cameras = clone.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Destroy(cameras[i]);
        }

        AudioListener[] listeners =
            clone.GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++)
        {
            Destroy(listeners[i]);
        }
    }

    private static void NormalizeModel(
        GameObject model,
        Transform anchor,
        float desiredHeight)
    {
        Renderer[] renderers =
            model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        if (bounds.size.y > 0.0001f)
        {
            float scale = desiredHeight / bounds.size.y;
            model.transform.localScale *= scale;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        Vector3 anchorPosition = anchor.position;
        Vector3 offset = new Vector3(
            anchorPosition.x - bounds.center.x,
            anchorPosition.y - bounds.min.y,
            anchorPosition.z - bounds.center.z);
        model.transform.position += offset;
    }

    private static ARBookPresentationActor EnsureActor(GameObject model)
    {
        ARBookPresentationActor actor =
            model.GetComponent<ARBookPresentationActor>();
        if (actor == null)
        {
            actor = model.AddComponent<ARBookPresentationActor>();
        }

        actor.animator = model.GetComponentInChildren<Animator>(true);
        return actor;
    }

    private static void ApplyAnimatorController(
        ARBookPresentationActor actor,
        RuntimeAnimatorController controller)
    {
        if (actor == null || actor.animator == null || controller == null)
        {
            return;
        }

        actor.animator.runtimeAnimatorController = controller;
        actor.animator.applyRootMotion = false;
    }

    private static ARBookCinematicDialogueController.DialogueLine[]
        BuildDialogueLines(ARBookInteractable interactable)
    {
        string[] sourceLines = interactable.ConsumeDialogueSequence();
        if (sourceLines == null || sourceLines.Length == 0)
        {
            sourceLines = new[] { string.Empty };
        }

        var lines =
            new ARBookCinematicDialogueController.DialogueLine[
                sourceLines.Length];
        for (int i = 0; i < sourceLines.Length; i++)
        {
            lines[i] =
                new ARBookCinematicDialogueController.DialogueLine
                {
                    speakerSide =
                        ARBookCinematicDialogueController.SpeakerSide.Left,
                    speakerName = interactable.GetDisplayName(),
                    text = sourceLines[i],
                    leftActorState = i == 0 ? "Greeting" : "Speak",
                    rightActorState = "Idle"
                };
        }

        return lines;
    }

    private static void AddModelsToSessionHideList(
        ARBookPresentationSession session,
        params GameObject[] models)
    {
        if (session == null)
        {
            return;
        }

        var targets = new List<GameObject>();
        if (session.hideDuringPresentation != null)
        {
            targets.AddRange(session.hideDuringPresentation);
        }

        for (int i = 0; i < models.Length; i++)
        {
            if (models[i] != null && !targets.Contains(models[i]))
            {
                targets.Add(models[i]);
            }
        }

        session.hideDuringPresentation = targets.ToArray();
    }

    private static void RestoreSessionHideList(
        ARBookPresentationSession session,
        ref GameObject[] originalList)
    {
        if (session != null)
        {
            session.hideDuringPresentation =
                originalList ?? Array.Empty<GameObject>();
        }

        originalList = null;
    }

    private void CleanupBattleModels()
    {
        DestroyClone(ref battleOpponentClone);
        DestroyClone(ref battlePlayerClone);
    }

    private void CleanupDialogueModels()
    {
        DestroyClone(ref dialogueLeftClone);
        DestroyClone(ref dialogueRightClone);
    }

    private static void DestroyClone(ref GameObject clone)
    {
        if (clone != null)
        {
            Destroy(clone);
            clone = null;
        }
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void SetAnchorActive(Transform anchor)
    {
        if (anchor != null)
        {
            anchor.gameObject.SetActive(true);
        }
    }
}
