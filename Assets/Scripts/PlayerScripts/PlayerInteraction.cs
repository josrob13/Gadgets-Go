using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactDistance = 2.0f;
    [Tooltip("Botón B del mando derecho = Two. (A = One)")]
    [SerializeField] private OVRInput.Button interactKey = OVRInput.Button.Two;
    [Tooltip("Mando con el que se interactúa. Botón B derecho => RTouch.")]
    [SerializeField] private OVRInput.Controller interactController = OVRInput.Controller.RTouch;
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
        // ── DIAGNÓSTICO TEMPORAL: confirmar que OVRInput legacy recibe pulsaciones ──
        if (OVRInput.GetDown(OVRInput.Button.One, interactController))
            Debug.Log("[Interact-DIAG] OVRInput detecta botón A (One) en el mando derecho.");
        if (OVRInput.GetDown(OVRInput.Button.Two, interactController))
            Debug.Log("[Interact-DIAG] OVRInput detecta botón B (Two) en el mando derecho.");
        // ───────────────────────────────────────────────────────────────────────────

        if (OVRInput.GetDown(interactKey, interactController))
        {
            Debug.Log($"[Interact] Botón de interacción pulsado ({interactKey} en {interactController}).");

            if (centerEyeAnchor == null)
                Debug.LogError("[Interact] centerEyeAnchor es NULL. El OverlapSphere se centrará en la raíz del rig (probablemente el suelo) en vez de tu cabeza — puede no alcanzar al NPC.");

            IInteractable interactable = GetInteractableObject(verbose: true);
            if (interactable != null)
            {
                Debug.Log($"[Interact] Interactuando con '{interactable.GetInteractText()}'. ¿MissionManager activo? {(MissionManager.Instance != null)} | ¿CameraManager activo? {(CameraManager.Instance != null)}");
                interactable.Interact(centerEyeAnchor); // <-- usa centerEyeAnchor en vez de transform
            }
            else
            {
                Debug.LogWarning($"[Interact] No se encontró ningún IInteractable dentro de {interactDistance}m desde {PlayerPosition}.");
            }
        }
    }

    public IInteractable GetInteractableObject(bool verbose = false)
    {
        List<IInteractable> interactables = new List<IInteractable>();

        // Usar PlayerPosition en vez de transform.position
        Collider[] colliders = Physics.OverlapSphere(PlayerPosition, interactDistance);
        foreach (Collider collider in colliders)
        {
            // GetComponentInParent: soporta colliders que están en un hijo (la malla)
            // mientras el NPCInteractable vive en el GameObject raíz del NPC.
            IInteractable interactable = collider.GetComponentInParent<IInteractable>();
            if (interactable != null && !interactables.Contains(interactable))
                interactables.Add(interactable);
        }

        if (verbose)
            Debug.Log($"[Interact-DIAG] OverlapSphere desde {PlayerPosition} r={interactDistance}m → {colliders.Length} colliders totales, {interactables.Count} IInteractable.");

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

        if (verbose)
            Debug.Log("[Interact-DIAG] Closest interactable: " + (closestInteractable != null ? closestInteractable.GetInteractText() : "None"));
        return closestInteractable;
    }
}