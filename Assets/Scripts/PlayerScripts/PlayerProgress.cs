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

    // I use here serialized structures in order to save the data. Hashsets for example are not serializable
    [Header("Player Progress")]
    [SerializeField] private List<string> completedMissionsSerialized = new();

    [Header("Mission Error Tracking")]
    [SerializeField] private int totalErrors = 0;
    [SerializeField] private List<MissionErrorCounter> errorsByMissionSerialized = new();

    // ---------------- Runtime ----------------
    private HashSet<string> completedMissions;
    private Dictionary<string, int> errorsByMission;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        completedMissions = new HashSet<string>(completedMissionsSerialized);
        errorsByMission = new Dictionary<string, int>(errorsByMissionSerialized.Count);

        foreach (var error in errorsByMissionSerialized)
            errorsByMission[error.missionId] = error.errors;
    }

    public bool CompleteMission(string missionId)
    {
        if (!completedMissions.Contains(missionId))
        {
            completedMissions.Add(missionId);
            Debug.Log($"Mission '{missionId}' completed!");

            // Little reward for player
            PlayerInventory.Instance?.AddSpyCoins(25);
            return true;
        }
        else
        {
            Debug.LogWarning($"Mission '{missionId}' was already completed.");
            return false;
        }
    }

    public bool IsMissionCompleted(string missionId)
    {
        return !string.IsNullOrEmpty(missionId) && completedMissions.Contains(missionId);
    }

    public bool IsWorldCompleted(World world)
    {
        if (world == null || world.Missions == null || world.Missions.Count == 0)
        {
            Debug.LogWarning("World is null or has no missions.");
            return false;
        }

        foreach (var mission in world.Missions)
        {
            if (mission != null && !IsMissionCompleted(mission.GetMissionName()))
            {
                Debug.Log($"World '{world.name}' is not completed. Missing mission: '{mission.GetMissionName()}'");
                return false;
            }
        }

        return true;
    }

    public void RegisterError(string missionId)
    {
        totalErrors++;

        // Increment error count in the dictionary or initialize it if not present
        if (errorsByMission.TryGetValue(missionId, out int currentErrors))
        {
            errorsByMission[missionId] = currentErrors + 1;
        }
        else
            errorsByMission[missionId] = 1;

        // Update the serialized list
        int i = errorsByMissionSerialized.FindIndex(e => e.missionId == missionId);
        if (i >= 0)
        {
            var entry = errorsByMissionSerialized[i];
            entry.errors++;
            errorsByMissionSerialized[i] = entry;
        }
        else
        {
            errorsByMissionSerialized.Add(new MissionErrorCounter { missionId = missionId, errors = 1 });
        }

        Debug.Log($"[Progress] Error registrado en '{missionId}'. Total misión: {GetErrors(missionId)} | Total global: {totalErrors}");
    }

    public int GetErrors(string missionId)
        => errorsByMission.ContainsKey(missionId) ? errorsByMission[missionId] : 0;

    public int GetTotalErrors() => totalErrors;
}
