using UnityEngine;

[CreateAssetMenu(fileName = "NewPostAnswerNode", menuName = "Dialogue/PostAnswerNode")]
public class PostAnswerNode : DialogueNode
{
    [TextArea(1, 3)]
    public string correctAnswer;

    [TextArea(1, 3)]
    public string badAnswer;

    [Header("Audio (overrides base voiceClip)")]
    [Tooltip("Voice clip played when the player answered correctly.")]
    public AudioClip correctVoiceClip;

    [Tooltip("Voice clip played when the player answered incorrectly.")]
    public AudioClip badVoiceClip;
}
