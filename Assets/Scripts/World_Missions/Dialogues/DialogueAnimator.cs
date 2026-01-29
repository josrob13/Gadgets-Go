using System;
using UnityEditor.Animations;
using UnityEngine;

public class DialogueAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private EmotionController emotionController;

    [SerializeField] private string bodyLayerName = "Base Layer";
    [SerializeField] private float transitionDuration = 1.25f;
    private int bodyLayerIndex;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (emotionController == null)
            emotionController = GetComponent<EmotionController>();

        Debug.Log(animator == null ? "Speaker Animator es null" : "Speaker Animator NO es null");
    }

    public void ApplyState(ActorState state)
    {
        Debug.Log("APPLY STATE ENTRA...");
        if (animator != null && !string.IsNullOrEmpty(state.BodyAnimation))
        {
            Debug.Log($"Applying body animation: {state.BodyAnimation}");
            PlayBodyAnimation(state.BodyAnimation);
        }

        if (emotionController != null && !string.IsNullOrEmpty(state.FaceExpression))
        {
            Debug.Log($"Applying face expression: {state.FaceExpression}");
            emotionController.SetExpression(state.FaceExpression);
        }

        if (emotionController == null)
        {
            Debug.LogWarning("EmotionController is null in DialogueAnimator.");
        }
        if (string.IsNullOrEmpty(state.FaceExpression))
        {
            Debug.Log("No face expression to apply.");
        }
    }

    private void PlayBodyAnimation(string bodyAnimation)
    {
        int hash = Animator.StringToHash(bodyAnimation);
        if (animator.HasState(bodyLayerIndex, hash))
        {
            Debug.Log($"Playing body animation: {bodyAnimation} on Animator: {animator.name}");
            animator.CrossFadeInFixedTime(bodyAnimation, transitionDuration, bodyLayerIndex);
        }
        else
        {
            Debug.LogWarning($"Body animation '{bodyAnimation}' not found in Animator.");
        }
    }

    public void StopAnimation()
    {
        animator.CrossFadeInFixedTime("Idle", transitionDuration, 0);
    }
}
