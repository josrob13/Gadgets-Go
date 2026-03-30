using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject normalUI;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float defaultFadeDuration = 1.5f;

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

    public void GamePause()
    {
        pauseMenuUI.SetActive(true);
        normalUI.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void GameResume()
    {
        pauseMenuUI.SetActive(false);
        normalUI.SetActive(true);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GameQuit()
    {
        Debug.Log("Game is quitting...");
        UnityEditor.EditorApplication.isPlaying = false; // Detiene el modo Play en el editor
        Application.Quit();
    }

    public IEnumerator FadeOut(float duration)
    {
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
    
    public IEnumerator FadeIn(float duration)
    {
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
