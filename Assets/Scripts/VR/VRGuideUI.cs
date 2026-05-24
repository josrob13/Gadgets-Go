using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the three VR Guide panels (Main Menu / Mission List / Hint).
/// Implements IVRPointerTarget so the right-hand ray pointer activates automatically
/// whenever the guide is open — no changes to VRRayPointer required.
///
/// ──────────────────────────────────────────────────────────────────
/// Required Canvas hierarchy (WorldSpace, RenderMode = WorldSpace):
///
///   [Canvas]  VRGuideCanvas          ← Canvas + GraphicRaycaster + VRGuideController + VRGuideUI
///   └── GuideRootPanel               ← Image (background card). Assign to guideRootPanel.
///       ├── MainMenuPanel            ← assign to mainMenuPanel
///       │   ├── TitleText (TMP)      ← e.g. "Guía de Juego"
///       │   ├── MissionsButton       ← Button — assign to missionsButton
///       │   │   └── Label (TMP)      ← e.g. "¿Qué debo hacer?"
///       │   └── HintButton           ← Button — assign to hintButton
///       │       └── Label (TMP)      ← e.g. "Pista" — assign to hintButtonLabel
///       ├── MissionListPanel         ← assign to missionListPanel (inactive by default)
///       │   ├── TitleText (TMP)      ← e.g. "Misiones"
///       │   ├── MissionListContainer ← Empty child with VerticalLayoutGroup — assign to missionListContainer
///       │   └── BackButton           ← Button — assign to missionListBackButton
///       │       └── Label (TMP)      ← e.g. "← Volver"
///       └── HintPanel                ← assign to hintPanel (inactive by default)
///           ├── TitleText (TMP)      ← e.g. "Pista"
///           ├── HintText (TMP)       ← assign to hintText
///           └── BackButton           ← Button — assign to hintBackButton
///               └── Label (TMP)      ← e.g. "← Volver"
/// ──────────────────────────────────────────────────────────────────
/// Buttons are detected by VRRayPointer via Physics.Raycast, so BoxColliders
/// are added to every button automatically in Start().
/// </summary>
[RequireComponent(typeof(Canvas))]
public class VRGuideUI : MonoBehaviour, IVRPointerTarget
{
    [Header("Root")]
    [SerializeField] private GameObject guideRootPanel;

