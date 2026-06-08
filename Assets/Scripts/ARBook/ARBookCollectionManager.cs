using UnityEngine;

public class ARBookCollectionManager : MonoBehaviour
{
    public bool clearOnStartForDebug;

    private const string CaptureKeyPrefix = "Captured_";
    private const string CapturedIdsKey = "CapturedIds";

    private void Start()
    {
        if (clearOnStartForDebug)
        {
            ClearCollectionForDebug();
        }
    }

    public void CaptureCreature(string captureId)
    {
        if (string.IsNullOrWhiteSpace(captureId))
        {
            Debug.LogWarning("CaptureCreature was called with an empty captureId.");
            return;
        }

        PlayerPrefs.SetInt(GetCaptureKey(captureId), 1);
        RegisterCapturedId(captureId);
        PlayerPrefs.Save();
    }

    public bool IsCaptured(string captureId)
    {
        if (string.IsNullOrWhiteSpace(captureId))
        {
            return false;
        }

        return PlayerPrefs.GetInt(GetCaptureKey(captureId), 0) == 1;
    }

    public void ClearCollectionForDebug()
    {
        string capturedIds = PlayerPrefs.GetString(CapturedIdsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(capturedIds))
        {
            string[] ids = capturedIds.Split(',');
            for (int i = 0; i < ids.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(ids[i]))
                {
                    PlayerPrefs.DeleteKey(GetCaptureKey(ids[i]));
                }
            }
        }

        PlayerPrefs.DeleteKey(CapturedIdsKey);
        PlayerPrefs.Save();
    }

    private string GetCaptureKey(string captureId)
    {
        return CaptureKeyPrefix + captureId;
    }

    private void RegisterCapturedId(string captureId)
    {
        string capturedIds = PlayerPrefs.GetString(CapturedIdsKey, string.Empty);
        string[] ids = capturedIds.Split(',');

        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] == captureId)
            {
                return;
            }
        }

        PlayerPrefs.SetString(
            CapturedIdsKey,
            string.IsNullOrWhiteSpace(capturedIds) ? captureId : capturedIds + "," + captureId);
    }
}
