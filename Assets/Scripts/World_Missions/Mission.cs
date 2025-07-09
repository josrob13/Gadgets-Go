using System.Collections.Generic;
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
}
