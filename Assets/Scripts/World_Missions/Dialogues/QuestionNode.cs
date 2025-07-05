using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestionNode", menuName = "Dialogue/Question Node")]
public class QuestionNode : DialogueNode
{
    [TextArea(1, 3)]
    public string questionText;   // Texto de la pregunta

    [Tooltip("Hasta 4 opciones máximo")]
    public string[] options = new string[4];
}