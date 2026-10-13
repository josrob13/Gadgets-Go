using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public string playerName;
    public int savedWorldIndex;
    public float totalPlayedTime;
    public InventoryData inventory;
    public List<string> completedMissions;
    public List<DialogueAnalyticsEntry> dialogueAnalyticsSessions;
    public List<InferenceCategoryErrorCounter> inferenceCategoryErrors;

    // Global face tracking totals
    public int totalFaceDiscomfortEvents;
    public float totalFaceDiscomfortSeconds;

    public GameData()
    {
        savedWorldIndex = 0;
        totalPlayedTime = 0f;
        completedMissions = new List<string>();
        dialogueAnalyticsSessions = new List<DialogueAnalyticsEntry>();
        inferenceCategoryErrors = new List<InferenceCategoryErrorCounter>();
        
        // Inicializamos las sub-clases
        inventory = new InventoryData();
    }
}