using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Controls the position and rotation of the dialogue Canvas in World Space.
/// Since there is only one Canvas for the entire game, this script is responsible for
/// teleporting it to the active mission area when requested.
/// </summary>
public class DialogueCanvasController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private float distanceFromPlayer = 2.5f;
    [SerializeField] private float heightOffset = 0f;
    
    private Transform playerCameraTransform;
    private VRGrabbableCanvas grabbable;

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        grabbable = GetComponent<VRGrabbableCanvas>();
        if (grabbable == null) grabbable = GetComponentInParent<VRGrabbableCanvas>();
        if (grabbable == null) grabbable = GetComponentInChildren<VRGrabbableCanvas>();
    }

    public Canvas GetCanvas() => canvas;

    private void Start()
    {
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

        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }

        canvas.gameObject.SetActive(false);
    }

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

    public void OnDialogueEnd()
    {
        grabbable?.ForceRelease();

        canvas.gameObject.SetActive(false);
        Debug.Log("[DialogueCanvasController] Diálogo terminado. Canvas oculto.");
    }
}
