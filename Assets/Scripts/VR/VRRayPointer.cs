using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Laser VR pointer visible during all mission interactions (dialogues and questions).
///
/// During dialogue: the laser is visible and pressing the index trigger advances the text.
/// During questions: the laser detects buttons and clicks them when the trigger is pressed.
/// </summary>
public class VRRayPointer : MonoBehaviour
{
    [Header("Ray visual")]
    [SerializeField] private float maxRayDistance = 10f;
    [SerializeField] private float rayWidth = 0.004f;
    [SerializeField] private Color rayColorStart = new Color(0.3f, 0.8f, 1f, 1f);
    [SerializeField] private Color rayColorEnd = new Color(0.3f, 0.8f, 1f, 0.1f);

    [Header("Dot visual")]
    [SerializeField] private float dotSize = 0.015f;
    [SerializeField] private Color dotColorNormal = Color.white;
    [SerializeField] private Color dotColorHovered = new Color(0.3f, 1f, 0.4f); 

    [Header("Controller input")]
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    [SerializeField] private OVRInput.Button selectButton = OVRInput.Button.PrimaryIndexTrigger;

    [Header("References")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private QuestionUI questionUI;
    [SerializeField] private LayerMask raycastMask = Physics.DefaultRaycastLayers;

    private LineRenderer lineRenderer;
    private GameObject   dot;
    private Renderer     dotRenderer;

    private Button hoveredButton = null;
    private bool previousSelectState = false;
    private bool isGrabbing = false;

    public bool IsGrabbing => isGrabbing;

    private void Awake()
    {
        CreateLineRenderer();
        CreateDot();
    }

    private void Start()
    {
        if (dialogueUI == null)
            dialogueUI = FindFirstObjectByType<DialogueUI>();
        if (questionUI == null)
            questionUI = FindFirstObjectByType<QuestionUI>();

        if (dialogueUI == null && questionUI == null)
            Debug.LogError("[VRRayPointer] No se encontró DialogueUI ni QuestionUI. Asignálos en el Inspector.");

        SetPointerVisible(false);
    }

    public void SetGrabbing(bool grabbing)
    {
        isGrabbing = grabbing;
        if (grabbing)
            SetPointerVisible(false);
    }

    private void Update()
    {
        // Pointer deactivates when the player is moving the canvas
        if (isGrabbing)
            return;

        bool dialogueActive  = dialogueUI != null && dialogueUI.IsVisible;
        bool questionActive  = questionUI != null && questionUI.IsVisible;
        bool pointerActive   = dialogueActive || questionActive;

        SetPointerVisible(pointerActive);
        if (!pointerActive) return;

        // Raycasting from controller position and direction
        Ray         ray = new Ray(transform.position, transform.forward);
        RaycastHit  hit;
        hoveredButton = null;

        if (Physics.Raycast(ray, out hit, maxRayDistance, raycastMask))
        {
            UpdateLine(transform.position, hit.point);

            dot.transform.position = hit.point;
            dot.transform.LookAt(transform.position);
            dot.SetActive(true);

            hoveredButton = hit.collider.GetComponent<Button>()
                         ?? hit.collider.GetComponentInParent<Button>();

            bool isInteractable = hoveredButton != null && hoveredButton.interactable;
            dotRenderer.material.color = isInteractable ? dotColorHovered : dotColorNormal;
        }
        else
        {
            UpdateLine(transform.position, transform.position + transform.forward * maxRayDistance);
            dot.SetActive(false);
        }

        bool currentSelectState = OVRInput.GetDown(selectButton, controller);
        if (currentSelectState)
        {
            if (hoveredButton != null && hoveredButton.interactable)
            {
                TrySelectHoveredButton();
            }
            else if (dialogueActive)
            {
                Debug.Log("[VRRayPointer] Trigger pulsado → avanzando diálogo.");
                dialogueUI.NextPressed = true;
            }
        }
        previousSelectState = currentSelectState;
    }

    private void TrySelectHoveredButton()
    {
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

    private void CreateLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth    = rayWidth;
        lineRenderer.endWidth      = rayWidth * 0.3f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows    = false;

        // Unlit material so it's not affected by scene lighting
        Material mat = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material = mat;

        // Gradient color along the ray
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
