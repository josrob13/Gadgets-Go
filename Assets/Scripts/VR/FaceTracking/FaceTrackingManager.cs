using System;
using UnityEngine;

/// <summary>
/// Monitors facial blendshapes via OVRFaceExpressions to detect user discomfort
/// (stress, frustration, overstimulation) during gameplay.
/// Fires OnDiscomfortStateChanged when sustained discomfort is detected or resolved.
/// Fully modular — independent from the dialogue system.
/// </summary>
public class FaceTrackingManager : MonoBehaviour
{
    public static FaceTrackingManager Instance { get; private set; }

    [Header("OVR Reference")]
    [SerializeField] private OVRFaceExpressions faceExpressions;

    [Header("Detection Thresholds")]
    [SerializeField, Range(0f, 1f)] private float scoreThreshold = 0.15f;
    [SerializeField] private float discomfortDurationThreshold = 3.5f;
    [SerializeField] private float resumeCooldown = 15f;

    [Header("Blendshape Weights")]
    [SerializeField, Range(0f, 1f)] private float browLowererWeight = 1.0f;
    [SerializeField, Range(0f, 1f)] private float eyeWideWeight = 0.8f;
    [SerializeField, Range(0f, 1f)] private float lipStretchWeight = 0.6f;

    /// <summary>Fired when discomfort state changes. true = discomfort onset, false = resolved.</summary>
    public event Action<bool> OnDiscomfortStateChanged;

    /// <summary>Whether discomfort is currently active.</summary>
    public bool IsDiscomfortActive { get; private set; }

    /// <summary>Current weighted discomfort score, range 0-1.</summary>
    public float DiscomfortScore { get; private set; }

    /// <summary>Seconds of continuous discomfort accumulated so far.</summary>
    public float DiscomfortTimer { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float debugLogInterval = 1f;

    private bool _trackingAvailable;
    private bool _inCooldown;
    private float _cooldownTimer;
    private float _debugTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (faceExpressions == null)
            faceExpressions = FindObjectOfType<OVRFaceExpressions>();

        _trackingAvailable = faceExpressions != null;

        if (!_trackingAvailable)
            Debug.LogWarning("[FaceTrackingManager] OVRFaceExpressions no encontrado — el face tracking está desactivado.");
        else
            Debug.Log("[FaceTrackingManager] Inicializado correctamente.");
    }

    private void Update()
    {
        if (!_trackingAvailable) return;

        if (_inCooldown)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
            {
                _inCooldown = false;
                Debug.Log("[FaceTrackingManager] Cooldown finalizado. Reanudando detección de incomodidad.");
            }
            return;
        }

        if (!faceExpressions.FaceTrackingEnabled || !faceExpressions.ValidExpressions)
        {
            if (debugMode)
            {
                _debugTimer += Time.deltaTime;
                if (_debugTimer >= debugLogInterval)
                {
                    _debugTimer = 0f;
                    Debug.Log($"[FaceTrackingManager] FaceTrackingEnabled={faceExpressions.FaceTrackingEnabled} | ValidExpressions={faceExpressions.ValidExpressions} — esperando datos válidos...");
                }
            }
            TryClearDiscomfort();
            return;
        }

        DiscomfortScore = ComputeDiscomfortScore();

        if (debugMode)
        {
            _debugTimer += Time.deltaTime;
            if (_debugTimer >= debugLogInterval)
            {
                _debugTimer = 0f;
                TryGetBlend(OVRFaceExpressions.FaceExpression.BrowLowererL, out float dBrowL);
                TryGetBlend(OVRFaceExpressions.FaceExpression.BrowLowererR, out float dBrowR);
                TryGetBlend(OVRFaceExpressions.FaceExpression.UpperLidRaiserL, out float dLidL);
                TryGetBlend(OVRFaceExpressions.FaceExpression.UpperLidRaiserR, out float dLidR);
                TryGetBlend(OVRFaceExpressions.FaceExpression.LipStretcherL, out float dLipL);
                TryGetBlend(OVRFaceExpressions.FaceExpression.LipStretcherR, out float dLipR);
                Debug.Log($"[FaceTrackingManager] Brow=({dBrowL:F2},{dBrowR:F2}) | Lid=({dLidL:F2},{dLidR:F2}) | Lip=({dLipL:F2},{dLipR:F2}) | Score={DiscomfortScore:F2} (umbral={scoreThreshold:F2}) | Timer={DiscomfortTimer:F1}s");
            }
        }

