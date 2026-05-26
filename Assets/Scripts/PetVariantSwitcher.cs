using UnityEngine;

public class PetVariantSwitcher : MonoBehaviour
{
    public GameObject[] pets;
    public int currentIndex;

    private void Start()
    {
        Show(currentIndex);
    }

    public void NextPet()
    {
        if (pets == null || pets.Length == 0)
        {
            return;
        }

        Show((currentIndex + 1) % pets.Length);
    }

    public void PreviousPet()
    {
        if (pets == null || pets.Length == 0)
        {
            return;
        }

        Show((currentIndex - 1 + pets.Length) % pets.Length);
    }

    public void Show(int index)
    {
        if (pets == null || pets.Length == 0)
        {
            return;
        }

        currentIndex = Mathf.Clamp(index, 0, pets.Length - 1);

        for (int i = 0; i < pets.Length; i++)
        {
            if (pets[i] != null)
            {
                pets[i].SetActive(i == currentIndex);
            }
        }
    }
}
