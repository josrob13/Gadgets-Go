using System.Collections.Generic;
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
            IInteractable interactable = GetInteractableObject();
            if (interactable != null)
                interactable.Interact(transform);
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

    public IInteractable GetInteractableObject()
    {
        List<IInteractable> interactables = new List<IInteractable>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactDistance);
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out IInteractable interactable))
                interactables.Add(interactable);
        }

        IInteractable closestInteractable = null;
        foreach (IInteractable interactable in interactables)
        {
            if (closestInteractable == null)
                closestInteractable = interactable;
            else
            {
                float currentDistance = Vector3.Distance(transform.position, interactable.GetTransform().position);
                float closestDistance = Vector3.Distance(transform.position, closestInteractable.GetTransform().position);
                if (currentDistance < closestDistance)
                {
                    closestInteractable = interactable;
                }
            }
        }

        return closestInteractable;
    }
}
