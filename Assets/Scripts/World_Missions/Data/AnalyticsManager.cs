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

    private Dictionary<string, DialogueAnalyticsEntry> dialogueAnalyticsMap;
    private Dictionary<SocialInferenceCategory, InferenceCategoryErrorCounter> inferenceErrorMap;

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
        {
            currentDialogueElapsed += Time.deltaTime;
        }
    }

    public void BeginDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId))
            return;

        currentDialogueId = dialogueId;
        currentDialogueElapsed = 0f;
        GetOrCreateDialogueEntry(dialogueId);
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

        DialogueAnalyticsEntry session = GetOrCreateDialogueEntry(currentDialogueId);
        session.wrongAnswers++;
    }
    
    public string ExportTherapistCsv()
    {
        string reportPath = Application.persistentDataPath + "/gamedata_report.csv";
        var lines = new List<string>
        {
            "Tipo;Valor",
            $"Tiempo total de juego (s);{totalPlayedTime:F1}",
            "",
            "dialogueId;timeSpentSeconds;wrongAnswers"
        };

        if (dialogueAnalyticsSessions != null && dialogueAnalyticsSessions.Count > 0)
        {
            foreach (var session in dialogueAnalyticsSessions)
            {
                string dialogueId = EscapeCsvField(session.dialogueId);
                lines.Add($"{dialogueId};{session.timeSpent:F1};{session.wrongAnswers}");
            }
        }
        else
        {
            lines.Add("No hay datos de diálogo disponibles;;");
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

        File.WriteAllLines(reportPath, lines, Encoding.UTF8);
        Debug.Log($"[AnalyticsManager] Reporte CSV exportado a: {reportPath}");
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

    private void LoadAnalyticsFromSave()
    {
        GameData savedData = SaveSystem.Load();
        if (savedData == null)
            return;

        totalPlayedTime = savedData.totalPlayedTime;
        dialogueAnalyticsSessions = savedData.dialogueAnalyticsSessions ?? new List<DialogueAnalyticsEntry>();
        inferenceCategoryErrors = savedData.inferenceCategoryErrors ?? new List<InferenceCategoryErrorCounter>();
        InitializeMaps();
    }

    public void SaveAnalytics()
    {
        GameData data = SaveSystem.Load() ?? new GameData();
        data.totalPlayedTime = totalPlayedTime;
        data.dialogueAnalyticsSessions = dialogueAnalyticsSessions ?? new List<DialogueAnalyticsEntry>();
        data.inferenceCategoryErrors = inferenceCategoryErrors ?? new List<InferenceCategoryErrorCounter>();
        SaveSystem.Save(data);
    }

    private void OnApplicationQuit()
    {
        SaveAnalytics();
    }
}
