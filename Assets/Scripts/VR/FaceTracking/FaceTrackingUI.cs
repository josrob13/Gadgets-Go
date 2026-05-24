using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the "Take a Break" suggestion panel shown when facial discomfort is detected.
/// Positions itself in World Space in front of the player's eyes and fades in/out smoothly.
/// Listens to FaceTrackingManager events and exposes a "ready to continue" button callback.
/// </summary>
public class FaceTrackingUI : MonoBehaviour, IVRPointerTarget
{
    [Header("Panel References")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    [SerializeField] private VRGrabbableCanvas grabbableCanvas;

    [Header("World Space Positioning")]
    [SerializeField] private float distanceFromPlayer = 1.5f;
    [SerializeField] private float heightOffset = 0f;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Content")]
    [SerializeField] private string title = "Tómate un descanso";
    [TextArea]
    [SerializeField] private string body =
        "Hemos notado que podrías estar sintiéndote estresado/a.\nTómate un momento para respirar.\n\nCuando estés listo/a, pulsa el botón de abajo.";
    [SerializeField] private string buttonLabel = "Estoy listo/a para continuar";

    [Header("VR Input")]
    [SerializeField] private OVRInput.Button confirmButton = OVRInput.Button.PrimaryIndexTrigger;
    [SerializeField] private OVRInput.Controller confirmController = OVRInput.Controller.RTouch;

    private OVRCameraRig _ovrRig;
    private Transform _eyeAnchor;
    private Coroutine _fadeCoroutine;
    private bool _isVisible;

    public bool IsVisible => _isVisible;

    public bool IsPointerActive => IsVisible;
    public bool BlocksTriggerFallback => true;
    public void OnPointerTriggerFallback() { }

    private void Awake()
    {
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        if (readyButtonText != null) readyButtonText.text = buttonLabel;

        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyButtonClicked);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        _ovrRig = FindFirstObjectByType<OVRCameraRig>();
        if (_ovrRig != null)
            _eyeAnchor = _ovrRig.centerEyeAnchor;

        if (FaceTrackingManager.Instance != null)
            FaceTrackingManager.Instance.OnDiscomfortStateChanged += OnDiscomfortStateChanged;
        else
            Debug.LogWarning("[FaceTrackingUI] FaceTrackingManager no encontrado — la UI no responderá a eventos automáticamente.");
    }

    private void OnDestroy()
    {
        if (FaceTrackingManager.Instance != null)
            FaceTrackingManager.Instance.OnDiscomfortStateChanged -= OnDiscomfortStateChanged;

        if (readyButton != null)
            readyButton.onClick.RemoveListener(OnReadyButtonClicked);
    }

    private void Update()
    {
        if (!_isVisible) return;
        if (OVRInput.GetDown(confirmButton, confirmController))
            OnReadyButtonClicked();
    }

    public void ShowBreakSuggestion(bool show)
    {
        _isVisible = show;

        if (show)
        {
            PositionInFrontOfPlayer();
            Time.timeScale = 0f;
            grabbableCanvas?.SetGrabbingEnabled(true);
        }
        else
        {
            Time.timeScale = 1f;
            grabbableCanvas?.SetGrabbingEnabled(false);
        }

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadePanel(show));
    }

    private void OnDiscomfortStateChanged(bool isDiscomfort)
    {
        ShowBreakSuggestion(isDiscomfort);
    }

    private void OnReadyButtonClicked()
    {
        Debug.Log("[FaceTrackingUI] El usuario ha confirmado que está listo para continuar.");

        if (FaceTrackingManager.Instance != null)
            FaceTrackingManager.Instance.OnUserResumed();
        else
            ShowBreakSuggestion(false);
    }

    private void PositionInFrontOfPlayer()
    {
        if (_eyeAnchor == null) return;

        Vector3 forward = _eyeAnchor.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 targetPosition = _eyeAnchor.position
            + forward * distanceFromPlayer
            + Vector3.up * heightOffset;

        transform.position = targetPosition;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private IEnumerator FadePanel(bool show)
    {
        float startAlpha = panelCanvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;
        float elapsed = 0f;

        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            yield return null;
        }

        panelCanvasGroup.alpha = endAlpha;

        if (show)
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        _fadeCoroutine = null;
    }
}
