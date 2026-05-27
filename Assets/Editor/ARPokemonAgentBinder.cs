using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ARPokemonAgentBinder
{
    [MenuItem("Tools/AR Interaction/Bind Pokemon Agent Animators")]
    public static void BindPokemonAgentAnimators()
    {
        var changedCount = 0;
        var missingCount = 0;
        var agents = Object.FindObjectsOfType<ARPokemonAgent>(true);

        foreach (var agent in agents)
        {
            if (agent.animator != null)
            {
                continue;
            }

            var animator = agent.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                missingCount++;
                Debug.LogWarning($"No child Animator found for ARPokemonAgent on {GetHierarchyPath(agent.transform)}.");
                continue;
            }

            Undo.RecordObject(agent, "Bind Pokemon Agent Animator");
            agent.animator = animator;
            EditorUtility.SetDirty(agent);
            MarkSceneDirty(agent.gameObject.scene);
            changedCount++;
        }

        Debug.Log($"Pokemon animator binding finished. Bound: {changedCount}, missing Animator: {missingCount}.");
    }

    [MenuItem("Tools/AR Interaction/Bind Selected Pokemon Agent Animators")]
    public static void BindSelectedPokemonAgentAnimators()
    {
        var changedCount = 0;
        var missingCount = 0;

        foreach (var selected in Selection.gameObjects)
        {
            var agents = selected.GetComponentsInChildren<ARPokemonAgent>(true);
            foreach (var agent in agents)
            {
                if (agent.animator != null)
                {
                    continue;
                }

                var animator = agent.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    missingCount++;
                    Debug.LogWarning($"No child Animator found for selected ARPokemonAgent on {GetHierarchyPath(agent.transform)}.");
                    continue;
                }

                Undo.RecordObject(agent, "Bind Selected Pokemon Agent Animator");
                agent.animator = animator;
                EditorUtility.SetDirty(agent);
                MarkSceneDirty(agent.gameObject.scene);
                changedCount++;
            }
        }

        Debug.Log($"Selected Pokemon animator binding finished. Bound: {changedCount}, missing Animator: {missingCount}.");
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
