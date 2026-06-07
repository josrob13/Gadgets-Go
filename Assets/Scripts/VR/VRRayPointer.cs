using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Laser VR pointer for mission interactions and main menu.
/// Uses Physics.Raycast for 3D world elements, and GraphicRaycaster for Canvas UI.
/// Activates automatically when any IVRPointerTarget in the scene is active.
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
    private TMP_InputField hoveredInputField = null;
    private TouchScreenKeyboard vrKeyboard = null;
    private bool isGrabbing = false;
    private IVRPointerTarget[] _targets;
    private GraphicRaycaster[] _canvasRaycasters;
    private string _lastHitName = "";

    public bool IsGrabbing => isGrabbing;

    private void Awake()
    {
        CreateLineRenderer();
        CreateDot();
    }

    private void Start()
    {
        _targets = FindObjectsOfType<MonoBehaviour>(true).OfType<IVRPointerTarget>().ToArray();
        _canvasRaycasters = FindObjectsOfType<GraphicRaycaster>(true);

        if (_targets.Length == 0)
            Debug.LogWarning("[VRRayPointer] No IVRPointerTarget found in scene.");
        else
            Debug.Log($"[VRRayPointer] {_targets.Length} target(s) registered, {_canvasRaycasters.Length} canvas(es) found.");

        SetPointerVisible(false);
    }

    public void SetGrabbing(bool grabbing)
    {
        isGrabbing = grabbing;
        if (grabbing) SetPointerVisible(false);
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
            if (target.BlocksTriggerFallback) anyBlocking = true;
        }

        SetPointerVisible(pointerActive);
        if (!pointerActive) return;

        Ray ray = new Ray(transform.position, transform.forward);
        hoveredButton     = null;
        hoveredInputField = null;

        // Poll virtual keyboard result
        if (vrKeyboard != null && vrKeyboard.status == TouchScreenKeyboard.Status.Done)
        {
            if (vrKeyboard.text != null)
            {
                var activeField = EventSystem.current?.currentSelectedGameObject?.GetComponent<TMP_InputField>();
                if (activeField != null) activeField.text = vrKeyboard.text;
            }
            vrKeyboard = null;
        }

        // --- Physics raycast (3D world elements with colliders) ---
        Vector3 dotWorldPos = transform.position + transform.forward * maxRayDistance;
        bool hitSomethingPhysics = false;

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxRayDistance, raycastMask))
        {
            dotWorldPos = hit.point;
            hitSomethingPhysics = true;

            hoveredButton     = hit.collider.GetComponent<Button>()
                             ?? hit.collider.GetComponentInParent<Button>();
            hoveredInputField = hit.collider.GetComponent<TMP_InputField>()
                             ?? hit.collider.GetComponentInParent<TMP_InputField>();

            string hitName = hit.collider.gameObject.name;
            if (hitName != _lastHitName)
            {
                Debug.Log($"[VRRayPointer] Physics hit '{hitName}' — button: {hoveredButton?.name ?? "none"}");
                _lastHitName = hitName;
            }
        }

        // --- UI raycast fallback (Canvas buttons, no colliders needed) ---
        if (hoveredButton == null && hoveredInputField == null)
        {
            Vector3 uiHitWorld;
            if (CheckUIElements(ray, out uiHitWorld))
                dotWorldPos = uiHitWorld;
        }

        // Update visuals
        UpdateLine(transform.position, dotWorldPos);

        bool anythingHovered = (hoveredButton != null && hoveredButton.interactable) || hoveredInputField != null;
        if (anythingHovered || hitSomethingPhysics)
        {
            dot.transform.position = dotWorldPos;
            dot.transform.LookAt(transform.position);
            dot.SetActive(true);
            dotRenderer.material.color = anythingHovered ? dotColorHovered : dotColorNormal;
        }
        else
        {
            if (_lastHitName != "") { Debug.Log("[VRRayPointer] Not hitting anything."); _lastHitName = ""; }
            dot.SetActive(false);
        }

        // --- Trigger ---
        if (OVRInput.GetDown(selectButton, controller))
        {
            Debug.Log($"[VRRayPointer] Trigger — button: {hoveredButton?.name ?? "null"}, inputField: {hoveredInputField?.name ?? "null"}");

            if (hoveredButton != null && hoveredButton.interactable)
            {
                hoveredButton.onClick.Invoke();
                Debug.Log($"[VRRayPointer] Clicked: {hoveredButton.gameObject.name}");
            }
            else if (hoveredInputField != null)
            {
                hoveredInputField.Select();
                hoveredInputField.ActivateInputField();
                vrKeyboard = TouchScreenKeyboard.Open(hoveredInputField.text, TouchScreenKeyboardType.Default, false, false, false, false);
                Debug.Log($"[VRRayPointer] Opened keyboard for '{hoveredInputField.name}'");
            }
            else if (!anyBlocking)
            {
                foreach (var target in _targets)
                    if (target.IsPointerActive) target.OnPointerTriggerFallback();
            }
        }
    }

    private bool CheckUIElements(Ray ray, out Vector3 worldHit)
    {
        worldHit = Vector3.zero;

        foreach (var gr in _canvasRaycasters)
        {
            if (gr == null) continue;
            var canvas = gr.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace) continue;

            Camera cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (cam == null) continue;

            // Ray-plane intersection with the canvas plane
            var plane = new Plane(-canvas.transform.forward, canvas.transform.position);
            float distance;
            if (!plane.Raycast(ray, out distance)) continue;
            if (distance > maxRayDistance) continue;

            worldHit = ray.GetPoint(distance);
            Vector2 screenPos = cam.WorldToScreenPoint(worldHit);

            var pData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            gr.Raycast(pData, results);

            foreach (var result in results)
            {
                Button btn = result.gameObject.GetComponent<Button>()
                          ?? result.gameObject.GetComponentInParent<Button>();
                if (btn != null && btn.interactable)
                {
                    hoveredButton = btn;
                    if (result.gameObject.name != _lastHitName)
                    {
                        Debug.Log($"[VRRayPointer] UI hit button '{btn.name}'");
                        _lastHitName = result.gameObject.name;
                    }
                    return true;
                }

                TMP_InputField field = result.gameObject.GetComponent<TMP_InputField>()
                                    ?? result.gameObject.GetComponentInParent<TMP_InputField>();
                if (field != null)
                {
                    hoveredInputField = field;
                    return true;
                }
            }
        }

        return false;
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
        dotRenderer.material = mat;
        dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dotRenderer.receiveShadows    = false;

        dot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (dot != null) Destroy(dot);
    }
}
