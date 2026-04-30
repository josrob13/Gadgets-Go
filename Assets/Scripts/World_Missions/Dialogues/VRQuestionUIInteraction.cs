using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Maneja la interacción VR con los botones de preguntas usando gestos de pellizcar.
/// Detecta cuándo el jugador hace el gesto de "pellizcar" (trigger de mano) 
/// y dispara el evento onClick del botón que está mirando.
/// </summary>
public class VRQuestionUIInteraction : MonoBehaviour
{
    [Header("VR Settings")]
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch; // Mano derecha
    [SerializeField] private OVRInput.Button pinchGestureButton = OVRInput.Button.PrimaryHandTrigger;
    
    [Header("Raycast Settings")]
    [SerializeField] private float raycastDistance = 1000f;
    [SerializeField] private LayerMask uiLayerMask = LayerMask.GetMask("UI");
    
    private OVRCameraRig ovrCameraRig;
    private Transform centerEyeAnchor;
    private GraphicRaycaster graphicRaycaster;
    private Canvas targetCanvas;
    private bool previousPinchState = false;

    private void Start()
    {
        // Obtener referencias necesarias
        ovrCameraRig = FindObjectOfType<OVRCameraRig>();
        if (ovrCameraRig != null)
        {
            centerEyeAnchor = ovrCameraRig.centerEyeAnchor;
        }
        else
        {
            Debug.LogWarning("[VRQuestionUIInteraction] No se encontró OVRCameraRig");
            centerEyeAnchor = Camera.main?.transform;
        }

        // Buscar el Canvas de preguntas
        targetCanvas = GetComponentInParent<Canvas>();
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();

        if (targetCanvas != null)
        {
            graphicRaycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        }

        if (centerEyeAnchor == null)
        {
            Debug.LogError("[VRQuestionUIInteraction] No se pudo encontrar la cámara del jugador");
            enabled = false;
        }
    }

    private void Update()
    {
        if (centerEyeAnchor == null || graphicRaycaster == null)
            return;

        // Detectar el gesto de pellizcar (trigger presionado)
        bool currentPinchState = OVRInput.Get(pinchGestureButton, controller);

        // Si el trigger fue presionado en este frame
        if (currentPinchState && !previousPinchState)
        {
            Debug.Log("[VRQuestionUIInteraction] Gesto de pellizcar detectado");
            HandlePinchGesture();
        }

        previousPinchState = currentPinchState;
    }

    /// <summary>
    /// Maneja el gesto de pellizcar detectando qué botón está bajo el cursor del jugador
    /// </summary>
    private void HandlePinchGesture()
    {
        // Raycast desde el centro del ojo del jugador
        Ray ray = new Ray(centerEyeAnchor.position, centerEyeAnchor.forward);

        // Intentar detectar un botón UI bajo el rayo
        GraphicRaycastResult result = PerformGraphicRaycast(ray);

        if (result.gameObject != null)
        {
            Button button = result.gameObject.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                Debug.Log($"[VRQuestionUIInteraction] Botón presionado: {button.gameObject.name}");
                button.OnSubmit(null);
            }
        }
    }

    /// <summary>
    /// Realiza un raycast gráfico desde el rayo dado
    /// </summary>
    private GraphicRaycastResult PerformGraphicRaycast(Ray ray)
    {
        GraphicRaycastResult result = new GraphicRaycastResult();

        if (graphicRaycaster == null)
            return result;

        // Convertir el rayo 3D a una posición 2D en la pantalla para el raycast gráfico
        // Esto es complejo porque necesitamos proyectar el rayo 3D al Canvas
        
        // Para canvases en WorldSpace, necesitamos hacer un raycast 3D normal
        RaycastHit hit;
        Physics.Raycast(ray, out hit, raycastDistance);

        if (hit.collider != null)
        {
            result.gameObject = hit.collider.gameObject;
            result.distance = hit.distance;
        }

        return result;
    }

    /// <summary>
    /// Estructura para almacenar resultados de raycast gráfico
    /// </summary>
    private struct GraphicRaycastResult
    {
        public GameObject gameObject;
        public float distance;
    }
}
