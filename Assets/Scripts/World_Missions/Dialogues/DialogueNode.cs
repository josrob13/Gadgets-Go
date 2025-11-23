using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;

[System.Serializable]
public struct ActorState
{
    [Tooltip("Nombre de la animación del cuerpo. Vacío = mantendrá la postura anterior.")]
    public string BodyAnimation;

    [Tooltip("Nombre de la emoción. Vacío = mantendrá la cara anterior.")]
    public string FaceExpression;
}

[CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [TextArea(2, 6)]
    public string text;

    public DialogueNode nextNode;

    [Header("Animation")]
    [SerializeField] private string speakerAnimator;

    // NUEVA VERSION DE DIALOGUE NODE:
    [Header("Visuals")]
    public ActorState actorState;

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