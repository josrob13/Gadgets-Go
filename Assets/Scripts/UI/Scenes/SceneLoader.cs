using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Header("UI de Carga")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private bool skipIntroInEditor = true;

    [Header("Canvas / Fade (fallback escritorio)")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private float fadeDuration = 1.5f;

    // Fade nativo VR — se busca al inicio y tras cada carga de escena.
    private OVRScreenFade _ovrFade;

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
    }

    private void Start()
    {
        RefreshOVRFade();
    }

    // ── Punto de entrada desde el botón "Jugar" del MainMenu ─────────────────
    public void StartGame()
    {
#if UNITY_EDITOR
        if (skipIntroInEditor)
        {
            // En el editor saltamos la intro para iterar rápido.
            // Cambia "MainMenu" por la escena que prefieras para pruebas.
            LoadSceneAsync("MainMenu");
            return;
        }
#endif
        LoadSceneAsync("Intro");
    }

    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // ── 1. FADE OUT ──────────────────────────────────────────────────────
        yield return StartCoroutine(DoFadeOut());

        // ── 2. CARGA ASÍNCRONA ──────────────────────────────────────────────
        if (mainCanvas != null)    mainCanvas.gameObject.SetActive(false);
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        if (loadingScreen != null) loadingScreen.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            // Unity reporta progreso de 0 a 0.9; lo mapeamos a 0–1 para el slider.
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (loadingBar != null) loadingBar.value = progress;

            if (operation.progress >= 0.9f)
                operation.allowSceneActivation = true;

            yield return null;
        }

        if (loadingScreen != null) loadingScreen.SetActive(false);

        // Re-buscar OVRScreenFade en la nueva escena.
        // • Si OVRCameraRig es DontDestroyOnLoad → encuentra el mismo componente
        //   (ya en negro por el FadeOut anterior) → FadeIn funciona directamente.
        // • Si OVRCameraRig se recrea por escena → encuentra el nuevo componente
        //   (empieza en claro) → lo ponemos a negro instantáneamente antes del FadeIn.
        RefreshOVRFade();

        // ── 3. FADE IN ───────────────────────────────────────────────────────
        yield return StartCoroutine(DoFadeIn());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers de fade
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator DoFadeOut()
    {
        if (_ovrFade != null)
        {
            _ovrFade.FadeOut();
            yield return new WaitForSeconds(fadeDuration);
        }
        else if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            yield return StartCoroutine(FadeCanvas(0f, 1f));
        }
    }

    private IEnumerator DoFadeIn()
    {
        if (_ovrFade != null)
        {
            // Si el OVRCameraRig es per-escena, el nuevo OVRScreenFade empieza
            // con alpha = 0 (claro). Lo ponemos a negro en cero segundos y luego
            // hacemos el fade-in normal, para que la transición sea siempre suave.
            float savedFadeTime = _ovrFade.fadeTime;
            _ovrFade.fadeTime = 0f;
            _ovrFade.FadeOut();              // se aplica en el mismo frame (duration 0)
            yield return null;               // un frame para que renderice en negro
            _ovrFade.fadeTime = savedFadeTime;

            _ovrFade.FadeIn();
            yield return new WaitForSeconds(fadeDuration);
        }
        else if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvas(1f, 0f));
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator FadeCanvas(float startAlpha, float targetAlpha)
    {
        float time = 0f;
        fadeCanvasGroup.alpha = startAlpha;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    /// <summary>
    /// Busca OVRScreenFade en la escena activa. Se llama al inicio y tras
    /// cada cambio de escena para mantener la referencia actualizada.
    /// </summary>
    private void RefreshOVRFade()
    {
        _ovrFade = FindObjectOfType<OVRScreenFade>();

        if (_ovrFade != null)
            Debug.Log("[SceneLoader] OVRScreenFade detectado — fade VR nativo activo.");
        else
            Debug.Log("[SceneLoader] OVRScreenFade no encontrado — usando CanvasGroup (modo desktop).");
    }
}
