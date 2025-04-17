using UnityEngine;

public abstract class BaseMissionController : MonoBehaviour {
    public Mission missionData; // ScriptableObject que contiene datos (título, preguntas, etc.)
    public int playerAttempts = 0; // Contador de intentos del jugador

    // Método que define el flujo completo de la misión (Template Method)
    public void ExecuteMission() {
        StartCinematic();
        while(!IsCinematicOver()) {
            // Durante la cinemática se lanzan preguntas
            if (IsQuestionTriggered()) {
                AskQuestion();
                if (!ValidatePlayerAnswer()) {
                    if (playerAttempts < 2)
                        GivePlayerAnotherTry();
                    else
                        ProvideGuideFeedback();
                }
            }
        }
        EndCinematic();
        FinalizeMission();
    }

    protected abstract void StartCinematic();
    protected abstract bool IsCinematicOver();
    protected abstract bool IsQuestionTriggered();
    protected abstract void AskQuestion();
    protected abstract bool ValidatePlayerAnswer();
    protected abstract void GivePlayerAnotherTry();
    protected abstract void ProvideGuideFeedback();
    protected abstract void EndCinematic();
    protected abstract void FinalizeMission();
}
