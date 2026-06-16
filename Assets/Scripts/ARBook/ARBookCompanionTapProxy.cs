using UnityEngine;

public class ARBookCompanionTapProxy : MonoBehaviour
{
    public ARVirtualPetController petController;

    private void Reset()
    {
        petController = GetComponentInChildren<ARVirtualPetController>(true);
    }

    private void OnMouseDown()
    {
        if (petController != null)
        {
            petController.Pet();
        }
    }
}
