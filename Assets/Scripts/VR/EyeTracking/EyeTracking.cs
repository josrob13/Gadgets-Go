using UnityEngine;
using TMPro;

public class EyeTracking : MonoBehaviour
{
    public OVREyeGaze eyeGaze;

    [Header("Attention objectives")]
    public LayerMask focusLayerMask = Physics.DefaultRaycastLayers;
    public string[] focusTags;
    public GameObject[] focusObjects;

    [Header("Tracking configuration")]
    public float minConfidence = 0.5f;
    public float distractionAlertTime = 5f;
    public float maxRayDistance = 100f;

    [Header("Optional feedback")]
    public GameObject distractionWarningUI;
    public TextMeshProUGUI distractionWarningText;
    public DialogueUI dialogueUI;
    public GameObject textBoxHighlightUI;

    [TextArea]
    public string warningMessage = "Por favor, concéntrate en los personajes o el recuadro de texto.";

    private float focusedTime;
    private float distractedTime;
    private float trackedTime;
    private float currentDistractedDuration;
    private bool warningActive;
    private int _distractionCount;

    public bool IsCurrentlyFocused { get; private set; }
    public float FocusedTime => focusedTime;
    public float DistractedTime => distractedTime;
    public float TrackedTime => trackedTime;
    public int DistractionCount => _distractionCount;
    public float DistractedPercentage => trackedTime <= 0f ? 0f : distractedTime / trackedTime * 100f;

    private float _nextDiagTime;
    private float _nextBeat;

    private void Start()
    {
        Debug.Log($"[Eye] Start — eyeGaze:{eyeGaze != null} | EyeTrackingEnabled:{(eyeGaze != null && eyeGaze.EyeTrackingEnabled)} | minConf:{minConfidence} | focusMask:{focusLayerMask.value} | alerta:{distractionAlertTime}s | dialogueUI:{dialogueUI != null} | textBoxHighlightUI:{textBoxHighlightUI != null} | warningUI:{distractionWarningUI != null}");
    }

    private void LogThrottled(string msg, float every = 2f)
    {
        if (Time.unscaledTime < _nextDiagTime) return;
        _nextDiagTime = Time.unscaledTime + every;
        Debug.Log(msg);
    }

    private void Update()
    {
        if (eyeGaze == null || !eyeGaze.EyeTrackingEnabled)
        {
            LogThrottled($"[Eye] Sin datos: eyeGaze nulo:{eyeGaze == null} | EyeTrackingEnabled:{(eyeGaze != null && eyeGaze.EyeTrackingEnabled)}");
            return;
        }
        if (eyeGaze.Confidence < minConfidence)
        {
            LogThrottled($"[Eye] Confianza baja: {eyeGaze.Confidence:F2} < {minConfidence}");
            return;
        }

        Ray gazeRay = new Ray(eyeGaze.transform.position, eyeGaze.transform.forward);
        bool hitFocus = false;
        bool hasRaycast = Physics.Raycast(gazeRay, out RaycastHit hit, maxRayDistance, Physics.DefaultRaycastLayers);

        if (hasRaycast)
        {
            hitFocus = IsFocusTarget(hit.collider.gameObject);
            Debug.DrawRay(gazeRay.origin, gazeRay.direction * hit.distance, hitFocus ? Color.green : Color.red);
        }
        else
        {
            Debug.DrawRay(gazeRay.origin, gazeRay.direction * maxRayDistance, Color.red);
        }

        string mirando = hasRaycast
        ? $"'{hit.collider.name}' (capa '{LayerMask.LayerToName(hit.collider.gameObject.layer)}')"
        : "nada";

        if (hitFocus != IsCurrentlyFocused)
        Debug.Log($"[Eye] {(hitFocus ? "ENFOCADO" : "DISTRAÍDO")} — mirando {mirando}");

        if (!hitFocus && IsCurrentlyFocused)
            _distractionCount++;

        IsCurrentlyFocused = hitFocus;
        trackedTime += Time.deltaTime;

        if (hitFocus)
        {
            focusedTime += Time.deltaTime;
            currentDistractedDuration = 0f;
            SetWarning(false);
        }
        else
        {
            distractedTime += Time.deltaTime;
            currentDistractedDuration += Time.deltaTime;

            if (currentDistractedDuration >= distractionAlertTime)
                SetWarning(true);
        }

        if (Time.unscaledTime >= _nextBeat)
        {
            _nextBeat = Time.unscaledTime + 5f;
            Debug.Log($"[Eye] latido — mirando {mirando} | foco:{hitFocus} | enfocado:{focusedTime:F1}s distraído:{distractedTime:F1}s racha:{currentDistractedDuration:F1}s | avisoActivo:{warningActive}");
        }
    }

    private bool IsFocusTarget(GameObject hitObject)
    {
        if (hitObject == null)
            return false;

        if (((1 << hitObject.layer) & focusLayerMask.value) != 0)
            return true;

        var current = hitObject.transform;
        while (current != null)
        {
            foreach (var tag in focusTags)
            {
                if (!string.IsNullOrEmpty(tag) && current.CompareTag(tag))
                    return true;
            }

            foreach (var focusObject in focusObjects)
            {
                if (focusObject != null && current.gameObject == focusObject)
                    return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void SetWarning(bool active)
    {
        if (warningActive == active) return;
        warningActive = active;

        Debug.Log($"[Eye] SetWarning({active}) — dialogueUI:{dialogueUI != null} | textBoxHighlightUI:{textBoxHighlightUI != null} | warningUI:{distractionWarningUI != null}");

        if (distractionWarningUI != null)
            distractionWarningUI.SetActive(active);

        if (dialogueUI != null)
            dialogueUI.SetHighlight(active);
        else if (textBoxHighlightUI != null)
            textBoxHighlightUI.SetActive(active);

        if (distractionWarningText != null)
            distractionWarningText.text = active ? warningMessage : string.Empty;
    }

    public void ResetTracking()
    {
        Debug.Log($"[Eye] ResetTracking — antes: tracked:{trackedTime:F1}s enfocado:{focusedTime:F1}s distraído:{distractedTime:F1}s");
        focusedTime = 0f;
        distractedTime = 0f;
        trackedTime = 0f;
        currentDistractedDuration = 0f;
        _distractionCount = 0;
        warningActive = false;
        IsCurrentlyFocused = false;
        SetWarning(false);
    }

    public float GetDistractedPercentage()
    {
        return DistractedPercentage;
    }
}
