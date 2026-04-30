using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Controla la posición y rotación del Canvas de diálogo para que esté fijo
/// en una posición relativa al jugador durante el diálogo, sin seguir la cámara.
/// </summary>
public class DialogueCanvasController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private float distanceFromPlayer = 2.5f;
    [SerializeField] private float heightOffset = 0f;
    
    private Vector3 fixedCanvasPosition;
    private Quaternion fixedCanvasRotation;
    private bool isDialogueActive = false;
    private Transform playerCameraTransform;

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        // Encontrar la cámara VR del jugador
        // Primero intentamos obtenerla de OVRCameraRig
        OVRCameraRig ovrCameraRig = FindObjectOfType<OVRCameraRig>();
        if (ovrCameraRig != null)
        {
            playerCameraTransform = ovrCameraRig.centerEyeAnchor;
        }
        else
        {
            // Si no hay OVRCameraRig, intentamos con la cámara principal
            playerCameraTransform = Camera.main?.transform;
        }

        if (playerCameraTransform == null)
        {
            Debug.LogError("[DialogueCanvasController] No se pudo encontrar la cámara del jugador");
        }

        // Asegurarse de que el Canvas está en WorldSpace
        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogWarning("[DialogueCanvasController] El Canvas no está en modo WorldSpace. Cambiando a WorldSpace.");
            canvas.renderMode = RenderMode.WorldSpace;
        }
    }

    /// <summary>
    /// Activa el control del Canvas cuando empieza un diálogo
    /// </summary>
    public void OnDialogueStart()
    {
        isDialogueActive = true;
        
        if (playerCameraTransform == null)
            return;

        // Calcular posición frente al jugador
        Vector3 forwardDirection = playerCameraTransform.forward;
        Vector3 upDirection = playerCameraTransform.up;
        
        fixedCanvasPosition = playerCameraTransform.position + forwardDirection * distanceFromPlayer + upDirection * heightOffset;
        
        // Hacer que el Canvas mire hacia el jugador (pero invertido, para que el jugador vea el contenido)
        fixedCanvasRotation = Quaternion.LookRotation(playerCameraTransform.position - fixedCanvasPosition, Vector3.up);
        
        // Aplicar posición y rotación
        canvas.transform.position = fixedCanvasPosition;
        canvas.transform.rotation = fixedCanvasRotation;

        Debug.Log("[DialogueCanvasController] Diálogo iniciado. Canvas fijado en posición.");
    }

    /// <summary>
    /// Desactiva el control del Canvas cuando termina un diálogo
    /// </summary>
    public void OnDialogueEnd()
    {
        isDialogueActive = false;
        Debug.Log("[DialogueCanvasController] Diálogo terminado. Canvas liberado.");
    }

    private void LateUpdate()
    {
        // Si el diálogo está activo, asegurar que el Canvas permanezca en la posición fija
        if (isDialogueActive && canvas != null)
        {
            canvas.transform.position = fixedCanvasPosition;
            canvas.transform.rotation = fixedCanvasRotation;
        }
    }

    /// <summary>
    /// Permite ajustar la distancia del Canvas desde el jugador
    /// </summary>
    public void SetDistanceFromPlayer(float distance)
    {
        distanceFromPlayer = distance;
    }

    /// <summary>
    /// Permite ajustar el offset de altura del Canvas
    /// </summary>
    public void SetHeightOffset(float offset)
    {
        heightOffset = offset;
    }
}
