using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ARTrackableEntityBinder
{
    [MenuItem("Tools/AR Interaction/Bind Trackable Entity Agents")]
    public static void BindTrackableEntityAgents()
    {
        var changedCount = 0;
        var missingCount = 0;
        var entities = Object.FindObjectsOfType<ARTrackableEntity>(true);

        foreach (var entity in entities)
        {
            if (BindEntity(entity))
            {
                changedCount++;
            }
            else
            {
                missingCount++;
            }
        }

        Debug.Log($"Trackable entity agent binding finished. Changed: {changedCount}, missing: {missingCount}.");
    }

    [MenuItem("Tools/AR Interaction/Bind Selected Trackable Entity Agents")]
    public static void BindSelectedTrackableEntityAgents()
    {
        var changedCount = 0;
        var missingCount = 0;

        foreach (var selected in Selection.gameObjects)
        {
            var entities = selected.GetComponentsInChildren<ARTrackableEntity>(true);
            foreach (var entity in entities)
            {
                if (BindEntity(entity))
                {
                    changedCount++;
                }
                else
                {
                    missingCount++;
                }
            }
        }

        Debug.Log($"Selected trackable entity agent binding finished. Changed: {changedCount}, missing: {missingCount}.");
    }

    private static bool BindEntity(ARTrackableEntity entity)
    {
        if (entity == null)
        {
            return false;
        }

        var searchRoot = entity.modelRoot != null ? entity.modelRoot : GetOnlyChildOrSelf(entity.transform);
        var changed = false;
        var found = true;

        Undo.RecordObject(entity, "Bind Trackable Entity Agents");

        if (entity.modelRoot == null && searchRoot != null && searchRoot != entity.transform)
        {
            entity.modelRoot = searchRoot;
            changed = true;
        }

        if (entity.manager == null)
        {
            var manager = Object.FindObjectOfType<ARInteractionManager>();
            if (manager != null)
            {
                entity.manager = manager;
                changed = true;
            }
        }

        switch (entity.entityType)
        {
            case AREntityType.Character:
            {
                var agent = searchRoot != null ? searchRoot.GetComponentInChildren<ARCharacterAgent>(true) : null;
                if (agent == null)
                {
                    found = false;
                    Debug.LogWarning($"No ARCharacterAgent found under {GetHierarchyPath(entity.transform)}.");
                    break;
                }

                if (entity.characterAgent != agent)
                {
                    entity.characterAgent = agent;
                    changed = true;
                }
                break;
            }

            case AREntityType.Pokemon:
            {
                var agent = searchRoot != null ? searchRoot.GetComponentInChildren<ARPokemonAgent>(true) : null;
                if (agent == null)
                {
                    found = false;
                    Debug.LogWarning($"No ARPokemonAgent found under {GetHierarchyPath(entity.transform)}.");
                    break;
                }

                if (entity.pokemonAgent != agent)
                {
                    entity.pokemonAgent = agent;
                    changed = true;
                }
                break;
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(entity);
            MarkSceneDirty(entity.gameObject.scene);
        }

        return found;
    }

    private static Transform GetOnlyChildOrSelf(Transform transform)
    {
        return transform.childCount == 1 ? transform.GetChild(0) : transform;
    }

    private static void MarkSceneDirty(Scene scene)
    {
        if (scene.IsValid() && scene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = $"{transform.name}/{path}";
        }

        return path;
    }
}
