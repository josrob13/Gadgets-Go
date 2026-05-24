using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DialogueUI : MonoBehaviour, IPointerClickHandler, IVRPointerTarget
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI textComp;
    [SerializeField] private GameObject highlightFrame;
    [HideInInspector] public bool NextPressed { get; set; } = false;
    public bool IsVisible => panel != null && panel.activeSelf;

    public bool IsPointerActive => IsVisible;
    public bool BlocksTriggerFallback => false;
    public void OnPointerTriggerFallback() => NextPressed = true;

    [Header("VR Settings")]
    [SerializeField] private bool enableVRInteraction = false;
    [SerializeField] private OVRInput.Button continueGestureButton = OVRInput.Button.PrimaryIndexTrigger;
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;

    private OVRCameraRig ovrCameraRig;
    private Transform centerEyeAnchor;
    private bool previousGestureState = false;

    private void Start()
    {
        ovrCameraRig = FindObjectOfType<OVRCameraRig>();
        if (ovrCameraRig != null)
        {
            centerEyeAnchor = ovrCameraRig.centerEyeAnchor;
            enableVRInteraction = true;
        }
    }

    public void Show() => panel.SetActive(true);
    public void Hide() => panel.SetActive(false);

    public void OnNextButton() => NextPressed = true;

    public void SetText(string txt) => textComp.text = txt;

    public TextMeshProUGUI GetTextComponent() => textComp;

    public void SetHighlight(bool active)
    {
        if (highlightFrame != null)
            highlightFrame.SetActive(active);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("DialogueUI: OnPointerClick called");
        NextPressed = true;
    }

    private void Update()
    {
        if (!enableVRInteraction || centerEyeAnchor == null || !panel.activeSelf)
            return;

        bool currentGestureState = OVRInput.Get(continueGestureButton, controller);

        if (currentGestureState && !previousGestureState)
        {
            Debug.Log("[DialogueUI] Gesto VR detectado para continuar diálogo");
            NextPressed = true;
        }

        previousGestureState = currentGestureState;
    }
}

