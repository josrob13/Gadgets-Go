using UnityEngine;
using TMPro;

public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private TextMeshProUGUI interactionTextMeshProGUI;

    private IInteractable _current;
    private float _nextBeat;

    private void Awake()
    {
        Debug.Log($"[InteractUI] Awake — activeInHierarchy:{gameObject.activeInHierarchy}");
    }

    private void Start()
    {
        Debug.Log($"[InteractUI] Start — container:{container != null} | playerInteraction:{playerInteraction != null} | text:{interactionTextMeshProGUI != null} | Camera.main:{Camera.main != null}");
    }

    private void Update()
    {
        IInteractable i = playerInteraction.GetInteractableObject();

        if (i != _current)
        {
            _current = i;
            Debug.Log(i != null
                ? $"[InteractUI] Interactuable cerca: '{i.GetInteractText()}' en {i.GetTransform().position}"
                : "[InteractUI] Ya no hay interactuable cerca.");
        }

        if (i != null)
            Show(i);
        else
            Hide();

        Heartbeat(i);
    }

    private void Show(IInteractable interactable)
    {
        interactionTextMeshProGUI.text = interactable.GetInteractText();
        container.SetActive(true);
    }
    
    private void Hide()
    {
        container.SetActive(false);
    }

    private void Heartbeat(IInteractable i)
    {
        if (Time.unscaledTime < _nextBeat) return;
        _nextBeat = Time.unscaledTime + 3f;

        string view = "sin cámara";
        if (container != null && Camera.main != null)
        {
            Vector3 vp = Camera.main.WorldToViewportPoint(container.transform.position);
            view = $"viewport=({vp.x:F2},{vp.y:F2}) z={vp.z:F2}m";
        }

        Debug.Log($"[InteractUI] latido — cerca:{(i != null ? i.GetInteractText() : "ninguno")} | activeInHierarchy:{container != null && container.activeInHierarchy} | {view}");
        playerInteraction.GetInteractableObject(verbose: true);
    }

    private void OnEnable()
    {
        Debug.Log("[InteractUI] OnEnable");
    }

    private void OnDisable()
    {
        Debug.Log("[InteractUI] OnDisable — desactivado desde:\n" + System.Environment.StackTrace);
    }
}
