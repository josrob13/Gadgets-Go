using System.Collections.Generic;
using UnityEngine;

public class GameHandler : MonoBehaviour
{
    public static GameHandler Instance;
    public WorldsDB worldsDB;
    public int indexWorld = 0;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
