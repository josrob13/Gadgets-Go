using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [TextArea(2, 6)]
    public string text;

    public DialogueNode nextNode;

    [Header("Animation")]
    [SerializeField] private string speakerAnimator;

    public string speakingTrigger = "Talk";

    public string GetText()
    {
        return text;
    }

    public void SetText(string newText)
    {
        text = newText;
    }
    
    public string GetSpeaker()
    {
        return speakerAnimator;
    }
}