using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Laser VR pointer visible during all mission interactions (dialogues and questions).
/// Activates automatically when any IVRPointerTarget in the scene is active.
/// To add support for a new panel, implement IVRPointerTarget — no changes needed here.
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
    [SerializeField] private LayerMask raycastMask = Physics.DefaultRaycastLayers;

    private LineRenderer lineRenderer;
    private GameObject dot;
    private Renderer dotRenderer;
    private Button hoveredButton = null;
    private bool isGrabbing = false;
    private IVRPointerTarget[] _targets;

    public bool IsGrabbing => isGrabbing;

    private void Awake()
    {
        CreateLineRenderer();
        CreateDot();
    }

    private void Start()
    {
        // true = include inactive GameObjects so panels that start hidden are still found.
        _targets = FindObjectsOfType<MonoBehaviour>(true).OfType<IVRPointerTarget>().ToArray();

        if (_targets.Length == 0)
            Debug.LogWarning("[VRRayPointer] No se encontró ningún IVRPointerTarget en la escena.");
        else
            Debug.Log($"[VRRayPointer] {_targets.Length} target(s) registrados.");

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
        if (isGrabbing) return;

        bool pointerActive = false;
        bool anyBlocking   = false;

        foreach (var target in _targets)
        {
            if (!target.IsPointerActive) continue;
            pointerActive = true;
            if (target.BlocksTriggerFallback)
                anyBlocking = true;
        }

        SetPointerVisible(pointerActive);
        if (!pointerActive) return;

        Ray        ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
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

        if (OVRInput.GetDown(selectButton, controller))
        {
            if (hoveredButton != null && hoveredButton.interactable)
            {
                TrySelectHoveredButton();
            }
            else if (!anyBlocking)
            {
                foreach (var target in _targets)
                {
                    if (target.IsPointerActive)
                        target.OnPointerTriggerFallback();
                }
            }
        }
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
        if (dot != null && !visible) dot.SetActive(false);
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

        Material mat = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material = mat;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(rayColorStart, 0f), new GradientColorKey(rayColorEnd, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(rayColorStart.a, 0f), new GradientAlphaKey(rayColorEnd.a, 1f) }
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
