using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

[CreateAssetMenu(fileName = "Mission", menuName = "Scriptable Objects/Mission")]
public class Mission : ScriptableObject
{
    [SerializeField] private string missionName;
    [SerializeField] private string description;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Dialogue Tree")]
    [SerializeField] private DialogueNode dialogueNode;

    [Header("Camera")]
    [SerializeField] private string cameraID;

    public string GetMissionName()
    {
        return missionName;
    }

    public string GetMissionDescription()
    {
        return description;
    }

    public DialogueNode GetDialogueNode()
    {
        return dialogueNode;
    }

    public float GetFadeDuration()
    {
        return fadeDuration;
    }
    
    public string GetCameraID()
    {
        return cameraID;
    }
}
