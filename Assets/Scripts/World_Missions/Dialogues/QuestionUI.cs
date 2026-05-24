using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class QuestionUI : MonoBehaviour, IVRPointerTarget
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI questionComp;
    [SerializeField] private Button[] optionButtons;
    
    [Header("VR Settings")]
    [SerializeField] private bool enableVRInteraction = true;
    [SerializeField] private OVRInput.Button pinchGestureButton = OVRInput.Button.PrimaryHandTrigger;
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    [SerializeField] private float raycastDistance = 1000f;

    public bool HasAnswered { get; private set; }
    public int SelectedIndex { get; private set; }
    /// <summary>True cuando el panel de preguntas está visible (para que VRRayPointer sepa cuándo activarse).</summary>
    public bool IsVisible => panel != null && panel.activeSelf;

    public bool IsPointerActive => IsVisible;
    public bool BlocksTriggerFallback => true;
    public void OnPointerTriggerFallback() { }

    private OVRCameraRig ovrCameraRig;
    private Transform centerEyeAnchor;
    private bool previousPinchState = false;
    private Canvas targetCanvas;
    private GraphicRaycaster graphicRaycaster;

    private void Awake()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int idx = i;
            optionButtons[i].onClick.AddListener(() => OnOptionClicked(idx));
        }

        // Colliders are sized in ShowQuestion() after the panel activates and layout runs.
        // Calling here would give (0,0) rect sizes because the Canvas hasn't laid out yet.

        // Obtener referencias VR
        ovrCameraRig = FindObjectOfType<OVRCameraRig>();
        if (ovrCameraRig != null)
        {
            centerEyeAnchor = ovrCameraRig.centerEyeAnchor;
        }

        // Obtener referencias UI
        targetCanvas = GetComponentInParent<Canvas>();
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();

        if (targetCanvas != null)
        {
            graphicRaycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        }
    }

    /// <summary>
    /// Añade un BoxCollider a cada botón para que Physics.Raycast del VRRayPointer
    /// pueda detectarlos. El collider se ajusta al tamaño del RectTransform del botón.
    /// </summary>
    private void SetupButtonColliders()
    {
        foreach (Button button in optionButtons)
        {
            if (button == null) continue;
            RectTransform rt = button.GetComponent<RectTransform>();
            if (rt == null) continue;

            BoxCollider col = button.GetComponent<BoxCollider>();
            if (col == null) col = button.gameObject.AddComponent<BoxCollider>();

            // Ajustar el collider al tamaño del botón (plano en Z)
            col.size   = new Vector3(rt.rect.width, rt.rect.height, 1f);
            col.center = Vector3.zero;
        }
    }

    /// <summary>
    /// Permite al VRRayPointer seleccionar una opción por índice directamente.
    /// </summary>
    public void SelectOption(int index)
    {
        if (index >= 0 && index < optionButtons.Length && optionButtons[index].interactable)
            OnOptionClicked(index);
    }

    public void ShowQuestion(string question, string[] options)
    {
        panel.SetActive(true);
        questionComp.text = question;
        HasAnswered = false;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(i < options.Length);
            if (i < options.Length) {
                optionButtons[i].transform.Find("DialogueText").GetComponent<TextMeshProUGUI>().text = options[i];
            }
        }

        // Size colliders now — RectTransform.rect is valid only after the panel is active
        // and layout has been rebuilt. Awake() would give (0,0) sizes.
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
        SetupButtonColliders();
    }

    private void OnOptionClicked(int idx)
    {
        SelectedIndex = idx;
        HasAnswered   = true;
    }

    public void Hide() => panel.SetActive(false);

    /// <summary>
    /// Detecta gestos VR y dispara los botones correspondientes
    /// </summary>
    private void Update()
    {
        if (!enableVRInteraction || centerEyeAnchor == null || !panel.activeSelf)
            return;

        // Detectar el gesto de pellizcar (trigger presionado)
        bool currentPinchState = OVRInput.Get(pinchGestureButton, controller);

        // Si el trigger fue presionado en este frame
        if (currentPinchState && !previousPinchState)
        {
            HandleVRPinch();
        }

        previousPinchState = currentPinchState;
    }

    /// <summary>
    /// Maneja el gesto de pellizcar en VR detectando qué botón está bajo la mirada del jugador
    /// </summary>
    private void HandleVRPinch()
    {
        Debug.Log("[QuestionUI] Gesto de pellizcar detectado");
        
        // Intentar primero con raycast gráfico (más confiable para UI)
        Button detectedButton = DetectButtonWithGraphicRaycast();
        
        // Si no funciona raycast gráfico, intentar raycast físico
        if (detectedButton == null)
            detectedButton = DetectButtonWithPhysicsRaycast();

        if (detectedButton != null && detectedButton.interactable)
        {
            Debug.Log($"[QuestionUI] Botón detectado: {detectedButton.gameObject.name}");
            
            // Obtener el índice del botón presionado
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == detectedButton)
                {
                    Debug.Log($"[QuestionUI] Ejecutando OnOptionClicked con índice {i}");
                    OnOptionClicked(i);
                    return;
                }
            }
        }
        else
        {
            Debug.LogWarning("[QuestionUI] No se detectó ningún botón bajo la mirada del jugador");
        }
    }

    /// <summary>
    /// Intenta detectar un botón usando raycast gráfico (para Canvas en Screen Space)
    /// </summary>
    private Button DetectButtonWithGraphicRaycast()
    {
        if (graphicRaycaster == null || targetCanvas == null)
            return null;

        // Para Canvas en WorldSpace, necesitamos convertir el rayo a posición en pantalla
        if (targetCanvas.renderMode == RenderMode.WorldSpace)
        {
            return null; // Usar raycast físico en su lugar
        }

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        graphicRaycaster.Raycast(eventData, results);

        foreach (var result in results)
        {
            Button button = result.gameObject.GetComponent<Button>();
            if (button != null)
                return button;
        }

        return null;
    }

    /// <summary>
    /// Intenta detectar un botón usando raycast físico
    /// </summary>
    private Button DetectButtonWithPhysicsRaycast()
    {
        Ray ray = new Ray(centerEyeAnchor.position, centerEyeAnchor.forward);
        RaycastHit hit;

        // Raycast físico para detectar colisores de los botones
        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            Button button = hit.collider.GetComponent<Button>();
            if (button != null)
                return button;

            // Si el collider no tiene Button, buscar en el padre
            button = hit.collider.GetComponentInParent<Button>();
            if (button != null)
                return button;
        }

        return null;
    }
}


