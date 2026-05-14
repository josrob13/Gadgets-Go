using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Controla la posición y rotación del Canvas de diálogo en World Space.
/// Como hay un solo Canvas para todo el juego, este script se encarga de
/// teletransportarlo a la zona de la misión activa cuando se le solicita.
/// </summary>
public class DialogueCanvasController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private float distanceFromPlayer = 2.5f;
    [SerializeField] private float heightOffset = 0f;
    
    private Transform playerCameraTransform;

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        // Encontrar la cámara VR del jugador
        OVRCameraRig ovrCameraRig = FindObjectOfType<OVRCameraRig>();
        if (ovrCameraRig != null)
        {
            playerCameraTransform = ovrCameraRig.centerEyeAnchor;
        }
        else
        {
            playerCameraTransform = Camera.main?.transform;
        }

        if (playerCameraTransform == null)
        {
            Debug.LogError("[DialogueCanvasController] No se pudo encontrar la cámara del jugador");
        }

        // Asegurarse de que el Canvas está en WorldSpace
        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }

        // Ocultar el Canvas por defecto
        canvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Se llama una sola vez al iniciar el diálogo de una misión.
    /// Reubica el Canvas estático en la zona donde ocurre la misión.
    /// </summary>
    public void OnDialogueStart(Vector3? worldPosition = null, Quaternion? worldRotation = null)
    {
        canvas.gameObject.SetActive(true);

        if (worldPosition.HasValue && worldRotation.HasValue)
        {
            canvas.transform.position = worldPosition.Value;
            canvas.transform.rotation = worldRotation.Value;
        }
        else if (playerCameraTransform != null)
        {
            Vector3 forwardDirection = playerCameraTransform.forward;
            forwardDirection.y = 0;
            forwardDirection.Normalize();

            Vector3 newPosition = playerCameraTransform.position 
                + forwardDirection * distanceFromPlayer 
                + Vector3.up * heightOffset;

            canvas.transform.position = newPosition;

            Vector3 directionAwayFromPlayer = newPosition - playerCameraTransform.position;
            directionAwayFromPlayer.y = 0;
            canvas.transform.rotation = Quaternion.LookRotation(-directionAwayFromPlayer.normalized, Vector3.up);
        }
    }

    /// <summary>
    /// Desactiva el Canvas cuando termina un diálogo.
    /// </summary>
    public void OnDialogueEnd()
    {
        canvas.gameObject.SetActive(false);
        Debug.Log("[DialogueCanvasController] Diálogo terminado. Canvas oculto.");
    }
}
