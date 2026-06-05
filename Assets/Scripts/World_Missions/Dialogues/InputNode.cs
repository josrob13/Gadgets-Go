using UnityEngine;

[CreateAssetMenu(fileName = "NewInputNode", menuName = "Dialogue/Input Node")]
public class InputNode : DialogueNode
{
    [Header("Input Settings")]
    [TextArea(1, 3)]
    [Tooltip("Prompt shown above the input field.")]
    public string inputPrompt = "¿Cómo te llamas?";

    [Tooltip("Placeholder text shown inside the empty input field.")]
    public string placeholder = "Escribe tu nombre...";

    [Tooltip("Maximum number of characters the player can enter.")]
    public int maxCharacters = 20;
}
