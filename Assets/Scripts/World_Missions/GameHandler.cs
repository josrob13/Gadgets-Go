using System.Collections.Generic;
using UnityEngine;

public class GameHandler : MonoBehaviour
{
    public static GameHandler Instance;
    [SerializeField] private WorldsDB worldsDB;
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

    public void StartNewGame(string baseTherapistId)
    {
        Debug.Log("[GameHandler] Iniciando Nueva Partida...");

        // 1. Crear nueva carpeta con timestamp
        string therapistFolder = SaveSystem.CreateNewTherapistFolder(baseTherapistId);
        SaveSystem.SetTherapistFolder(therapistFolder);

        // 2. Reseteamos los analytics para empezar de cero
        AnalyticsManager.Instance?.ResetAnalytics();

        // 3. Creamos datos limpios por defecto
        GameData newGameData = new GameData();
        newGameData.playerName = PlayerName;

        // 4. Sobrescribimos el archivo viejo en el disco duro
        SaveSystem.Save(newGameData);

        // 5. Reiniciamos las variables internas del GameHandler por si acaso
        this.indexWorld = newGameData.savedWorldIndex;

        // 6. Llamamos al SceneLoader para ir a la intro (que a su vez cargará "RealGame")
        // Asegúrate de que "Intro" y "RealGame" estén en File -> Build Settings
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync("Intro");
        }
        else
        {
            Debug.LogError("¡No se encontró el SceneLoader en la escena!");
        }
    }

    public void ContinueGame()
    {
        string mostRecentFolder = SaveSystem.GetMostRecentTherapistFolder();
        if (!string.IsNullOrEmpty(mostRecentFolder))
        {
            SaveSystem.SetTherapistFolder(mostRecentFolder);
            GameData loadedData = SaveSystem.Load();
            // Restaurar estado del juego
            this.indexWorld = loadedData.savedWorldIndex;
            AnalyticsManager.Instance?.LoadAnalyticsFromSave();

            Debug.Log($"[GameHandler] Continuando partida en carpeta: {mostRecentFolder}");

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadSceneAsync("RealGame");
            }
            else
            {
                Debug.LogError("¡No se encontró el SceneLoader en la escena!");
            }
        }
        else
        {
            Debug.LogWarning("[GameHandler] No hay partidas guardadas para continuar.");
        }
    }

    public void LoadGame(string therapistId)
    {
        SaveSystem.SetTherapistFolder(therapistId);
        GameData loadedData = SaveSystem.Load();
        // Restaurar estado del juego
        this.indexWorld = loadedData.savedWorldIndex;
        AnalyticsManager.Instance?.LoadAnalyticsFromSave();

        Debug.Log($"[GameHandler] Cargando partida desde carpeta: {therapistId}");

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync("RealGame");
        }
        else
        {
            Debug.LogError("¡No se encontró el SceneLoader en la escena!");
        }
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
        AnalyticsManager.Instance?.SaveAnalytics();

        if (indexWorld >= worldsDB.worlds.Length)
        {
            Debug.Log("[GameHandler] ¡Todos los mundos completados! Juego finalizado.");
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
