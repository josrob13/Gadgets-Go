using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

[CreateAssetMenu(fileName = "Mission", menuName = "Scriptable Objects/Mission")]
public class Mission : ScriptableObject
{
    [SerializeField] private string missionName;
    [SerializeField] private string missionDescription;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Dialogue Tree")]
    [SerializeField] private DialogueNode dialogueNode;
    [SerializeField] private List<string> dialogueLines;

    [Header("Camera")]
    [SerializeField] private string cameraID;

    public string GetMissionName()
    {
        return missionName;
    }

    public string GetMissionDescription()
    {
        return missionDescription;
    }

    public DialogueNode GetDialogueNode()
    {
        return dialogueNode;
    }

    public float GetFadeDuration()
    {
        return fadeDuration;
    }

    public List<string> GetDialogueLines()
    {
        return dialogueLines;
    }
    
    public string GetCameraID()
    {
        return cameraID;
    }
}
