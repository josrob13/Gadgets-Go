using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla final "¡Gracias por jugar!" que se muestra cuando el jugador
/// completa TODOS los mundos. <see cref="GameHandler.NextWorld"/> carga la escena
/// cuyo nombre coincide con <see cref="SceneName"/> (por defecto "ThankYou").
///
/// El componente es totalmente autocontenido: construye su propia UI WorldSpace
/// (cartel + botón "Salir") por código —siguiendo el patrón de <see cref="VRGuideUI"/>—
/// y no requiere ninguna referencia asignada en el Inspector.
///
/// Se autoinstala mediante <see cref="SceneManager.sceneLoaded"/>: en cuanto se carga
/// la escena final, se crea automáticamente un GameObject con este componente. Por eso
/// la escena "ThankYou" solo necesita el rig de VR (cámara + VRRayPointer), que se hereda
/// al duplicarla a partir de MainMenu. También funciona si se añade manualmente a una
/// escena con rig VR.
///
/// Interacción:
///   • VR  → el VRRayPointer detecta el botón por su BoxCollider (raycast físico) y por
///           el GraphicRaycaster del canvas. Implementa IVRPointerTarget para activar el
///           puntero aunque la escena no traiga otro target.
///   • Escritorio/Editor → clic de ratón vía GraphicRaycaster + EventSystem.
/// </summary>
public class ThankYouScreen : MonoBehaviour, IVRPointerTarget
{
    /// <summary>Nombre de la escena final. Debe coincidir con el campo endSceneName de GameHandler.</summary>
    public const string SceneName = "ThankYou";

    // ─── Autoinstalación por nombre de escena ───────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterBootstrap()
    {
        // Evita doble suscripción si el dominio no se recargó entre Play y Play.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName) return;
        if (FindObjectOfType<ThankYouScreen>() != null) return; // ya colocado manualmente

        var go = new GameObject(nameof(ThankYouScreen));
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<ThankYouScreen>();
    }

    // ─── IVRPointerTarget ───────────────────────────────────────────────────────
    // El puntero VR está siempre activo en esta pantalla (no hay nada más que hacer
    // que pulsar "Salir"). No bloqueamos el fallback del gatillo.
    public bool IsPointerActive => true;
    public bool BlocksTriggerFallback => false;
    public void OnPointerTriggerFallback() { }

    // ─── Ciclo de vida ──────────────────────────────────────────────────────────

    private void Start()
    {
        HideOtherSceneCanvases();
        EnsureEventSystem();
        BuildUI();
    }

    /// <summary>
    /// Desactiva los canvas que vengan en la escena (p. ej. el menú heredado al
    /// duplicar MainMenu) para que solo se vea el cartel de agradecimiento.
    /// Solo toca canvas de la escena activa; ignora los DontDestroyOnLoad.
    /// </summary>
    private void HideOtherSceneCanvases()
    {
        Scene active = gameObject.scene;
        foreach (var canvas in FindObjectsOfType<Canvas>(true))
        {
            if (canvas == null) continue;
            if (canvas.transform.IsChildOf(transform)) continue; // el nuestro
            if (canvas.gameObject.scene != active) continue;      // persistente / otra escena
            canvas.gameObject.SetActive(false);
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    // ─── Construcción de la UI ──────────────────────────────────────────────────

    private void BuildUI()
    {
        Camera cam = Camera.main;

        // ── Canvas raíz (WorldSpace) ──────────────────────────────────────────
        var canvasGO = new GameObject("ThankYouCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasRT = canvas.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(1000f, 700f);
        canvasRT.localScale = Vector3.one * 0.0016f; // ≈1.6 m de ancho

        // Colocar ~2.2 m enfrente de la cámara, a su altura, mirándola.
        if (cam != null)
        {
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            fwd.Normalize();

            canvasGO.transform.position = cam.transform.position + fwd * 2.2f;
            canvasGO.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            canvas.worldCamera = cam; // habilita el clic de ratón (fallback escritorio)
        }
        else
        {
            canvasGO.transform.position = new Vector3(0f, 1.6f, 2.2f);
        }

        // ── Fondo ─────────────────────────────────────────────────────────────
        RectTransform bg = CreateRect("Background", canvasRT);
        Stretch(bg);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);

        // ── Cartel "¡Gracias por jugar!" ──────────────────────────────────────
        RectTransform titleRT = CreateRect("Title", bg);
        titleRT.anchorMin = new Vector2(0.08f, 0.42f);
        titleRT.anchorMax = new Vector2(0.92f, 0.92f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        var title = titleRT.gameObject.AddComponent<TextMeshProUGUI>();
        title.text = "¡Gracias por jugar!";
        title.fontSize = 96f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        title.enableWordWrapping = true;

        // ── Botón "Salir" ─────────────────────────────────────────────────────
        RectTransform btnRT = CreateRect("QuitButton", bg);
        btnRT.anchorMin = new Vector2(0.30f, 0.12f);
        btnRT.anchorMax = new Vector2(0.70f, 0.30f);
        btnRT.offsetMin = Vector2.zero;
        btnRT.offsetMax = Vector2.zero;

        var btnImg = btnRT.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0.82f, 0.24f, 0.24f, 1f);

        var btn = btnRT.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(QuitApplication);

        RectTransform lblRT = CreateRect("Label", btnRT);
        Stretch(lblRT);
        var lbl = lblRT.gameObject.AddComponent<TextMeshProUGUI>();
        lbl.text = "Salir";
        lbl.fontSize = 48f;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color = Color.white;

        // Collider para que el VRRayPointer detecte el botón por raycast físico.
        Canvas.ForceUpdateCanvases();
        AddButtonCollider(btn);
    }

    // ─── Acción de salir ────────────────────────────────────────────────────────

    private void QuitApplication()
    {
        Debug.Log("[ThankYouScreen] El usuario pulsó 'Salir' — cerrando la aplicación.");

        // UIManager.GameQuit() guarda las analíticas y maneja la salida en el Editor.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.GameQuit();
            return;
        }

        AnalyticsManager.Instance?.SaveAnalytics();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── Helpers de UI ──────────────────────────────────────────────────────────

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Añade un BoxCollider dimensionado según el RectTransform del botón para que
    /// Physics.Raycast (usado por VRRayPointer) pueda detectarlo en WorldSpace.
    /// Mismo patrón que VRGuideUI / UIButtonColliderSync.
    /// </summary>
    private static void AddButtonCollider(Button btn)
    {
        if (btn == null) return;
        var rt = btn.GetComponent<RectTransform>();
        if (rt == null) return;

        var col = btn.GetComponent<BoxCollider>() ?? btn.gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(rt.rect.width, rt.rect.height, 1f);
        col.center = Vector3.zero;
    }
}
