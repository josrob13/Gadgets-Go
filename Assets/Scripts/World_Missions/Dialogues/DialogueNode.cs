using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [TextArea(2, 6)]
    public string text;

    public DialogueNode nextNode;

    public string GetText()
    {
        return text;
    }
    
    public void SetText(string newText)
    {
        text = newText;
    }
}