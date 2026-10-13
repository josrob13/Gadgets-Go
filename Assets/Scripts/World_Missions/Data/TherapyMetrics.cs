using System;
using System.Collections.Generic;

[Serializable]
public class TherapyMetrics
{
    public float totalPlayedTime;
    public List<DialogueAnalyticsEntry> dialogueAnalyticsSessions = new();
    public List<InferenceCategoryErrorCounter> inferenceCategoryErrors = new();
    public int totalFaceDiscomfortEvents;
    public float totalFaceDiscomfortSeconds;
}

public enum SocialInferenceCategory
{
    MessageReception,
    EmotionRecognition,
    NextMovePrediction,
    RelationshipUnderstanding
}

[Serializable]
public class DialogueAnalyticsEntry
{
    public string dialogueId;
    public int sessionsPlayed;

    // Existing
    public float timeSpent;
    public int wrongAnswers;

    // Hint usage
    public int hintsRequested;

    // Eye tracking (per mission)
    public float eyeFocusedSeconds;
    public float eyeDistractedSeconds;
    public int eyeDistractionEvents;

    // Face tracking (per mission)
    public int faceDiscomfortEvents;
    public float faceDiscomfortSeconds;
    public float faceDiscomfortPeakScore;

    // Question response time
    public float totalAnswerTimeSeconds;
    public int questionsAnswered;
    public float AvgAnswerTimeSeconds => questionsAnswered > 0 ? totalAnswerTimeSeconds / questionsAnswered : 0f;
}

[Serializable]
public class InferenceCategoryErrorCounter
{
    public SocialInferenceCategory category;
    public int errors;
}
