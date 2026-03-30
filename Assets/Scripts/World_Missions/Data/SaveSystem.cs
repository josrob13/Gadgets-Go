using UnityEngine;
using System.IO;

public static class SaveSystem
{
    private static string saveFilePath = Application.persistentDataPath + "/gamedata.json";

    public static void Save(GameData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[SaveSystem] Partida guardada exitosamente en: {saveFilePath}");
    }

    public static GameData Load()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            GameData loadedData = JsonUtility.FromJson<GameData>(json);
            
            Debug.Log("[SaveSystem] Partida cargada correctamente.");
            return loadedData;
        }
        else
        {
            Debug.LogWarning("[SaveSystem] No se encontró partida guardada. Creando datos nuevos.");
            return new GameData();
        }
    }
}