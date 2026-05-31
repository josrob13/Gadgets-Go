using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    [Header("Session analytics")]
    public float totalPlayedTime;
    public string currentDialogueId;
    public float currentDialogueElapsed;

    [Header("Saved analytics")]
    public List<DialogueAnalyticsEntry> dialogueAnalyticsSessions = new();
    public List<InferenceCategoryErrorCounter> inferenceCategoryErrors = new();

    [Header("Face tracking (global)")]
    public int totalFaceDiscomfortEvents;
    public float totalFaceDiscomfortSeconds;

    private Dictionary<string, DialogueAnalyticsEntry> dialogueAnalyticsMap;
    private Dictionary<SocialInferenceCategory, InferenceCategoryErrorCounter> inferenceErrorMap;
    private bool _isTrackingDiscomfort;
    private string _discomfortMissionId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMaps();
            LoadAnalyticsFromSave();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (FaceTrackingManager.Instance != null)
        {
            FaceTrackingManager.Instance.OnDiscomfortStateChanged += OnFaceDiscomfortChanged;
            Debug.Log("[AnalyticsManager] Subscribed to FaceTrackingManager.");
        }
        else
        {
            Debug.LogWarning("[AnalyticsManager] FaceTrackingManager not found — face analytics disabled.");
        }
    }

    private void OnDestroy()
    {
        if (FaceTrackingManager.Instance != null)
            FaceTrackingManager.Instance.OnDiscomfortStateChanged -= OnFaceDiscomfortChanged;
    }

    private void InitializeMaps()
    {
        dialogueAnalyticsMap = new Dictionary<string, DialogueAnalyticsEntry>();
        inferenceErrorMap = new Dictionary<SocialInferenceCategory, InferenceCategoryErrorCounter>();

        if (dialogueAnalyticsSessions != null)
        {
            foreach (var entry in dialogueAnalyticsSessions)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.dialogueId))
                    dialogueAnalyticsMap[entry.dialogueId] = entry;
            }
        }

        if (inferenceCategoryErrors != null)
        {
            foreach (var counter in inferenceCategoryErrors)
            {
                if (counter != null)
                    inferenceErrorMap[counter.category] = counter;
            }
        }
    }

    private void Update()
    {
        totalPlayedTime += Time.deltaTime;

        if (!string.IsNullOrEmpty(currentDialogueId))
            currentDialogueElapsed += Time.deltaTime;

        if (_isTrackingDiscomfort)
        {
            float dt = Time.unscaledDeltaTime;
            totalFaceDiscomfortSeconds += dt;
            if (!string.IsNullOrEmpty(_discomfortMissionId))
                GetOrCreateDialogueEntry(_discomfortMissionId).faceDiscomfortSeconds += dt;
        }
    }

    public void BeginDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId))
            return;

        currentDialogueId = dialogueId;
        currentDialogueElapsed = 0f;
        GetOrCreateDialogueEntry(dialogueId).sessionsPlayed++;
    }

    public void EndDialogue()
    {
        if (string.IsNullOrEmpty(currentDialogueId))
            return;

        DialogueAnalyticsEntry session = GetOrCreateDialogueEntry(currentDialogueId);
        session.timeSpent += currentDialogueElapsed;
        currentDialogueId = null;
        currentDialogueElapsed = 0f;
    }

    public void RegisterInferenceError(SocialInferenceCategory category)
    {
        if (!inferenceErrorMap.TryGetValue(category, out var counter))
        {
            counter = new InferenceCategoryErrorCounter { category = category, errors = 0 };
            inferenceCategoryErrors.Add(counter);
            inferenceErrorMap[category] = counter;
        }

        counter.errors++;
    }

    public void RegisterCurrentDialogueWrongAnswer()
    {
        if (string.IsNullOrEmpty(currentDialogueId))
            return;

        GetOrCreateDialogueEntry(currentDialogueId).wrongAnswers++;
    }

    public void RegisterMissionEyeData(string missionId, EyeTracking eyeTracker)
    {
        if (eyeTracker == null || string.IsNullOrEmpty(missionId)) return;
        var entry = GetOrCreateDialogueEntry(missionId);
        entry.eyeFocusedSeconds += eyeTracker.FocusedTime;
        entry.eyeDistractedSeconds += eyeTracker.DistractedTime;
        entry.eyeDistractionEvents += eyeTracker.DistractionCount;
        Debug.Log($"[Analytics] Eye data written to entry '{missionId}': focused={entry.eyeFocusedSeconds:F1}s, distracted={entry.eyeDistractedSeconds:F1}s, events={entry.eyeDistractionEvents}");
    }

    public void RegisterHintUsed(string missionId)
    {
        if (string.IsNullOrEmpty(missionId)) return;
        GetOrCreateDialogueEntry(missionId).hintsRequested++;
    }

    public void RegisterQuestionAnswerTime(string missionId, float seconds)
    {
        if (string.IsNullOrEmpty(missionId)) return;
        var entry = GetOrCreateDialogueEntry(missionId);
        entry.totalAnswerTimeSeconds += seconds;
        entry.questionsAnswered++;
    }

    private void OnFaceDiscomfortChanged(bool isDiscomfort)
    {
        Debug.Log($"[Analytics] FaceDiscomfort changed: {isDiscomfort}");
        string missionId = MissionManager.Instance?.GetCurrentMissionId();
        bool inMission = !string.IsNullOrEmpty(missionId) && missionId != "Unkown Mission";

        if (isDiscomfort)
        {
            _isTrackingDiscomfort = true;
            _discomfortMissionId = inMission ? missionId : null;
            totalFaceDiscomfortEvents++;

            if (inMission)
            {
                var entry = GetOrCreateDialogueEntry(missionId);
                entry.faceDiscomfortEvents++;
                entry.faceDiscomfortPeakScore = Mathf.Max(entry.faceDiscomfortPeakScore, FaceTrackingManager.Instance?.DiscomfortScore ?? 0f);
            }
        }
        else
        {
            _isTrackingDiscomfort = false;
            _discomfortMissionId = null;
        }
    }
    
    public string ExportTherapistCsv()
    {
        string basePath = Application.persistentDataPath;
        string therapistFolder = SaveSystem.GetCurrentTherapistFolder();
        if (!string.IsNullOrEmpty(therapistFolder))
        {
            basePath = Path.Combine(basePath, therapistFolder);
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
        }
        string reportPath = Path.Combine(basePath, "gamedata_report.csv");
        foreach (var s in dialogueAnalyticsSessions)
            Debug.Log($"[Analytics] CSV dump — '{s.dialogueId}': focused={s.eyeFocusedSeconds:F1}s, distracted={s.eyeDistractedSeconds:F1}s, events={s.eyeDistractionEvents}");
        var lines = new List<string>
        {
            "Tipo;Valor",
            $"Tiempo total de juego (s);{totalPlayedTime:F1}",
            "",
            $"Eventos de incomodidad facial (total);{totalFaceDiscomfortEvents}",
            $"Tiempo de incomodidad facial (s, total);{totalFaceDiscomfortSeconds:F1}",
            "",
            "dialogueId;sesiones;tiempoSegundos;respuestasErroneas;pistasUsadas;avgTiempoRespuestaS;ojosEnfocadosS;ojosDistrayendoseS;eventosDistraccion;eventosIncomodidadFacial;tiempoIncomodidadFacialS;peakScoreIncomodidad"
        };

        if (dialogueAnalyticsSessions != null && dialogueAnalyticsSessions.Count > 0)
        {
            foreach (var s in dialogueAnalyticsSessions)
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

        if (inferenceCategoryErrors != null && inferenceCategoryErrors.Count > 0)
        {
            foreach (var counter in inferenceCategoryErrors)
            {
                lines.Add($"{counter.category};{counter.errors}");
            }
        }
        else
        {
            lines.Add("No hay errores de inferencia registrados;");
        }

        try
        {
            File.WriteAllLines(reportPath, lines, Encoding.UTF8);
            Debug.Log($"[AnalyticsManager] Reporte CSV exportado a: {reportPath}");
        }
        catch (IOException)
        {
            // File is open in another program — write to a timestamped backup instead
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            reportPath = reportPath.Replace(".csv", $"_{timestamp}.csv");
            File.WriteAllLines(reportPath, lines, Encoding.UTF8);
            Debug.LogWarning($"[AnalyticsManager] CSV en uso — backup exportado a: {reportPath}");
        }

        return reportPath;
    }

    private string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(";") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private DialogueAnalyticsEntry GetOrCreateDialogueEntry(string dialogueId)
    {
        if (dialogueAnalyticsMap.TryGetValue(dialogueId, out var entry))
            return entry;

        entry = new DialogueAnalyticsEntry { dialogueId = dialogueId, timeSpent = 0f, wrongAnswers = 0 };
        dialogueAnalyticsSessions.Add(entry);
        dialogueAnalyticsMap[dialogueId] = entry;
        return entry;
    }

    public void LoadAnalyticsFromSave()
    {
        GameData savedData = SaveSystem.Load();
        if (savedData == null)
            return;

        totalPlayedTime = savedData.totalPlayedTime;
        dialogueAnalyticsSessions = savedData.dialogueAnalyticsSessions ?? new List<DialogueAnalyticsEntry>();
        inferenceCategoryErrors = savedData.inferenceCategoryErrors ?? new List<InferenceCategoryErrorCounter>();
        totalFaceDiscomfortEvents = savedData.totalFaceDiscomfortEvents;
        totalFaceDiscomfortSeconds = savedData.totalFaceDiscomfortSeconds;
        InitializeMaps();
    }

    public void SaveAnalytics()
    {
        // Freeze any in-flight discomfort accumulation so Update() doesn't add more after this point
        _isTrackingDiscomfort = false;

        GameData data = SaveSystem.Load() ?? new GameData();
        data.totalPlayedTime = totalPlayedTime;
        data.dialogueAnalyticsSessions = dialogueAnalyticsSessions ?? new List<DialogueAnalyticsEntry>();
        data.inferenceCategoryErrors = inferenceCategoryErrors ?? new List<InferenceCategoryErrorCounter>();
        data.totalFaceDiscomfortEvents = totalFaceDiscomfortEvents;
        data.totalFaceDiscomfortSeconds = totalFaceDiscomfortSeconds;
        SaveSystem.Save(data);
    }

    public void ResetAnalytics()
    {
        totalPlayedTime = 0f;
        currentDialogueId = null;
        currentDialogueElapsed = 0f;
        dialogueAnalyticsSessions.Clear();
        inferenceCategoryErrors.Clear();
        dialogueAnalyticsMap.Clear();
        inferenceErrorMap.Clear();
        Debug.Log("[AnalyticsManager] Analytics reseteados para nueva partida.");
    }

    private void OnApplicationQuit()
    {
        SaveAnalytics();
    }
}
