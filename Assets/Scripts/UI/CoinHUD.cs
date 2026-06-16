using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HUD de monedas (SpyCoins) siempre visible y CÓMODO en VR.
///
/// Comodidad: en lugar de quedar fijado rígidamente a la cámara (lo que provoca
/// fatiga/mareo en VR), sigue la cabeza con un "lazy-follow" suave —mismo patrón que
/// <see cref="CompanionLazyFollow"/>— reposando en la zona baja-derecha del campo de
/// visión, fuera del foco central y sin obstruir el diálogo (centro) ni el compañero
/// (izquierda).
///
/// Se autoinstala por escena vía <see cref="SceneManager.sceneLoaded"/>: aparece en
/// cualquier escena jugable (todas menos las de <see cref="ExcludedScenes"/>) y se
/// reconstruye en cada una, tomando el OVRCameraRig fresco —el rig es por-escena, no
/// persistente—. El número de monedas vive en el singleton persistente
/// <see cref="PlayerInventory"/> y se actualiza por su evento OnSpyCoinsChanged.
///
/// También puede colocarse manualmente en un GameObject de la escena para ajustar el
/// offset/velocidad desde el Inspector; en ese caso el autoinstalador no duplica el HUD.
/// </summary>
public class CoinHUD : MonoBehaviour
{
    /// <summary>Escenas donde NO se muestra el HUD (menús / flujo, no jugables).</summary>
    private static readonly string[] ExcludedScenes = { "MainMenu", "Intro", "ThankYou", "SampleScene" };

    // ─── Autoinstalación por escena ─────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterBootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (System.Array.IndexOf(ExcludedScenes, scene.name) >= 0) return;
        if (FindObjectOfType<CoinHUD>() != null) return; // ya colocado manualmente

        var go = new GameObject(nameof(CoinHUD));
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<CoinHUD>();
    }

    // ─── Ajustes de seguimiento (comodidad VR) ──────────────────────────────────

    [Header("Lazy-follow (comodidad VR)")]
    [SerializeField] private float positionSpeed = 4f;
    [SerializeField] private float rotationSpeed = 6f;
    [Tooltip("X = lado (positivo = derecha del jugador), Y = vertical (negativo = bajo el ojo), Z = adelante (m).")]
    [SerializeField] private Vector3 offset = new Vector3(0.26f, -0.27f, 0.70f);

    private Transform _eye;
    private TextMeshProUGUI _countText;
    private bool _subscribed;

    private static Sprite _coinSprite;

    // ─── Ciclo de vida ──────────────────────────────────────────────────────────

    private void Start()
    {
        AcquireEye();
        BuildUI();
        TrySubscribe();
        RefreshCount();

        // Colocación inicial sin lerp para que no "vuele" desde el origen al aparecer.
        if (_eye != null)
        {
            Vector3 fwd = HorizontalForward();
            transform.position = ComputeTarget(fwd);
            transform.rotation = FaceRotation(fwd);
        }
    }

    private void OnDestroy()
    {
        if (_subscribed && PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnSpyCoinsChanged -= SetCount;
    }

    // PlayerInventory es un singleton persistente que normalmente ya existe, pero si se
    // entra directamente a una escena de mundo puede crearse justo después: nos
    // suscribimos en cuanto esté disponible.
    private void TrySubscribe()
    {
        if (_subscribed || PlayerInventory.Instance == null) return;
        PlayerInventory.Instance.OnSpyCoinsChanged += SetCount;
        _subscribed = true;
        RefreshCount();
    }

    private void Update()
    {
        if (!_subscribed) TrySubscribe();

        if (_eye == null)
        {
            AcquireEye();
            if (_eye == null) return;
        }

        Vector3 fwd = HorizontalForward();
        transform.position = Vector3.Lerp(transform.position, ComputeTarget(fwd), positionSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, FaceRotation(fwd), rotationSpeed * Time.deltaTime);
    }

    // ─── Seguimiento ────────────────────────────────────────────────────────────

    private void AcquireEye()
    {
        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
        _eye = rig != null ? rig.centerEyeAnchor
                           : (Camera.main != null ? Camera.main.transform : null);
    }

    private Vector3 ComputeTarget(Vector3 fwd)
    {
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        return _eye.position
             + fwd * offset.z
             + right * offset.x
             + Vector3.up * offset.y;
    }

    // El canvas mira en la dirección de la vista (+Z hacia donde mira el jugador) para
    // que el texto se lea de frente.
    private Quaternion FaceRotation(Vector3 fwd) => Quaternion.LookRotation(fwd, Vector3.up);

    private Vector3 HorizontalForward()
    {
        Vector3 fwd = _eye.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        return fwd.normalized;
    }

    // ─── Datos ──────────────────────────────────────────────────────────────────

    private void RefreshCount() => SetCount(PlayerInventory.Instance != null ? PlayerInventory.Instance.GetSpyCoins() : 0);

    private void SetCount(int total)
    {
        if (_countText != null)
            _countText.text = total.ToString();
    }

    // ─── Construcción de la UI ──────────────────────────────────────────────────

    private void BuildUI()
    {
        var canvasGO = new GameObject("CoinHUDCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var canvasRT = (RectTransform)canvasGO.transform;
        canvasRT.sizeDelta = new Vector2(260f, 92f);
        canvasRT.localScale = Vector3.one * 0.0012f; // ≈31 cm de ancho

        // Chip de fondo semitransparente para legibilidad sobre cualquier entorno.
        RectTransform chip = CreateRect("Chip", canvasRT);
        Stretch(chip);
        var chipImg = chip.gameObject.AddComponent<Image>();
        chipImg.color = new Color(0f, 0f, 0f, 0.55f);

        // Icono de moneda (círculo dorado generado por código).
        RectTransform icon = CreateRect("CoinIcon", chip);
        icon.anchorMin = new Vector2(0f, 0.5f);
        icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0f, 0.5f);
        icon.sizeDelta = new Vector2(58f, 58f);
        icon.anchoredPosition = new Vector2(16f, 0f);
        var iconImg = icon.gameObject.AddComponent<Image>();
        iconImg.sprite = GetCoinSprite();
        iconImg.color = new Color(1f, 0.82f, 0.20f, 1f); // dorado

        // Número de monedas.
        RectTransform count = CreateRect("Count", chip);
        count.anchorMin = Vector2.zero;
        count.anchorMax = Vector2.one;
        count.offsetMin = new Vector2(86f, 6f);
        count.offsetMax = new Vector2(-16f, -6f);
        _countText = count.gameObject.AddComponent<TextMeshProUGUI>();
        _countText.alignment = TextAlignmentOptions.MidlineLeft;
        _countText.fontSize = 48f;
        _countText.color = Color.white;
        _countText.text = "0";
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

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

    /// <summary>Genera (y cachea) un sprite circular blanco para usar como icono de moneda.</summary>
    private static Sprite GetCoinSprite()
    {
        if (_coinSprite != null) return _coinSprite;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float r = size * 0.5f;
        var c = new Vector2(r, r);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float alpha = Mathf.Clamp01(r - d); // borde suave de 1 px
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        _coinSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _coinSprite;
    }
}
