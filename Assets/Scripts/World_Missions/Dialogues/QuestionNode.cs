using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestionNode", menuName = "Dialogue/Question Node")]
public class QuestionNode : DialogueNode
{
    [TextArea(1, 3)]
    public string questionText;

    [Tooltip("Hasta 4 opciones máximo")]
    public string[] options = new string[4];

    public DialogueNode onCorrect;
    public DialogueNode onIncorrect;
    public int correctOptionIndex;

    [Header("Guide Hint")]
    [TextArea(2, 5)]
    [Tooltip("Explanation shown in the VR Guide when the player asks for a hint during this question. Leave empty to show a generic 'no hint' message.")]
    public string hint;
}