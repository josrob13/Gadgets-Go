using System.Collections.Generic;
using UnityEngine;

public class GameHandler : MonoBehaviour
{
    public static GameHandler Instance;
    private WorldsDB worldsDB;
    private int indexWorld = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning($"[GameHandler] Se ha encontrado una segunda instancia de GameHandler en la escena. El objeto '{this.name}' será destruido para mantener el singleton.");
            Destroy(gameObject);
        }
    }

    public void StartNewGame()
    {
        Debug.Log("[GameHandler] Iniciando Nueva Partida...");

        // 1. Creamos datos limpios por defecto
        GameData newGameData = new GameData();
        
        // 2. Sobrescribimos el archivo viejo en el disco duro
        // SaveSystem.Save(newGameData);

        // 3. Reiniciamos las variables internas del GameHandler por si acaso
        this.indexWorld = newGameData.savedWorldIndex;

        // 4. Llamamos al SceneLoader para ir al juego
        // Asegúrate de que "RealGame" esté en File -> Build Settings
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
        Debug.Log($"[Progress] Intentando avanzar de mundo desde {indexWorld}");

        if (worldsDB?.worlds == null || worldsDB.worlds.Length == 0)
        {
            Debug.LogWarning("WorldsDB is not set or empty.");
            return false;
        }

        int idx = Mathf.Clamp(indexWorld, 0, worldsDB.worlds.Length - 1);
        var current = worldsDB.worlds[idx];
        if (!PlayerProgress.Instance.IsWorldCompleted(current)) return false;

        indexWorld = idx + 1;
        if (indexWorld >= worldsDB.worlds.Length)
        {
            Debug.LogWarning("No more worlds to advance to. JUEGO FINALIZADO!");
        }
        else
        {
            var next = worldsDB.worlds[indexWorld];
            Debug.Log($"[Progress] Avanzando a mundo {indexWorld}: {next?.name}");
        }

        // Aquí no cargo escena para no acoplar. Hazlo en tu MissionManager/SceneController.
        return true;
    }
}
