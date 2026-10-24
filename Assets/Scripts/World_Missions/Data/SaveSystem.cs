using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System;

public static class SaveSystem
{
    private const string ProgressFile = "player_progress.json";

    private const string MetricsFile = "therapy_metrics.json";
    public const int CurrentSaveVersion = 1;

    private static string currentTherapistFolder = "";

    public static void SetTherapistFolder(string therapistId)
    {
        currentTherapistFolder = therapistId;
    }

    public static string GetCurrentTherapistFolder()
    {
        return currentTherapistFolder;
    }
    
    public static bool HasActiveSession => !string.IsNullOrEmpty(currentTherapistFolder);

    private static bool TryWriteJson(string file, object data)
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, currentTherapistFolder));
            string path = SessionPath(file);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));

            if (File.Exists(path)) File.Replace(tmp, path, path + ".bak");
            else File.Move(tmp, path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] No se pudo guardar '{file}': {e.Message}");
            return false;
        }
    }

    private static T ReadJson<T>(string file) where T : class
    {
        if (!HasActiveSession) return null;

        string path = SessionPath(file);
        foreach (string candidate in new[] { path, path + ".bak" })
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                var obj = JsonUtility.FromJson<T>(File.ReadAllText(candidate));
                if (obj != null) return obj;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] No se pudo leer '{candidate}': {e.Message}");
            }
        }
        return null;
    }

    private static void WarnIfNewer(int version, string file)
    {
        if (version > CurrentSaveVersion)
            Debug.LogWarning($"[SaveSystem] '{file}' es de una versión más nueva ({version}) que la del juego ({CurrentSaveVersion}).");
    }

    private static string SessionPath(string file)
    {
        return Path.Combine(Application.persistentDataPath, currentTherapistFolder, file);
    }

    public static void SaveProgress(PlayerSaveData data)
    {
        if (!HasActiveSession) return;
        data.saveVersion = CurrentSaveVersion;
        data.sessionId = currentTherapistFolder;
        TryWriteJson(ProgressFile, data);
    }

    public static PlayerSaveData LoadProgress()
    {
        var data = ReadJson<PlayerSaveData>(ProgressFile) ?? new PlayerSaveData();
        data.inventory ??= new InventoryData();
        data.completedMissions ??= new List<string>();
        WarnIfNewer(data.saveVersion, ProgressFile);
        return data;
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
            string filePath = Path.Combine(folderPath, ProgressFile);
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

    public static void SaveMetrics(TherapyMetrics therapyMetrics)
    {
        if (!HasActiveSession) return;
        therapyMetrics.saveVersion = CurrentSaveVersion;
        therapyMetrics.sessionId = currentTherapistFolder;
        TryWriteJson(MetricsFile, therapyMetrics);
    }

    public static TherapyMetrics LoadMetrics()
    {
        var m = ReadJson<TherapyMetrics>(MetricsFile) ?? new TherapyMetrics();
        m.dialogueAnalyticsSessions ??= new List<DialogueAnalyticsEntry>();
        m.inferenceCategoryErrors ??= new List<InferenceCategoryErrorCounter>();
        WarnIfNewer(m.saveVersion, MetricsFile);
        return m;
    }

    internal static void ExportTherapistReports(PlayerSaveData p, TherapyMetrics therapyMetrics)
    {
        if (!HasActiveSession) return;
        try
        {
            string folderPath = Path.Combine(Application.persistentDataPath, currentTherapistFolder);
            Directory.CreateDirectory(folderPath);

            string csvPath  = WriteCsv(p, therapyMetrics, folderPath);
            string htmlPath = ReportGenerator.GenerateHTMLReport(p, therapyMetrics, currentTherapistFolder, folderPath);

            Debug.Log($"[SaveSystem] CSV exportado a: {csvPath}");
            if (!string.IsNullOrEmpty(htmlPath))
                Debug.Log($"[SaveSystem] Informe HTML generado en: {htmlPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] No se pudieron generar los informes: {e.Message}");
        }
    }

    private static string WriteCsv(PlayerSaveData p, TherapyMetrics m, string folderPath)
    {
        string reportPath = Path.Combine(folderPath, "informe_terapeuta.csv");

        var lines = new List<string>
        {
            "Tipo;Valor",
            $"Misiones completadas;{p.completedMissions?.Count ?? 0}",
            $"Tiempo total de juego (s);{m.totalPlayedTime:F1}",
            "",
            $"Eventos de incomodidad facial (total);{m.totalFaceDiscomfortEvents}",
            $"Tiempo de incomodidad facial (s, total);{m.totalFaceDiscomfortSeconds:F1}",
            "",
            "dialogueId;sesiones;tiempoSegundos;respuestasErroneas;pistasUsadas;avgTiempoRespuestaS;ojosEnfocadosS;ojosDistrayendoseS;eventosDistraccion;eventosIncomodidadFacial;tiempoIncomodidadFacialS;peakScoreIncomodidad"
        };

        if (m.dialogueAnalyticsSessions != null && m.dialogueAnalyticsSessions.Count > 0)
        {
            foreach (var s in m.dialogueAnalyticsSessions)
            {
                string id = EscapeCsvField(s.dialogueId);
                lines.Add($"{id};{s.sessionsPlayed};{s.timeSpent:F1};{s.wrongAnswers};{s.hintsRequested};{s.AvgAnswerTimeSeconds:F1};{s.eyeFocusedSeconds:F1};{s.eyeDistractedSeconds:F1};{s.eyeDistractionEvents};{s.faceDiscomfortEvents};{s.faceDiscomfortSeconds:F1};{s.faceDiscomfortPeakScore:F2}");
            }
        }
        else
        {
            lines.Add("No hay datos de diálogo disponibles;;;;;;;;;;;;");
        }

        lines.Add("");
        lines.Add("category;errors");

        if (m.inferenceCategoryErrors != null && m.inferenceCategoryErrors.Count > 0)
        {
            foreach (var counter in m.inferenceCategoryErrors)
                lines.Add($"{counter.category};{counter.errors}");
        }
        else
        {
            lines.Add("No hay errores de inferencia registrados;");
        }

        try
        {
            File.WriteAllLines(reportPath, lines, Encoding.UTF8);
        }
        catch (IOException)
        {
            // File is open in another program — write to a timestamped backup instead
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            reportPath = reportPath.Replace(".csv", $"_{timestamp}.csv");
            File.WriteAllLines(reportPath, lines, Encoding.UTF8);
            Debug.LogWarning($"[SaveSystem] CSV en uso — backup exportado a: {reportPath}");
        }

        return reportPath;
    }

    private static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        if (value.Contains(";") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            return "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }
}