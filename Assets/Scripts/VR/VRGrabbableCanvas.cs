using UnityEngine;

/// <summary>
/// Makes the dialogue canvas grabbable with the right controller's grip trigger.
///
/// Mechanics:
///   • When the grip is held down, the canvas remains "attached" to the hand, preserving
///     the exact position and rotation offset from the moment it was grabbed.
///   • When the grip is released, the canvas remains fixed in its new position/rotation.
///   • The VRRayPointer laser is hidden while grabbing.
///   • Only acts when the canvas is active (during a dialogue).
/// </summary>
public class VRGrabbableCanvas : MonoBehaviour
{
    [Header("Controller")]
    [SerializeField] private Transform rightHandAnchor;
    [SerializeField] private OVRInput.Button grabButton = OVRInput.Button.PrimaryHandTrigger;
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private VRRayPointer rayPointer;

    [Header("Visual feedback")]
    [SerializeField] private bool scaleOnGrab = true;
    [SerializeField] private float grabbedScale = 1.05f;

    private bool isGrabbing = false;
    private bool _grabbingEnabled = true;
    private Vector3 localOffsetPosition;
    private Quaternion localOffsetRotation;
    private Vector3 originalScale;

    private void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>() ?? GetComponentInChildren<Canvas>();
    }

    private void Start()
    {
        if (rightHandAnchor == null)
        {
            OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null)
                rightHandAnchor = rig.rightHandAnchor;
        }

        if (rayPointer == null)
            rayPointer = FindObjectOfType<VRRayPointer>();

        if (rightHandAnchor == null)
            Debug.LogError("[VRGrabbableCanvas] No se encontró RightHandAnchor. Asígnalo en el Inspector.");

        if (targetCanvas != null)
            originalScale = targetCanvas.transform.localScale;
    }

    private void Update()
    {
        if (!_grabbingEnabled || targetCanvas == null || !targetCanvas.gameObject.activeSelf || rightHandAnchor == null)
            return;

        bool gripPressed = OVRInput.Get(grabButton, controller);

        if (gripPressed && !isGrabbing)
            BeginGrab();
        else if (!gripPressed && isGrabbing)
            EndGrab();

        if (isGrabbing)
            ApplyGrab();
    }

    private void BeginGrab()
    {
        isGrabbing = true;

        // Calculate the offset of the canvas with respect to the hand anchor.
        localOffsetPosition = rightHandAnchor.InverseTransformPoint(targetCanvas.transform.position);
        localOffsetRotation = Quaternion.Inverse(rightHandAnchor.rotation) * targetCanvas.transform.rotation;
        if (scaleOnGrab && targetCanvas != null)
            targetCanvas.transform.localScale = originalScale * grabbedScale;

        // Hide laser pointer
        rayPointer?.SetGrabbing(true);

        Debug.Log("[VRGrabbableCanvas] Canvas agarrado.");
    }

    private void ApplyGrab()
    {
        targetCanvas.transform.position = rightHandAnchor.TransformPoint(localOffsetPosition);
        targetCanvas.transform.rotation = rightHandAnchor.rotation * localOffsetRotation;
    }

    private void EndGrab()
    {
        isGrabbing = false;

        // Restore scale
        if (scaleOnGrab && targetCanvas != null)
            targetCanvas.transform.localScale = originalScale;

        // Re-activate laser pointer
        rayPointer?.SetGrabbing(false);

        Debug.Log("[VRGrabbableCanvas] Canvas soltado en nueva posición.");
    }

    public bool IsGrabbing => isGrabbing;

    public void SetGrabbingEnabled(bool enabled)
    {
        _grabbingEnabled = enabled;
        if (!enabled && isGrabbing)
            EndGrab();
    }

    public void ForceRelease()
    {
        if (isGrabbing)
            EndGrab();
    }
}
