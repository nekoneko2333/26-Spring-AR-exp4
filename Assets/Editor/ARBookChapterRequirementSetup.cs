using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ARBookChapterRequirementSetup
{
    [MenuItem("ARBook/Tools/Fill Chapter Capture Requirements")]
    public static void FillChapterCaptureRequirements()
    {
        ARBookChapterCompletionTrigger[] triggers =
            Object.FindObjectsOfType<ARBookChapterCompletionTrigger>(true);
        ARBookInteractable[] interactables =
            Object.FindObjectsOfType<ARBookInteractable>(true);
        ARBookQuestTracker[] trackers =
            Object.FindObjectsOfType<ARBookQuestTracker>(true);

        int changed = 0;
        for (int i = 0; i < triggers.Length; i++)
        {
            ARBookChapterCompletionTrigger trigger = triggers[i];
            if (trigger == null)
            {
                continue;
            }

            List<string> ids = CollectCaptureIdsForTrigger(
                trigger,
                interactables,
                trackers);
            if (ids.Count == 0)
            {
                Debug.LogWarning(
                    $"章节 {trigger.chapterId} 没有自动找到可收服宝可梦 ID，保留原配置。",
                    trigger);
                continue;
            }

            Undo.RecordObject(trigger, "Fill Chapter Capture Requirements");
            trigger.requireAllChapterCaptures = true;
            trigger.requiredChapterCaptureIds = ids.ToArray();
            trigger.requiredCaptureId = string.Empty;

            EditorUtility.SetDirty(trigger);
            EditorSceneManager.MarkSceneDirty(trigger.gameObject.scene);
            changed++;
            Debug.Log(
                $"章节 {trigger.chapterId} 通关收服需求：{string.Join(", ", ids)}",
                trigger);
        }

        Debug.Log($"章节通关宝可梦 ID 绑定完成：更新 {changed} 个 CompletionTrigger。");
    }

    private static List<string> CollectCaptureIdsForTrigger(
        ARBookChapterCompletionTrigger trigger,
        ARBookInteractable[] interactables,
        ARBookQuestTracker[] trackers)
    {
        List<string> ids = new List<string>();
        Transform triggerRoot = FindTargetRoot(trigger.transform);

        for (int i = 0; i < interactables.Length; i++)
        {
            ARBookInteractable interactable = interactables[i];
            if (interactable == null ||
                !interactable.canBeCaptured ||
                string.IsNullOrWhiteSpace(interactable.captureId))
            {
                continue;
            }

            Transform interactableRoot = FindTargetRoot(interactable.transform);
            if (triggerRoot != null &&
                interactableRoot != null &&
                interactableRoot == triggerRoot)
            {
                AddUnique(ids, interactable.captureId);
            }
        }

        if (ids.Count > 0)
        {
            return ids;
        }

        for (int i = 0; i < trackers.Length; i++)
        {
            ARBookQuestTracker tracker = trackers[i];
            if (tracker == null || tracker.chapterId != trigger.chapterId)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(tracker.requiredCaptureId))
            {
                AddUnique(ids, tracker.requiredCaptureId);
            }

            if (tracker.creature != null &&
                tracker.creature.canBeCaptured &&
                !string.IsNullOrWhiteSpace(tracker.creature.captureId))
            {
                AddUnique(ids, tracker.creature.captureId);
            }
        }

        return ids;
    }

    private static Transform FindTargetRoot(Transform transform)
    {
        Transform current = transform;
        Transform best = null;
        while (current != null)
        {
            if (HasComponentNamed(current, "ObserverBehaviour") ||
                HasComponentNamed(current, "ImageTargetBehaviour") ||
                HasComponentNamed(current, "ARBookChapterRoot"))
            {
                best = current;
            }

            current = current.parent;
        }

        return best != null ? best : transform.root;
    }

    private static bool HasComponentNamed(Transform transform, string typeName)
    {
        Component[] components = transform.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component != null &&
                component.GetType().Name == typeName)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddUnique(List<string> ids, string id)
    {
        if (!ids.Contains(id))
        {
            ids.Add(id);
        }
    }
}
