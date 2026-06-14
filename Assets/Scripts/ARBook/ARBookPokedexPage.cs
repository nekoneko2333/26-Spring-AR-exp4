using UnityEngine;

public class ARBookPokedexPage : MonoBehaviour
{
    public ARBookCollectionManager collectionManager;
    public ARBookPokedexSlot[] slots;
    public bool refreshOnEnable = true;
    public bool refreshOnStart = true;
    public bool refreshWhileEnabled;
    public float refreshInterval = 0.5f;

    private float nextRefreshTime;

    private void Start()
    {
        ResolveCollectionManager();

        if (refreshOnStart)
        {
            Refresh();
        }
    }

    private void OnEnable()
    {
        if (refreshOnEnable)
        {
            ResolveCollectionManager();
            Refresh();
        }
    }

    private void Update()
    {
        if (!refreshWhileEnabled || Time.time < nextRefreshTime)
        {
            return;
        }

        ResolveCollectionManager();
        Refresh();
        nextRefreshTime = Time.time + Mathf.Max(0.1f, refreshInterval);
    }

    [ContextMenu("Refresh Pokedex Page")]
    public void Refresh()
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].Refresh(collectionManager);
            }
        }
    }

    [ContextMenu("Refresh And Log Pokedex Page")]
    public void RefreshAndLog()
    {
        ResolveCollectionManager();
        Refresh();

        if (slots == null)
        {
            Debug.Log($"{name}: slots is null.");
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                Debug.Log($"{name}: slot {i} is null.");
                continue;
            }

            Debug.Log(slots[i].GetDebugState(collectionManager));
        }
    }

    private void ResolveCollectionManager()
    {
        if (collectionManager == null)
        {
            collectionManager = FindObjectOfType<ARBookCollectionManager>();
        }
    }
}
