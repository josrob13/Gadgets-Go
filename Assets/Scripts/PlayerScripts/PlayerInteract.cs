using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactDistance; // Distance within which the player can interact with objects
    public KeyCode interactKey = KeyCode.E; // Key to press for interaction
    public KeyCode pauseKey = KeyCode.Escape; // Key to press for pausing the game
    public UIManager uiManager;
    public Fade fadeManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            checkInteractions();
        }

        if (Input.GetKeyDown(pauseKey))
        {
            uiManager.GamePause();
        }
    }

    private void checkInteractions()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactDistance);
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out NPCInteractable npcInteractable))
            {
                npcInteractable.Interact();
            }
        }
    }
}
