using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactDistance = 2.0f; // Distance within which the player can interact with objects
    [SerializeField] private KeyCode interactKey = KeyCode.E; // Key to press for interaction
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape; // Key to press for pausing the game
    [SerializeField] private KeyCode menuKey = KeyCode.Tab; // Key to press for pausing the game
    [SerializeField] private InteractiveMenu interactiveMenu;
    public UIManager uiManager;
    public Fade fade;

    public void KeyInteract()
    {
        if (Input.GetKeyDown(interactKey))
        {
            CheckInteractions();
        }

        if (Input.GetKeyDown(pauseKey))
        {
            uiManager.GamePause();
        }

        if (Input.GetKeyDown(menuKey))
        {
            // Show the interactive menu
        }
    }

    private void CheckInteractions()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactDistance);
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out NPCInteractable npcInteractable))
            {
                npcInteractable.Interact(transform);
            }
        }
    }
}
