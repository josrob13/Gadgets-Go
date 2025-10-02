using UnityEngine;

[CreateAssetMenu(fileName = "NewPostAnswerNode", menuName = "Dialogue/PostAnswerNode")]
public class PostAnswerNode : DialogueNode
{
    [TextArea(1, 3)]
    public string correctAnswer;
    
    [TextArea(1, 3)]
    public string badAnswer;
}
