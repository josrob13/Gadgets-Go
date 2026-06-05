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
    [SerializeField] private bool skipIntroInEditor = true; // Tu bypass para pruebas

    [Header("Canvas, Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private float fadeDuration = 1.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // Llama a este método desde el evento OnClick() del botón "Jugar"
    public void StartGame()
    {
#if UNITY_EDITOR
        if (skipIntroInEditor)
        {
            LoadSceneAsync("GameWorld");
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
        if (fadeCanvasGroup != null)
        {
            Debug.Log("[SceneLoader] Iniciando transición de escena con fade out...");
            fadeCanvasGroup.blocksRaycasts = true;
            yield return StartCoroutine(Fade(0f, 1f));
        }
        else
        {
            Debug.LogWarning("[SceneLoader] No se ha asignado un CanvasGroup para el fade. La transición será instantánea.");
        }

        if (mainCanvas != null) mainCanvas.gameObject.SetActive(false);
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        if (loadingScreen != null) loadingScreen.SetActive(true);
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false; 

        while (!operation.isDone)
        {
            // Unity carga la escena del 0 al 0.9. Mapeamos eso del 0 al 1 para el Slider.
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (loadingBar != null) loadingBar.value = progress;

            // Cuando la carga llega al 90%, significa que está lista para mostrarse
            if (operation.progress >= 0.9f)
            {
                // Aquí podrías añadir un mensaje de "Pulsa cualquier tecla para continuar"
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        if (loadingScreen != null) loadingScreen.SetActive(false);
        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(1f, 0f));
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator Fade(float startAlpha, float targetAlpha)
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
}