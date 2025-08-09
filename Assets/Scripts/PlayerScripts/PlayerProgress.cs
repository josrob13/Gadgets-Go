using System.Collections.Generic;
using UnityEngine;

public class PlayerProgress : MonoBehaviour
{
    [System.Serializable]
    public struct MissionErrorCounter
    {
        public string missionId;
        public int errors;
    }
    
    public static PlayerProgress Instance { get; private set; }

    [Header("Player Progress")]
    [SerializeField] private List<string> completedMissions = new();
    [SerializeField] private int indexWorld = 0;
    [SerializeField] private int missionPoints = 0;


    [SerializeField] private int totalErrors = 0;
    [SerializeField] private List<MissionErrorCounter> errorsByMission = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CompleteMission(string missionId)
    {
        if (!completedMissions.Contains(missionId))
        {
            completedMissions.Add(missionId);
            missionPoints++;
            Debug.Log($"Mission '{missionId}' completed!");
        }
        else
        {
            Debug.LogWarning($"Mission '{missionId}' was already completed.");
        }
    }

    public void RegisterError(string missionId)
    {
        totalErrors++;
        int i = errorsByMission.FindIndex(e => e.missionId == missionId);
        if (i >= 0)
        {
            var entry = errorsByMission[i];
            entry.errors++;
            errorsByMission[i] = entry;
        }
        else
        {
            errorsByMission.Add(new MissionErrorCounter { missionId = missionId, errors = 1 });
        }
        Debug.Log($"[Progress] Error registrado en '{missionId}'. Total misión: {GetErrors(missionId)} | Total global: {totalErrors}");
    }

    public int GetErrors(string missionId)
        => errorsByMission.Find(e => e.missionId == missionId).errors;

    public int GetTotalErrors() => totalErrors;

    public bool IsMissionCompleted(string missionId)
    {
        return completedMissions.Contains(missionId);
    }
}
