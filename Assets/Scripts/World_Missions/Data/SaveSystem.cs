using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System;

public static class SaveSystem
{
    private static string currentTherapistFolder = "";

    public static void SetTherapistFolder(string therapistId)
    {
        currentTherapistFolder = therapistId;
    }

    public static string GetCurrentTherapistFolder()
    {
        return currentTherapistFolder;
    }

    private static string GetSaveFilePath()
    {
        string basePath = Application.persistentDataPath;
        if (!string.IsNullOrEmpty(currentTherapistFolder))
        {
            basePath = Path.Combine(basePath, currentTherapistFolder);
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
        }
        return Path.Combine(basePath, "gamedata.json");
    }

    public static void Save(GameData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string filePath = GetSaveFilePath();
        File.WriteAllText(filePath, json);

        string folderPath = Path.GetDirectoryName(filePath);
        string csvPath    = AnalyticsManager.Instance.ExportTherapistCsv();
        string htmlPath   = ReportGenerator.GenerateHTMLReport(data, folderPath);

        Debug.Log($"[SaveSystem] Partida guardada en: {filePath}");
        Debug.Log($"[SaveSystem] CSV exportado a: {csvPath}");
        if (!string.IsNullOrEmpty(htmlPath))
            Debug.Log($"[SaveSystem] Informe HTML generado en: {htmlPath}");
    }

    public static GameData Load()
    {
        string filePath = GetSaveFilePath();
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
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

    public static string CreateNewTherapistFolder(string baseTherapistId)
    {
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string folderName = $"Terapeuta_{baseTherapistId}_{timestamp}";
        string fullPath = Path.Combine(Application.persistentDataPath, folderName);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
        return folderName;
    }

    public static string GetMostRecentTherapistFolder()
    {
        List<string> folders = GetAvailableTherapistFolders();
        if (folders.Count == 0) return "";

        string basePath = Application.persistentDataPath;
        string mostRecent = "";
        DateTime mostRecentTime = DateTime.MinValue;

        foreach (string folder in folders)
        {
            string folderPath = Path.Combine(basePath, folder);
            string filePath = Path.Combine(folderPath, "gamedata.json");
            if (File.Exists(filePath))
            {
                DateTime lastWrite = File.GetLastWriteTime(filePath);
                if (lastWrite > mostRecentTime)
                {
                    mostRecentTime = lastWrite;
                    mostRecent = folder;
                }
            }
        }
        return mostRecent;
    }

    public static List<string> GetAvailableTherapistFolders()
    {
        string basePath = Application.persistentDataPath;
        List<string> folders = new List<string>();
        if (Directory.Exists(basePath))
        {
            foreach (string dir in Directory.GetDirectories(basePath))
            {
                string folderName = Path.GetFileName(dir);
                if (folderName.StartsWith("Terapeuta_"))
                {
                    folders.Add(folderName);
                }
            }
        }
        return folders;
    }
}