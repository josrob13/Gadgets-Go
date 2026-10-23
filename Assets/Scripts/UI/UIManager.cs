using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

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
        Debug.Log("Game is quitting — saving...");
        GameHandler.Instance?.SaveFull();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public IEnumerator FadeOut(float duration) => Fade(fadeOut: true, duration);
    public IEnumerator FadeIn(float duration)  => Fade(fadeOut: false, duration);

    private IEnumerator Fade(bool fadeOut, float duration)
    {
        var fade = GetOvrFade();
        if (fade == null)
        {
            Debug.LogWarning("[UIManager] OVRScreenFade no encontrado: se omite el fundido.");
            yield return new WaitForSeconds(duration);
            yield break;
        }

        if (fadeOut) fade.FadeOut(); else fade.FadeIn();
        yield return new WaitForSeconds(duration);
    }

    private OVRScreenFade GetOvrFade()
    {
        if (ovrFade == null)
            ovrFade = FindObjectOfType<OVRScreenFade>();
        return ovrFade;
    }
}

