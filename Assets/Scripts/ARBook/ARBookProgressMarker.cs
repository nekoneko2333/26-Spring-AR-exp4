using UnityEngine;

public class ARBookProgressMarker : MonoBehaviour
{
    public string key = "ChapterFlag";
    public int value = 1;
    public bool saveImmediately = true;

    [ContextMenu("Set Progress Key")]
    public void SetProgressKey()
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning($"{name} has no PlayerPrefs key.");
            return;
        }

        PlayerPrefs.SetInt(key, value);
        if (saveImmediately)
        {
            PlayerPrefs.Save();
        }
    }

    [ContextMenu("Clear Progress Key")]
    public void ClearProgressKey()
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        PlayerPrefs.DeleteKey(key);
        if (saveImmediately)
        {
            PlayerPrefs.Save();
        }
    }

    [ContextMenu("Log Progress Key")]
    public void LogProgressKey()
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.Log($"{name}: key is empty.");
            return;
        }

        Debug.Log($"{name}: {key} = {PlayerPrefs.GetInt(key, 0)}");
    }
}
