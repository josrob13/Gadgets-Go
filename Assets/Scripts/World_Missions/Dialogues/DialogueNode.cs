using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [TextArea(2, 6)]
    public string text;            // El texto que mostrará este nodo

    public DialogueNode nextNode;  // Referencia al siguiente nodo (puede ser null si es el último)
}