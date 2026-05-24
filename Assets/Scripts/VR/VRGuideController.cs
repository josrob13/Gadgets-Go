using UnityEngine;

/// <summary>
/// Singleton that controls the VR Guide panel.
/// Pressing the left controller Menu button (OVRInput.Button.Start + LTouch) toggles the guide.
///
/// Place this component on the same GameObject as the WorldSpace Canvas + VRGuideUI.
/// That GameObject must be a child of CompanionRoot (which holds CompanionLazyFollow).
/// Assign the WorldsDB asset and the CompanionLazyFollow reference in the Inspector.
/// </summary>
public class VRGuideController : MonoBehaviour
{
    public static VRGuideController Instance { get; private set; }

    [Header("Data")]
    [Tooltip("Assign the WorldsDB ScriptableObject so the mission list can be built.")]
    [SerializeField] private WorldsDB worldsDB;

    [Header("Companion")]
    [Tooltip("Assign the CompanionLazyFollow on the CompanionRoot parent GameObject.")]
    [SerializeField] private CompanionLazyFollow companion;

    [Header("Locomotion")]
    [Tooltip("Drag any smooth-turn or smooth-move MonoBehaviours here. " +
             "They will be disabled while the guide is open so the joystick " +
             "no longer triggers the comfort vignette.")]
    [SerializeField] private MonoBehaviour[] locomotionHandlers;

    private VRGuideUI _guideUI;
    private bool _isOpen;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
            _guideUI = GetComponent<VRGuideUI>();
            Debug.Log($"[VRGuideController] Awake — Instance set. _guideUI found: {_guideUI != null}. companion assigned: {companion != null}. worldsDB assigned: {worldsDB != null}");
        }
        else
        {
            Debug.Log("[VRGuideController] Duplicate detected — destroying this instance.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("[VRGuideController] Start — hiding guide.");
        _guideUI?.HideAll();
    }

    private void Update()
    {
        // Left controller Menu / Start button
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
        {
            Debug.Log("[VRGuideController] Left Menu button pressed — toggling guide.");
            Toggle();
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        Debug.Log($"[VRGuideController] Open() — _guideUI: {(_guideUI != null ? "OK" : "NULL")}, companion: {(companion != null ? "OK" : "NULL")}");

        companion?.SetGuideOpen(true);
        SetLocomotion(false);
        FaceTrackingManager.Instance?.SetGuidePause(true);

        bool hintAvailable = DialogueManager.Instance?.ActiveQuestion != null;
        _guideUI?.ShowMainMenu(hintAvailable);

        DialogueManager.Instance?.SetGuidePause(true);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        Debug.Log("[VRGuideController] Close()");

        companion?.SetGuideOpen(false);
        SetLocomotion(true);
        FaceTrackingManager.Instance?.SetGuidePause(false);

        _guideUI?.HideAll();
        DialogueManager.Instance?.SetGuidePause(false);
    }

    /// <summary>Called by the Missions button in the main menu panel.</summary>
    public void OnRequestMissions()
    {
        World world = GetCurrentWorld();
        _guideUI?.ShowMissionList(world);
    }

    /// <summary>Called by the Hint button in the main menu panel.</summary>
    public void OnRequestHint()
    {
        QuestionNode q = DialogueManager.Instance?.ActiveQuestion;
        string text = (q != null && !string.IsNullOrEmpty(q.hint))
            ? q.hint
            : "No hay pista disponible para esta pregunta.";
        _guideUI?.ShowHint(text);
    }

    // ─── Internals ────────────────────────────────────────────────────────────

    private void SetLocomotion(bool enabled)
    {
        foreach (MonoBehaviour handler in locomotionHandlers)
            if (handler != null) handler.enabled = enabled;
    }

    /// <summary>
    /// Returns the first world the player hasn't fully completed yet.
    /// Falls back to the last world if everything is done.
    /// </summary>
    private World GetCurrentWorld()
    {
        if (worldsDB == null || worldsDB.worlds == null || worldsDB.worlds.Length == 0)
            return null;

        foreach (World world in worldsDB.worlds)
        {
            if (world == null) continue;
            if (PlayerProgress.Instance == null || !PlayerProgress.Instance.IsWorldCompleted(world))
                return world;
        }

        return worldsDB.worlds[worldsDB.worlds.Length - 1];
    }
}
