using System.Collections.Generic;
using UnityEngine;

public enum SocialInferenceCategory
{
    MessageReception,
    EmotionRecognition,
    NextMovePrediction,
    RelationshipUnderstanding
}

[System.Serializable]
public class DialogueAnalyticsEntry
{
    public string dialogueId;
    public float timeSpent;
    public int wrongAnswers;
}

[System.Serializable]
public class InferenceCategoryErrorCounter
{
    public SocialInferenceCategory category;
    public int errors;
}

[System.Serializable]
public class PlayerStatsData
{
    public float[] position; // Guardamos un array [x,y,z] porque Vector3 a veces da problemas al serializar
}

[System.Serializable]
public class InventoryData
{
    public List<string> itemIDs;
    public int spyCoins;
}

[System.Serializable]
public class GameData
{
    public int savedWorldIndex;
    public float totalPlayedTime;
    public PlayerStatsData playerStats;
    public InventoryData inventory;
    public List<string> completedMissions;
    public List<DialogueAnalyticsEntry> dialogueAnalyticsSessions;
    public List<InferenceCategoryErrorCounter> inferenceCategoryErrors;

    public GameData()
    {
        savedWorldIndex = 0;
        totalPlayedTime = 0f;
        completedMissions = new List<string>();
        dialogueAnalyticsSessions = new List<DialogueAnalyticsEntry>();
        inferenceCategoryErrors = new List<InferenceCategoryErrorCounter>();
        
        // Inicializamos las sub-clases
        playerStats = new PlayerStatsData();
        inventory = new InventoryData();
    }
}