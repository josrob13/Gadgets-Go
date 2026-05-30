using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject normalUI;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float defaultFadeDuration = 1.5f;

    // ── Referencia al fade nativo de OVR (se busca automáticamente en Start) ──
    private OVRScreenFade ovrFade;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (fadeCanvasGroup == null)
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
    }

    private void Start()
    {
        // OVRScreenFade está en la cámara principal del rig VR.
        // Buscarlo aquí (Start) permite que el OVRCameraRig esté ya inicializado.
        ovrFade = FindObjectOfType<OVRScreenFade>();
        if (ovrFade != null)
            Debug.Log("[UIManager] OVRScreenFade detectado: se usará fade VR nativo.");
        else
            Debug.Log("[UIManager] OVRScreenFade no encontrado: se usará CanvasGroup (modo desktop).");
    }

    public void GameQuit()
    {
        Debug.Log("Game is quitting — saving analytics...");
        AnalyticsManager.Instance?.SaveAnalytics();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Fade  (VR → OVRScreenFade | Desktop → CanvasGroup)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Oscurece la pantalla hasta negro (0 → 1 en alpha).
    /// En VR usa OVRScreenFade para cubrir ambos ojos por completo.
    /// </summary>
    public IEnumerator FadeOut(float duration)
    {
        if (ovrFade != null)
        {
            // OVRScreenFade.FadeOut() reproduce la animación internamente;
            // esperamos su duración antes de continuar.
            ovrFade.FadeOut();
            yield return new WaitForSeconds(duration);
        }
        else
        {
            // Fallback: Canvas 2D (modo escritorio)
            float time = 0f;
            fadeCanvasGroup.alpha = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(time / duration);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }
    }

    /// <summary>
    /// Aclara la pantalla desde negro (1 → 0 en alpha).
    /// En VR usa OVRScreenFade para cubrir ambos ojos por completo.
    /// </summary>
    public IEnumerator FadeIn(float duration)
    {
        if (ovrFade != null)
        {
            ovrFade.FadeIn();
            yield return new WaitForSeconds(duration);
        }
        else
        {
            // Fallback: Canvas 2D (modo escritorio)
            float time = 0f;
            fadeCanvasGroup.alpha = 1f;

            while (time < duration)
            {
                time += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (time / duration));
                yield return null;
            }

            fadeCanvasGroup.alpha = 0f;
        }
    }
}

