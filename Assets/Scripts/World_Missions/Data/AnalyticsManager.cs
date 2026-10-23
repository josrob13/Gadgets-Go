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
            // LoadAnalyticsFromSave();
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

    public TherapyMetrics Export() => new TherapyMetrics
    {
        totalPlayedTime = totalPlayedTime,
        dialogueAnalyticsSessions = dialogueAnalyticsSessions,
        inferenceCategoryErrors = inferenceCategoryErrors,
        totalFaceDiscomfortEvents = totalFaceDiscomfortEvents,
        totalFaceDiscomfortSeconds = totalFaceDiscomfortSeconds
    };

    public void Import(TherapyMetrics m)
    {
        m ??= new TherapyMetrics();
        totalPlayedTime            = m.totalPlayedTime;
        dialogueAnalyticsSessions  = m.dialogueAnalyticsSessions ?? new List<DialogueAnalyticsEntry>();
        inferenceCategoryErrors    = m.inferenceCategoryErrors   ?? new List<InferenceCategoryErrorCounter>();
        totalFaceDiscomfortEvents  = m.totalFaceDiscomfortEvents;
        totalFaceDiscomfortSeconds = m.totalFaceDiscomfortSeconds;
        InitializeMaps();
        currentDialogueId = null;
        currentDialogueElapsed = 0f;
        _isTrackingDiscomfort = false;
        _discomfortMissionId = null;
    }

    public void SaveMetricsLight() => SaveSystem.SaveMetrics(Export());

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

    public void RegisterFaceDiscomfort(bool isDiscomfort, float score)
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
                entry.faceDiscomfortPeakScore = Mathf.Max(entry.faceDiscomfortPeakScore, score);
            }
        }
        else
        {
            _isTrackingDiscomfort = false;
            _discomfortMissionId = null;
        }
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
        // SaveAnalytics();
    }
}
