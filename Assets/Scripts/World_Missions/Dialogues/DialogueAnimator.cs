using UnityEditor.Animations;
using UnityEngine;

public class DialogueAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    void Awake()
    {
        Debug.Log(animator == null ? "Speaker Animator es null" : "Speaker Animator NO es null");
    }

    public void TurnSpeakingAnimation(string triggerName, bool value)
    {
        Debug.Log("ENTRA A TURN SPEAKING ANIMATION...");
        if (animator != null)
        {
            animator.SetBool(triggerName, value);
        }
    }
}
