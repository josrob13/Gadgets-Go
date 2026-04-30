using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DialogueUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI textComp;
    [HideInInspector] public bool NextPressed { get; set; } = false;

    [Header("VR Settings")]
    [SerializeField] private bool enableVRInteraction = true;
    [SerializeField] private OVRInput.Button continueGestureButton = OVRInput.Button.PrimaryHandTrigger;
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;

    private OVRCameraRig ovrCameraRig;
    private Transform centerEyeAnchor;
    private bool previousGestureState = false;

    private void Start()
    {
        // Obtener referencias VR
        ovrCameraRig = FindObjectOfType<OVRCameraRig>();
        if (ovrCameraRig != null)
        {
            centerEyeAnchor = ovrCameraRig.centerEyeAnchor;
        }
    }

    public void Show() => panel.SetActive(true);
    public void Hide() => panel.SetActive(false);

    public void OnNextButton() => NextPressed = true;

    public void SetText(string txt) => textComp.text = txt;

    public TextMeshProUGUI GetTextComponent() => textComp;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("DialogueUI: OnPointerClick called");
        NextPressed = true;
    }

    /// <summary>
    /// Detecta gestos VR para continuar al siguiente diálogo
    /// </summary>
    private void Update()
    {
        if (!enableVRInteraction || centerEyeAnchor == null || !panel.activeSelf)
            return;

        // Detectar el gesto de pellizcar (trigger presionado)
        bool currentGestureState = OVRInput.Get(continueGestureButton, controller);

        // Si el trigger fue presionado en este frame
        if (currentGestureState && !previousGestureState)
        {
            Debug.Log("[DialogueUI] Gesto VR detectado para continuar diálogo");
            NextPressed = true;
        }

        previousGestureState = currentGestureState;
    }
}