    [Header("Main Menu Panel")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button missionsButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI hintButtonLabel;

    [Header("Mission List Panel")]
    [SerializeField] private GameObject missionListPanel;
    [SerializeField] private Transform missionListContainer;
    [SerializeField] private Button missionListBackButton;

    [Header("Hint Panel")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private Button hintBackButton;

    // ── IVRPointerTarget ──────────────────────────────────────────────────────
    // The right-hand pointer is active whenever the guide root panel is visible.
    // BlocksTriggerFallback = true prevents accidental dialogue advances while
    // the player is reading the guide.
    public bool IsPointerActive       => guideRootPanel != null && guideRootPanel.activeSelf;
    public bool BlocksTriggerFallback => true;
    public void OnPointerTriggerFallback() { }

    private static readonly Color ColorPending   = Color.white;
    private static readonly Color ColorCompleted = new Color(0.45f, 0.85f, 0.45f);
    private static readonly Color ColorDimmed    = new Color(0.55f, 0.55f, 0.55f);

    private readonly List<GameObject> _missionEntries = new();

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        missionsButton?.onClick.AddListener(() => VRGuideController.Instance?.OnRequestMissions());
        hintButton?.onClick.AddListener(() => VRGuideController.Instance?.OnRequestHint());
        missionListBackButton?.onClick.AddListener(BackToMainMenu);
        hintBackButton?.onClick.AddListener(BackToMainMenu);
    }

    private void Start()
    {
        // RectTransform.rect is (0,0) in Awake — layout hasn't run yet.
        // Force a full Canvas rebuild so the always-visible main-menu buttons get correct sizes.
        Canvas.ForceUpdateCanvases();
        AddCollider(missionsButton);
        AddCollider(hintButton);
        // Back-buttons live on panels that start inactive; their colliders are sized
        // the first time SetPanels activates their panel (see SetPanels below).
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the main menu. Enables the Hint button only when a quiz is active.
    /// </summary>
    public void ShowMainMenu(bool hintAvailable)
    {
        SetPanels(main: true, missions: false, hint: false);

        if (hintButton != null)
            hintButton.interactable = hintAvailable;

        if (hintButtonLabel != null)
            hintButtonLabel.color = hintAvailable ? ColorPending : ColorDimmed;
    }

    /// <summary>
    /// Shows the mission list for <paramref name="world"/>, rebuilding entries fresh each time.
    /// </summary>
    public void ShowMissionList(World world)
    {
        SetPanels(main: false, missions: true, hint: false);
        RebuildMissionList(world);
    }

    /// <summary>
    /// Shows the hint panel with <paramref name="hint"/> as the body text.
    /// </summary>
    public void ShowHint(string hint)
    {
        SetPanels(main: false, missions: false, hint: true);
        if (hintText != null)
            hintText.text = hint;
    }

    /// <summary>
    /// Hides the entire guide (root panel off). Called by VRGuideController on close.
    /// The component itself stays active so VRRayPointer can always find it.
    /// </summary>
    public void HideAll()
    {
        if (guideRootPanel != null)
            guideRootPanel.SetActive(false);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private void BackToMainMenu()
    {
        bool hintAvailable = DialogueManager.Instance?.ActiveQuestion != null;
        ShowMainMenu(hintAvailable);
    }

    private void SetPanels(bool main, bool missions, bool hint)
    {
        if (guideRootPanel != null)   guideRootPanel.SetActive(true);
        if (mainMenuPanel != null)    mainMenuPanel.SetActive(main);
        if (missionListPanel != null) missionListPanel.SetActive(missions);
        if (hintPanel != null)        hintPanel.SetActive(hint);

        // Force ALL canvases to recalculate layout so RectTransform.rect values
        // are valid before we size the BoxColliders used by Physics.Raycast.
        Canvas.ForceUpdateCanvases();

        if (main && mainMenuPanel != null)
        {
            AddCollider(missionsButton);
            AddCollider(hintButton);
        }
        else if (missions && missionListPanel != null)
        {
            AddCollider(missionListBackButton);
        }
        else if (hint && hintPanel != null)
        {
            AddCollider(hintBackButton);
        }
    }

    private void RebuildMissionList(World world)
    {
        foreach (var entry in _missionEntries)
            Destroy(entry);
        _missionEntries.Clear();

        if (world == null || missionListContainer == null) return;

        foreach (Mission mission in world.Missions)
        {
            if (mission == null) continue;

            bool done = PlayerProgress.Instance != null
                && PlayerProgress.Instance.IsMissionCompleted(mission.GetMissionName());

            var entry = BuildMissionEntry(mission.GetMissionName(), done);
            entry.transform.SetParent(missionListContainer, false);
            _missionEntries.Add(entry);
        }
    }

    /// <summary>
    /// Builds a single mission row entirely in code — no prefab required.
    /// Layout: [dot] [mission name]
    /// Completed missions are greyed out with strikethrough; pending are white.
    /// </summary>
    private static GameObject BuildMissionEntry(string missionName, bool completed)
    {
        // ── Row ──────────────────────────────────────────────────────────────
        var row = new GameObject(missionName, typeof(RectTransform));
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 44f);

        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 10f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.padding = new RectOffset(6, 6, 4, 4);

        // ── Status dot ───────────────────────────────────────────────────────
        var dot = new GameObject("Dot", typeof(RectTransform));
        dot.transform.SetParent(row.transform, false);
        dot.GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 16f);

        var dotImg = dot.AddComponent<Image>();
        dotImg.color = completed
            ? ColorCompleted
            : new Color(0.7f, 0.7f, 0.7f, 0.5f);

        var dotLayout = dot.AddComponent<LayoutElement>();
        dotLayout.minWidth = 16f;
        dotLayout.minHeight = 16f;
        dotLayout.preferredWidth = 16f;

        // ── Mission name label ────────────────────────────────────────────────
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);

        var labelLayout = labelGO.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;

        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = missionName;
        label.fontSize = 13f;
        label.color = completed ? ColorDimmed : ColorPending;
        label.fontStyle = completed ? FontStyles.Strikethrough : FontStyles.Normal;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;

        return row;
    }

    /// <summary>
    /// Adds a BoxCollider sized to the button's RectTransform so Physics.Raycast
    /// (used by VRRayPointer) can detect it in WorldSpace.
    /// </summary>
    private static void AddCollider(Button btn)
    {
        if (btn == null) { Debug.LogWarning("[VRGuideUI] AddCollider: btn is null"); return; }
        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null) return;

        var col = btn.GetComponent<BoxCollider>() ?? btn.gameObject.AddComponent<BoxCollider>();
        col.size   = new Vector3(rt.rect.width, rt.rect.height, 20f);
        col.center = Vector3.zero;
        Debug.Log($"[VRGuideUI] Collider '{btn.name}': size={col.size}, active={btn.gameObject.activeInHierarchy}, interactable={btn.interactable}");
    }
}
