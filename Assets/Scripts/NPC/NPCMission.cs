using UnityEngine;

[RequireComponent(typeof(NPCInteractable))]
public class NPCMission : MonoBehaviour, IMissionProvider
{
    [Tooltip("The mission to start when interacting with this NPC.")]
    [SerializeField] private Mission mission;

    /// <summary>
    /// Devuelve la misión asignada a este NPC. Usado por MissionManager para encontrar
    /// los NPCs que participan en una misión y calcular la posición del canvas de diálogo.
    /// </summary>
    public Mission GetMission() => mission;

    public void StartMission()
    {
        if (mission != null)
        {
            MissionManager.Instance.StartMission(mission);
        }
        else
        {
            Debug.LogWarning($"[{name}] no tiene asignada ninguna MissionDefinition.");
        }
    }
}
