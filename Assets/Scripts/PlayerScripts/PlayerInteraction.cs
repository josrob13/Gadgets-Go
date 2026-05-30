using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactDistance = 2.0f;
    [SerializeField] private OVRInput.Button interactKey = OVRInput.Button.One;
    [SerializeField] private OVRInput.Button menuKey = OVRInput.Button.Three;
    [SerializeField] private InteractiveMenu interactiveMenu;
    [SerializeField] private Transform centerEyeAnchor; // <-- AÑADIR ESTO
    public UIManager uiManager;

    private void Start()
    {
        // Si no se asigna manualmente, lo buscamos automáticamente
        if (centerEyeAnchor == null)
        {
            Debug.LogWarning("CenterEyeAnchor no asignado. Intentando encontrarlo automáticamente...");
        }

        Debug.Log("PlayerInteraction script started.");
        Debug.Log("CenterEyeAnchor: " + centerEyeAnchor?.name);
    }

    // Posición real del jugador en el mundo VR
    private Vector3 PlayerPosition => centerEyeAnchor != null ? centerEyeAnchor.position : transform.position;

    public void KeyInteract()
    {
        if (OVRInput.GetDown(interactKey))
        {
            Debug.Log("Interact key pressed.");
            IInteractable interactable = GetInteractableObject();
            if (interactable != null)
                interactable.Interact(centerEyeAnchor); // <-- usa centerEyeAnchor en vez de transform
        }
    }

    public IInteractable GetInteractableObject()
    {
        List<IInteractable> interactables = new List<IInteractable>();

        // Usar PlayerPosition en vez de transform.position
        Collider[] colliders = Physics.OverlapSphere(PlayerPosition, interactDistance);
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out IInteractable interactable))
                interactables.Add(interactable);
        }

        IInteractable closestInteractable = null;
        foreach (IInteractable interactable in interactables)
        {
            if (closestInteractable == null)
            {
                closestInteractable = interactable;
            }
            else
            {
                float currentDistance = Vector3.Distance(PlayerPosition, interactable.GetTransform().position);
                float closestDistance = Vector3.Distance(PlayerPosition, closestInteractable.GetTransform().position);
                if (currentDistance < closestDistance)
                    closestInteractable = interactable;
            }
        }

        Debug.Log("Closest interactable: " + (closestInteractable != null ? closestInteractable.GetInteractText() : "None"));
        return closestInteractable;
    }
}