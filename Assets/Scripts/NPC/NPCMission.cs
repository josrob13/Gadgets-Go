using UnityEngine;

[RequireComponent(typeof(NPCInteractable))]
public class NPCMission : MonoBehaviour, IMissionProvider
{
    [Tooltip("The mission to start when interacting with this NPC.")]
    [SerializeField] private Mission mission;

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
