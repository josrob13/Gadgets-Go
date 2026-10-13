using System.Collections.Generic;
using UnityEngine;

public class GameHandler : MonoBehaviour
{
    public static GameHandler Instance;
    [SerializeField] private WorldsDB worldsDB;

    [Tooltip("Escena que se carga cuando el jugador completa TODOS los mundos. " + "Debe estar añadida en File → Build Settings.")]
    [SerializeField] private string endSceneName = "ThankYou";
    [Tooltip("Escena Intro")]
    [SerializeField] private string introSceneName = "Intro";
    private int indexWorld = 0;
    public string PlayerName { get; set; } = "Jugador";
    public int CurrentWorldIndex => indexWorld;

    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log("[GameHandler] Instancia creada y persistente entre escenas.");
            Instance = this;
            GameObject rootObject = transform.root.gameObject;
            DontDestroyOnLoad(rootObject);

            if (AnalyticsManager.Instance == null && rootObject.GetComponentInChildren<AnalyticsManager>() == null)
            {
                Debug.Log("[GameHandler] AnalyticsManager no encontrado. Agregando componente.");
                // Verifying if AnalyticsManager is already in the scene before adding it to avoid duplicates
                rootObject.AddComponent<AnalyticsManager>();
            }
        }
        else
        {
            Debug.LogWarning($"[GameHandler] Se ha encontrado una segunda instancia de GameHandler en la escena. El objeto '{this.name}' será destruido para mantener el singleton.");
            Destroy(gameObject);
        }
    }

    private PlayerSaveData BuildProgress() => new PlayerSaveData
    {
        playerName = PlayerName,
        savedWorldIndex = indexWorld,
        completedMissions = PlayerProgress.Instance != null ? PlayerProgress.Instance.ExportCompletedMissions() : new List<string>(),
        inventory = PlayerInventory.Instance != null ? PlayerInventory.Instance.Export() : new InventoryData()
    };

    private void ApplyProgress(PlayerSaveData p)
    {
        PlayerName = p.playerName;
        indexWorld = p.savedWorldIndex;
        PlayerProgress.Instance?.ImportCompletedMissions(p.completedMissions);
        PlayerInventory.Instance?.Import(p.inventory);
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveLight(); }
    private void OnApplicationQuit() { SaveLight(); }

    public void SaveProgress() => SaveSystem.SaveProgress(BuildProgress());

    private void BeginSession(string folder, bool isNew)
    {
        SaveSystem.SetTherapistFolder(folder);
        ApplyProgress(isNew ? new PlayerSaveData() : SaveSystem.LoadProgress());
        AnalyticsManager.Instance?.Import(isNew ? new TherapyMetrics() : SaveSystem.LoadMetrics());
    }

    public void SaveLight()
    {
        SaveProgress();
        AnalyticsManager.Instance?.SaveMetricsLight();
    }

    public void SaveFull()
    {
        var p = BuildProgress();
        SaveSystem.SaveProgress(p);
        AnalyticsManager.Instance?.SaveMetricsAndExport(p);
    }

    public void StartNewGame(string baseTherapistId)
    {
        Debug.Log("[GameHandler] Iniciando Nueva Partida...");

        BeginSession(SaveSystem.CreateNewTherapistFolder(baseTherapistId), isNew: true);
        SaveProgress();
        SceneLoader.Instance?.LoadSceneAsync(introSceneName);

        if (SceneLoader.Instance == null)
            Debug.LogError("¡No se encontró el SceneLoader en la escena!");
    }

    public void LoadCurrentWorld()
    {
        if (worldsDB?.worlds == null || worldsDB.worlds.Length == 0)
        {
            Debug.LogError("[GameHandler] WorldsDB no asignado o vacío — no se puede cargar el mundo actual.");
            return;
        }

        int idx = Mathf.Clamp(indexWorld, 0, worldsDB.worlds.Length - 1);
        string sceneName = worldsDB.worlds[idx].SceneName;
        Debug.Log($"[GameHandler] Cargando mundo {idx}: '{worldsDB.worlds[idx].WorldName}' → escena '{sceneName}'");

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneAsync(sceneName);
        else
            Debug.LogError("[GameHandler] SceneLoader.Instance es null — no se puede cargar la escena del mundo actual.");
    }

    public void ContinueGame()
    {
        string folder = SaveSystem.GetMostRecentTherapistFolder();
        if (string.IsNullOrEmpty(folder))
        {
            Debug.LogWarning("[GameHandler] No hay partidas guardadas.");
            return;
        }
        BeginSession(folder, isNew: false);
        Debug.Log($"[GameHandler] Continuando partida en carpeta: {folder}");
        LoadCurrentWorld();
    }

    public void LoadGame(string therapistId)
    {
        Debug.Log($"[GameHandler] Cargando partida desde carpeta: {therapistId}");
        BeginSession(therapistId, isNew: false);
        LoadCurrentWorld();
    }
    
    public bool NextWorld()
    {
        Debug.Log($"[GameHandler] Intentando avanzar de mundo desde {indexWorld}");

        if (worldsDB?.worlds == null || worldsDB.worlds.Length == 0)
        {
            Debug.LogWarning("[GameHandler] WorldsDB no asignado en el Inspector — no se puede avanzar de mundo.");
            return false;
        }

        int idx = Mathf.Clamp(indexWorld, 0, worldsDB.worlds.Length - 1);
        var current = worldsDB.worlds[idx];
        if (!PlayerProgress.Instance.IsWorldCompleted(current)) return false;

        indexWorld = idx + 1;
        // Persist the new world index to disk immediately.
        SaveLight();

        if (indexWorld >= worldsDB.worlds.Length)
        {
            Debug.Log($"[GameHandler] ¡Todos los mundos completados! Cargando escena final '{endSceneName}'.");

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadSceneAsync(endSceneName);
            else
                Debug.LogError("[GameHandler] SceneLoader.Instance es null — no se puede cargar la escena final.");

            return true;
        }

        World nextWorld = worldsDB.worlds[indexWorld];
        Debug.Log($"[GameHandler] Avanzando al mundo {indexWorld}: '{nextWorld.WorldName}' — cargando escena '{nextWorld.SceneName}'");

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneAsync(nextWorld.SceneName);
        else
            Debug.LogError("[GameHandler] SceneLoader.Instance es null — no se puede cargar la escena del mundo siguiente.");

        return true;
    }
}
