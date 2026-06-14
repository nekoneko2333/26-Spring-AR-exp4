using UnityEditor;
using UnityEngine;

public static class ARBookPokedexSlotAutoFillTool
{
    [MenuItem("Tools/AR Book/Pokedex/Auto Fill Selected Slots")]
    public static void AutoFillSelectedSlots()
    {
        int updatedCount = 0;

        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            ARBookPokedexSlot[] slots =
                selectedObject.GetComponentsInChildren<ARBookPokedexSlot>(true);

            for (int i = 0; i < slots.Length; i++)
            {
                if (AutoFillSlot(slots[i]))
                {
                    updatedCount++;
                }
            }
        }

        Debug.Log($"Auto-filled {updatedCount} Pokedex slot(s).");
    }

    [MenuItem("Tools/AR Book/Pokedex/Auto Fill Selected Slots", true)]
    public static bool CanAutoFillSelectedSlots()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    private static bool AutoFillSlot(ARBookPokedexSlot slot)
    {
        if (slot == null)
        {
            return false;
        }

        string unlockId = GetUnlockId(slot.name);
        if (string.IsNullOrWhiteSpace(unlockId))
        {
            Debug.LogWarning($"Skipped {slot.name}: expected name format Slots_0x_id.");
            return false;
        }

        Undo.RecordObject(slot, "Auto Fill Pokedex Slot");

        slot.unlockMode = ARBookPokedexSlot.UnlockMode.CapturedCreature;
        slot.unlockId = unlockId;
        slot.unlockedRoot = slot.transform.childCount > 0
            ? slot.transform.GetChild(0).gameObject
            : null;
        slot.lockedRoot = null;
        slot.keepSlotVisibleWhenLocked = false;

        EditorUtility.SetDirty(slot);
        return true;
    }

    private static string GetUnlockId(string objectName)
    {
        string[] parts = objectName.Split('_');
        if (parts.Length < 3)
        {
            return string.Empty;
        }

        return string.Join("_", parts, 2, parts.Length - 2);
    }
}
