using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NameInputUI : MonoBehaviour, IVRPointerTarget
{
    public bool IsPointerActive => panel != null && panel.activeSelf;
    public bool BlocksTriggerFallback => false;
    public void OnPointerTriggerFallback() => FocusInputField();

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI promptLabel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button confirmButton;

    [Header("VR Settings")]
    [Tooltip("VR button that acts as confirm (e.g. A button on right controller).")]
    [SerializeField] private OVRInput.Button confirmVRButton = OVRInput.Button.One;
    [SerializeField] private OVRInput.Controller vrController = OVRInput.Controller.RTouch;

    public bool HasConfirmed { get; private set; }
    public string EnteredName => inputField != null ? inputField.text.Trim() : string.Empty;

    private bool _vrAvailable;

    private void Start()
    {
        _vrAvailable = FindObjectOfType<OVRCameraRig>() != null;

        confirmButton?.onClick.AddListener(OnConfirmButton);

        if (inputField != null)
            inputField.onSelect.AddListener(_ => OpenKeyboard());
    }

    public void Show(string prompt, string placeholder, int maxChars)
    {
        HasConfirmed = false;

        if (promptLabel != null) promptLabel.text = prompt;

        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.characterLimit = maxChars;

            if (inputField.placeholder is TextMeshProUGUI placeholderTmp)
                placeholderTmp.text = placeholder;
        }

        if (panel != null) panel.SetActive(true);

        SetupConfirmButtonCollider();
        StartCoroutine(ActivateNextFrame());
    }

    private IEnumerator ActivateNextFrame()
    {
        yield return null;
        FocusInputField();
    }

    private void FocusInputField()
    {
        if (inputField == null) return;
        inputField.ActivateInputField();
        inputField.Select();
        OpenKeyboard();
    }

    private void OpenKeyboard()
    {
        // In VR the keyboard is managed by VRRayPointer (which syncs text via
        // a Coroutine and never touches inputField.touchScreenKeyboard).
        // Assigning inputField.touchScreenKeyboard here would make TMP_InputField
        // poll .status every frame, causing NullReferenceException on Quest.
        if (_vrAvailable) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (inputField == null) return;
        string placeholderText = inputField.placeholder is TextMeshProUGUI ph ? ph.text : "";
        // Fire-and-forget: do NOT assign to inputField.touchScreenKeyboard so
        // TMP_InputField never polls .status (which crashes on Quest/OpenXR).
        TouchScreenKeyboard.Open(
            inputField.text,
            TouchScreenKeyboardType.Default,
            autocorrection: false,
            multiline: false,
            secure: false,
            alert: false,
            textPlaceholder: placeholderText
        );
#endif
    }

    private void SetupConfirmButtonCollider()
    {
        if (confirmButton == null) return;
        RectTransform rt = confirmButton.GetComponent<RectTransform>();
        if (rt == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());

        BoxCollider col = confirmButton.GetComponent<BoxCollider>();
        if (col == null) col = confirmButton.gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(rt.rect.width, rt.rect.height, 1f);
        col.center = Vector3.zero;
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        inputField?.DeactivateInputField();
    }

    public void OnConfirmButton()
    {
        if (!string.IsNullOrWhiteSpace(EnteredName))
            HasConfirmed = true;
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            OnConfirmButton();

        if (_vrAvailable && OVRInput.GetDown(confirmVRButton, vrController))
            OnConfirmButton();
    }
}
