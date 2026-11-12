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
    [SerializeField] private Button quitButton;

    [Header("Confirm Quit Panel")]
    [SerializeField] private GameObject confirmQuitPanel;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Mission List Panel")]
    [SerializeField] private GameObject missionListPanel;
    [SerializeField] private Transform missionListContainer;
    [SerializeField] private Button missionListBackButton;

    [Header("Mission List - Aspecto y scroll")]
    [Tooltip("Alto fijo de cada fila (unidades del canvas). El texto se encoge para caber en él.")]
    [SerializeField] private float rowHeight = 20f;
    [Tooltip("Tamaño máximo de la fuente de cada misión (autosize).")]
    [SerializeField] private float fontMax = 8.5f;
    [Tooltip("Tamaño mínimo de la fuente; por debajo se corta con '...'.")]
    [SerializeField] private float fontMin = 5f;
    [Tooltip("Velocidad del scroll con el joystick (alturas de viewport por segundo).")]
    [SerializeField] private float scrollSpeed = 1.5f;

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

    private static readonly Color ColorPending   = Color.black;
    private static readonly Color ColorCompleted = new Color(0.45f, 0.85f, 0.45f);
    private static readonly Color ColorDimmed    = new Color(0.6f, 0.6f, 0.6f);

    private readonly List<GameObject> _missionEntries = new();
    private ScrollRect _scroll;

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_scroll == null || missionListPanel == null || !missionListPanel.activeInHierarchy) return;

        float y = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).y;

        float overflow = _scroll.content.rect.height - _scroll.viewport.rect.height;
        if (Mathf.Abs(y) < 0.2f || overflow <= 0f) return;   // si cabe todo, no hace nada

        _scroll.verticalNormalizedPosition = Mathf.Clamp01(
            _scroll.verticalNormalizedPosition
            + y * scrollSpeed * Time.unscaledDeltaTime * _scroll.viewport.rect.height / overflow);
    }

    private void Awake()
    {
        missionsButton?.onClick.AddListener(() => VRGuideController.Instance?.OnRequestMissions());
        hintButton?.onClick.AddListener(() => VRGuideController.Instance?.OnRequestHint());
        quitButton?.onClick.AddListener(() => VRGuideController.Instance?.OnRequestQuit());
        confirmYesButton?.onClick.AddListener(() => VRGuideController.Instance?.OnConfirmQuit());
        confirmNoButton?.onClick.AddListener(BackToMainMenu);
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
        AddCollider(quitButton);
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
    /// Shows the confirm quit panel.
    /// </summary>
    public void ShowConfirmQuit()
    {
        SetPanels(main: false, missions: false, hint: false, confirmQuit: true);
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

    private void SetPanels(bool main, bool missions, bool hint, bool confirmQuit = false)
    {
        if (guideRootPanel != null)    guideRootPanel.SetActive(true);
        if (mainMenuPanel != null)     mainMenuPanel.SetActive(main);
        if (missionListPanel != null)  missionListPanel.SetActive(missions);
        if (hintPanel != null)         hintPanel.SetActive(hint);
        if (confirmQuitPanel != null)  confirmQuitPanel.SetActive(confirmQuit);

        Canvas.ForceUpdateCanvases();

        if (main)
        {
            AddCollider(missionsButton);
            AddCollider(hintButton);
            AddCollider(quitButton);
        }
        else if (missions)
        {
            AddCollider(missionListBackButton);
        }
        else if (hint)
        {
            AddCollider(hintBackButton);
        }
        else if (confirmQuit)
        {
            AddCollider(confirmYesButton);
            AddCollider(confirmNoButton);
        }
    }

    private void RebuildMissionList(World world)
    {
        foreach (var entry in _missionEntries)
            Destroy(entry);
        _missionEntries.Clear();

        if (world == null || missionListContainer == null) return;

        EnsureScrollView();

        // El contenedor debe controlar el ANCHO de cada fila para que el texto se ajuste
        // al panel y no se salga. Con childControlHeight + !forceExpandHeight cada fila
        // crece en alto según el texto envuelto.
        var vlg = missionListContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
        }

        foreach (Mission mission in world.Missions)
        {
            if (mission == null) continue;

            bool done = PlayerProgress.Instance != null
                && PlayerProgress.Instance.IsMissionCompleted(mission.GetMissionName());

            var entry = BuildMissionEntry(mission.GetUserGuide(), done);
            entry.transform.SetParent(missionListContainer, false);
            _missionEntries.Add(entry);
        }

        // Recalcula el layout y deja la lista arriba del todo.
        Canvas.ForceUpdateCanvases();
        if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
    }

    private void EnsureScrollView()
    {
        if (_scroll != null || missionListContainer == null) return;

        var content = (RectTransform)missionListContainer;

        var vpGO = new GameObject("MissionViewport", typeof(RectTransform), typeof(RectMask2D));
        var vp = (RectTransform)vpGO.transform;
        vp.SetParent(content.parent, false);
        vp.anchorMin = content.anchorMin;
        vp.anchorMax = content.anchorMax;
        vp.pivot = content.pivot;
        vp.sizeDelta = content.sizeDelta;
        vp.anchoredPosition = content.anchoredPosition;
        vp.SetSiblingIndex(content.GetSiblingIndex());

        content.SetParent(vp, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scroll = vpGO.AddComponent<ScrollRect>();
        _scroll.viewport = vp;
        _scroll.content = content;
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    /// <summary>
    /// Builds a single mission row entirely in code — no prefab required.
    /// Layout: [dot] [mission name]
    /// Completed missions are greyed out with strikethrough; pending are white.
    /// </summary>
    private GameObject BuildMissionEntry(string missionName, bool completed)
    {
        // ── Row ──────────────────────────────────────────────────────────────
        // Sin ancho fijo: el VerticalLayoutGroup del contenedor controla el ancho
        // (ver RebuildMissionList) y el alto crece según el texto envuelto.
        var row = new GameObject(missionName, typeof(RectTransform));

        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 10f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.padding = new RectOffset(6, 6, 2, 2);

        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = rowHeight;
        rowLE.preferredHeight = rowHeight;

        // ── Status dot ───────────────────────────────────────────────────────
        var dot = new GameObject("Dot", typeof(RectTransform));
        dot.transform.SetParent(row.transform, false);

        var dotImg = dot.AddComponent<Image>();
        dotImg.color = completed
            ? ColorCompleted
            : new Color(0.7f, 0.7f, 0.7f, 0.5f);

        var dotLayout = dot.AddComponent<LayoutElement>();
        dotLayout.minWidth = 16f;
        dotLayout.minHeight = 16f;
        dotLayout.preferredWidth = 16f;
        dotLayout.preferredHeight = 16f;
        dotLayout.flexibleWidth = 0f;

        // ── Mission name label ────────────────────────────────────────────────
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);

        var labelLayout = labelGO.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;   // ocupa el ancho restante de la fila

        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = missionName;
        label.color = completed ? ColorDimmed : ColorPending;
        label.fontStyle = completed ? FontStyles.Strikethrough : FontStyles.Normal;
        label.enableWordWrapping = true;            // texto largo baja de línea en vez de salirse
        label.enableAutoSizing = true;              // se encoge para caber en el alto fijo de la fila
        label.fontSizeMin = fontMin;
        label.fontSizeMax = fontMax;
        label.overflowMode = TextOverflowModes.Ellipsis; // solo si ni con fontMin cabe
        label.alignment = TextAlignmentOptions.MidlineLeft;

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
