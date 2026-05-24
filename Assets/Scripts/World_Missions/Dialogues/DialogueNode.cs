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
    public Mission relatedMission;

    [TextArea(2, 6)]
    public string text;

    public DialogueNode nextNode;

    [Header("Animation")]
    [SerializeField] private string speakerAnimator;

    // NUEVA VERSION DE DIALOGUE NODE:
    [Header("Visuals")]
    public ActorState actorState;

    [Header("Audio")]
    [Tooltip("Voice clip that plays when this dialogue line is shown. Leave empty for no voice.")]
    public AudioClip voiceClip;

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