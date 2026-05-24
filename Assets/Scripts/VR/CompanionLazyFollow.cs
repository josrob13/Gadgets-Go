using UnityEngine;

public class CompanionLazyFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float positionSpeed = 3f;
    [SerializeField] private float rotationSpeed = 3f;

    [Header("Idle Offset — companion at rest (waist-left)")]
    [Tooltip("X = side (negative = player's left), Y = vertical (negative = below eye), Z = forward")]
    [SerializeField] private Vector3 idleOffset = new Vector3(-0.40f, -0.50f, 0.35f);

    [Header("Active Offset — guide panel open (centred, eye level)")]
    [Tooltip("X = side (negative = player's left), Y = vertical (negative = below eye), Z = forward")]
    [SerializeField] private Vector3 activeOffset = new Vector3(0.00f, -0.05f, 0.75f);

    [Header("Companion Model")]
    [SerializeField] private Transform companionModel;
    [SerializeField] private float modelFacingOffset = 180f;

    [Header("Eye Anchor")]
    [Tooltip("Drag CenterEyeAnchor from OVRCameraRig here. If left empty the script will search the scene at Start.")]
    [SerializeField] private Transform eyeTransform;

    private Transform _eye;
    private bool _guideOpen;

    private void Start()
    {
        Debug.Log($"[CompanionLazyFollow] Start — eyeTransform assigned in Inspector: {eyeTransform != null} ({(eyeTransform != null ? eyeTransform.name : "null")})");

        if (eyeTransform != null)
        {
            _eye = eyeTransform;
        }
        else
        {
            OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
            Debug.Log($"[CompanionLazyFollow] eyeTransform was null — searched for OVRCameraRig: {(rig != null ? rig.name : "NOT FOUND")}");
            _eye = rig != null ? rig.centerEyeAnchor : Camera.main?.transform;
        }

        if (_eye != null)
        {
            Vector3 target = ComputeTargetPosition(HorizontalForward());
            Debug.Log($"[CompanionLazyFollow] _eye found: {_eye.name} at world pos {_eye.position}. Placing CompanionRoot at {target}");
            transform.position = target;
        }
        else
        {
            Debug.LogError("[CompanionLazyFollow] _eye is NULL — companion will not move. Assign CenterEyeAnchor to Eye Transform in the Inspector.");
        }

        if (companionModel != null)
        {
            companionModel.localRotation = Quaternion.Euler(0f, modelFacingOffset, 0f);
            Debug.Log($"[CompanionLazyFollow] companionModel found: {companionModel.name}");
        }
        else
        {
            Debug.LogWarning("[CompanionLazyFollow] companionModel is NULL — robot will not face the player. Assign the robot child to Companion Model in the Inspector.");
        }
    }

    private void Update()
    {
        if (_eye == null) return;

        Vector3 fwd = HorizontalForward();

        transform.position = Vector3.Lerp(
            transform.position,
            ComputeTargetPosition(fwd),
            positionSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.LookRotation(fwd, Vector3.up),
            rotationSpeed * Time.deltaTime
        );
    }

    public void SetGuideOpen(bool open)
    {
        Debug.Log($"[CompanionLazyFollow] SetGuideOpen({open})");
        _guideOpen = open;
    }

    private Vector3 ComputeTargetPosition(Vector3 fwd)
    {
        Vector3 right  = Vector3.Cross(Vector3.up, fwd);
        Vector3 offset = _guideOpen ? activeOffset : idleOffset;

        return _eye.position
            + fwd   * offset.z
            + right * offset.x
            + Vector3.up * offset.y;
    }

    private Vector3 HorizontalForward()
    {
        Vector3 fwd = _eye.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        return fwd.normalized;
    }
}