        if (DiscomfortScore >= scoreThreshold)
        {
            DiscomfortTimer += Time.deltaTime;

            if (!IsDiscomfortActive && DiscomfortTimer >= discomfortDurationThreshold)
            {
                IsDiscomfortActive = true;
                Debug.Log($"[FaceTrackingManager] Incomodidad detectada. Puntuación={DiscomfortScore:F2} — disparando evento a {OnDiscomfortStateChanged?.GetInvocationList().Length ?? 0} suscriptor(es).");
                OnDiscomfortStateChanged?.Invoke(true);
            }
        }
        else
        {
            if (!IsDiscomfortActive)
                DiscomfortTimer = Mathf.Max(0f, DiscomfortTimer - Time.deltaTime);
        }
    }

    /// <summary>
    /// Called when the user confirms they are ready to continue after a break.
    /// Resets discomfort state and starts a cooldown to avoid immediate re-triggering.
    /// </summary>
    public void OnUserResumed()
    {
        TryClearDiscomfort();
        _inCooldown = true;
        _cooldownTimer = resumeCooldown;
        Debug.Log($"[FaceTrackingManager] El usuario ha reanudado. Cooldown iniciado ({resumeCooldown}s).");
    }

    /// <summary>Computes a weighted discomfort score [0-1] from the monitored blendshapes.</summary>
    private float ComputeDiscomfortScore()
    {
        float totalWeight = 0f;
        float weightedSum = 0f;

        if (TryGetBlend(OVRFaceExpressions.FaceExpression.BrowLowererL, out float browL) &&
            TryGetBlend(OVRFaceExpressions.FaceExpression.BrowLowererR, out float browR))
        {
            weightedSum += (browL + browR) * 0.5f * browLowererWeight;
            totalWeight += browLowererWeight;
        }

        if (TryGetBlend(OVRFaceExpressions.FaceExpression.UpperLidRaiserL, out float lidL) &&
            TryGetBlend(OVRFaceExpressions.FaceExpression.UpperLidRaiserR, out float lidR))
        {
            weightedSum += (lidL + lidR) * 0.5f * eyeWideWeight;
            totalWeight += eyeWideWeight;
        }

        if (TryGetBlend(OVRFaceExpressions.FaceExpression.LipStretcherL, out float lipL) &&
            TryGetBlend(OVRFaceExpressions.FaceExpression.LipStretcherR, out float lipR))
        {
            weightedSum += (lipL + lipR) * 0.5f * lipStretchWeight;
            totalWeight += lipStretchWeight;
        }

        return totalWeight > 0f ? weightedSum / totalWeight : 0f;
    }

    /// <summary>Safely reads a single blendshape value from OVRFaceExpressions.</summary>
    private bool TryGetBlend(OVRFaceExpressions.FaceExpression expression, out float value)
    {
        value = 0f;
        try
        {
            value = faceExpressions[expression];
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FaceTrackingManager] No se pudo leer el blendshape {expression}: {e.Message}");
            return false;
        }
    }

    /// <summary>Clears active discomfort and fires the resolved event if needed.</summary>
    private void TryClearDiscomfort()
    {
        if (IsDiscomfortActive)
        {
            IsDiscomfortActive = false;
            Debug.Log("[FaceTrackingManager] Incomodidad resuelta.");
            OnDiscomfortStateChanged?.Invoke(false);
        }
        DiscomfortTimer = 0f;
        DiscomfortScore = 0f;
    }
}
