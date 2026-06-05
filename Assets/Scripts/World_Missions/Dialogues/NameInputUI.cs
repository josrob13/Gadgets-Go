using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NameInputUI : MonoBehaviour, IVRPointerTarget
{
    // IVRPointerTarget — keeps the VRRayPointer laser active while this panel is open.
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
        Debug.Log($"[NameInputUI] Start — _vrAvailable={_vrAvailable}, inputField={inputField != null}, confirmButton={confirmButton != null}");

        confirmButton?.onClick.AddListener(OnConfirmButton);

        if (inputField != null)
            inputField.onSelect.AddListener(_ =>
            {
                Debug.Log("[NameInputUI] onSelect fired — opening keyboard");
                OpenKeyboard();
            });
    }

    public void Show(string prompt, string placeholder, int maxChars)
    {
        Debug.Log($"[NameInputUI] Show — prompt='{prompt}'");
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
        Debug.Log("[NameInputUI] ActivateNextFrame — waiting one frame then focusing");
        yield return null;
        FocusInputField();
    }

    private void FocusInputField()
    {
        if (inputField == null)
        {
            Debug.LogError("[NameInputUI] FocusInputField — inputField is null!");
            return;
        }
        Debug.Log("[NameInputUI] FocusInputField — calling ActivateInputField + Select");
        inputField.ActivateInputField();
        inputField.Select();
        OpenKeyboard();
    }

    private void OpenKeyboard()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (inputField == null) return;
        string placeholderText = inputField.placeholder is TextMeshProUGUI ph ? ph.text : "";
        inputField.touchScreenKeyboard = TouchScreenKeyboard.Open(
            inputField.text,
            TouchScreenKeyboardType.Default,
            autocorrection: false,
            multiline: false,
            secure: false,
            alert: false,
            textPlaceholder: placeholderText
        );
        Debug.Log($"[NameInputUI] OpenKeyboard — keyboard status={inputField.touchScreenKeyboard?.status}");
#else
        Debug.Log("[NameInputUI] OpenKeyboard — skipped (Editor/Link, use PC keyboard to type)");
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
