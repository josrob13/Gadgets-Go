using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puntero láser VR visible durante toda la interacción de misiones (diálogos y preguntas).
///
/// Durante el diálogo: el láser es visible y al apretar el gatillo índice se avanza el texto.
/// Durante las preguntas: el láser detecta botones y los pulsa al apretar el trigger.
///
/// SETUP en Unity Editor:
///   1. Adjunta este script al GameObject "RightHandAnchor" (dentro de TrackingSpace del Camera Rig).
///   2. Asigna DialogueUI y QuestionUI en el Inspector (o se buscarán automáticamente).
///   3. Asegúrate de que DialogueUI tiene "Enable VR Interaction" DESACTIVADO para evitar duplicación.
/// </summary>
public class VRRayPointer : MonoBehaviour
{
    [Header("Visual del Rayo")]
    [Tooltip("Distancia máxima del rayo en metros.")]
    [SerializeField] private float maxRayDistance = 10f;
    [Tooltip("Ancho del rayo en metros.")]
    [SerializeField] private float rayWidth = 0.004f;
    [Tooltip("Color del rayo (parte inicial).")]
    [SerializeField] private Color rayColorStart = new Color(0.3f, 0.8f, 1f, 1f);
    [Tooltip("Color del rayo (parte final, más transparente).")]
    [SerializeField] private Color rayColorEnd   = new Color(0.3f, 0.8f, 1f, 0.1f);

    [Header("Punto de Impacto")]
    [Tooltip("Tamaño de la esfera que indica dónde apunta el rayo.")]
    [SerializeField] private float dotSize = 0.015f;
    [SerializeField] private Color dotColorNormal  = Color.white;
    [SerializeField] private Color dotColorHovered = new Color(0.3f, 1f, 0.4f); // verde al pasar sobre botón

    [Header("Input del Mando")]
    [SerializeField] private OVRInput.Controller controller    = OVRInput.Controller.RTouch;
    [SerializeField] private OVRInput.Button     selectButton  = OVRInput.Button.PrimaryIndexTrigger;

    [Header("Referencias")]
    [Tooltip("Si se deja vacío, se busca automáticamente en la escena.")]
    [SerializeField] private DialogueUI dialogueUI;
    [Tooltip("Si se deja vacío, se busca automáticamente en la escena.")]
    [SerializeField] private QuestionUI questionUI;
    [Tooltip("LayerMask que usa el rayo. Asegúrate de que los botones estén en ese layer (Default funciona).")]
    [SerializeField] private LayerMask raycastMask = Physics.DefaultRaycastLayers;

    // ── Componentes generados en tiempo de ejecución ──
    private LineRenderer lineRenderer;
    private GameObject   dot;
    private Renderer     dotRenderer;

    // ── Estado ──
    private Button hoveredButton      = null;
    private bool   previousSelectState = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  Inicialización
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        CreateLineRenderer();
        CreateDot();
    }

    private void Start()
    {
        if (dialogueUI == null)
            dialogueUI = FindObjectOfType<DialogueUI>();
        if (questionUI == null)
            questionUI = FindObjectOfType<QuestionUI>();

        if (dialogueUI == null && questionUI == null)
            Debug.LogError("[VRRayPointer] No se encontró DialogueUI ni QuestionUI. Asignálos en el Inspector.");

        SetPointerVisible(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Update
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        // El puntero se activa durante el diálogo Y durante las preguntas
        bool dialogueActive  = dialogueUI != null && dialogueUI.IsVisible;
        bool questionActive  = questionUI != null && questionUI.IsVisible;
        bool pointerActive   = dialogueActive || questionActive;

        SetPointerVisible(pointerActive);
        if (!pointerActive) return;

        // Lanzar rayo desde la posición y dirección del mando
        Ray         ray = new Ray(transform.position, transform.forward);
        RaycastHit  hit;
        hoveredButton = null;

        if (Physics.Raycast(ray, out hit, maxRayDistance, raycastMask))
        {
            // Posicionar el rayo hasta el punto de impacto
            UpdateLine(transform.position, hit.point);

            // Posicionar el punto de impacto
            dot.transform.position = hit.point;
            // Orientar el punto mirando hacia la cámara para que siempre se vea redondo
            dot.transform.LookAt(transform.position);
            dot.SetActive(true);

            // Detectar si el objeto golpeado es (o tiene) un Button
            hoveredButton = hit.collider.GetComponent<Button>()
                         ?? hit.collider.GetComponentInParent<Button>();

            // Feedback de color: verde al pasar sobre un botón interactable
            bool isInteractable = hoveredButton != null && hoveredButton.interactable;
            dotRenderer.material.color = isInteractable ? dotColorHovered : dotColorNormal;
        }
        else
        {
            // El rayo no impactó nada — extender hasta la distancia máxima
            UpdateLine(transform.position, transform.position + transform.forward * maxRayDistance);
            dot.SetActive(false);
        }

        // ── Detectar pulsación del gatillo índice ──
        bool currentSelectState = OVRInput.GetDown(selectButton, controller);
        if (currentSelectState)
        {
            if (hoveredButton != null && hoveredButton.interactable)
            {
                // Pulsar el botón de respuesta en QuestionUI
                TrySelectHoveredButton();
            }
            else if (dialogueActive)
            {
                // Avanzar el diálogo cuando no hay botón bajo el cursor
                Debug.Log("[VRRayPointer] Trigger pulsado → avanzando diálogo.");
                dialogueUI.NextPressed = true;
            }
        }
        previousSelectState = currentSelectState;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void TrySelectHoveredButton()
    {
        // Invocar el onClick del botón directamente (funciona con cualquier listener)
        hoveredButton.onClick.Invoke();
        Debug.Log($"[VRRayPointer] Botón seleccionado: {hoveredButton.gameObject.name}");
    }

    private void UpdateLine(Vector3 start, Vector3 end)
    {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    private void SetPointerVisible(bool visible)
    {
        if (lineRenderer != null) lineRenderer.enabled = visible;
        if (dot != null && !visible)         dot.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Creación de componentes en runtime
    // ─────────────────────────────────────────────────────────────────────────

    private void CreateLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth    = rayWidth;
        lineRenderer.endWidth      = rayWidth * 0.3f; // se afina hacia el final
        lineRenderer.useWorldSpace = true;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows    = false;

        // Material unlit para que no le afecte la iluminación de la escena
        Material mat = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material = mat;

        // Gradiente de color a lo largo del rayo
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(rayColorStart, 0f), new GradientColorKey(rayColorEnd, 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(rayColorStart.a, 0f), new GradientAlphaKey(rayColorEnd.a, 1f) }
        );
        lineRenderer.colorGradient = gradient;
    }

    private void CreateDot()
    {
        dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "VRRayDot";
        dot.transform.localScale = Vector3.one * dotSize;

        // Quitar el collider del dot para que no interfiera con los raycasts
        Destroy(dot.GetComponent<Collider>());

        dotRenderer = dot.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = dotColorNormal;
        dotRenderer.material    = mat;
        dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dotRenderer.receiveShadows    = false;

        dot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (dot != null) Destroy(dot);
    }
}
