using UnityEngine;

public class Fade : MonoBehaviour
{

    public Animator fadeAnimator;

    public void FadeOut()
    {
        fadeAnimator.Play("FadeOut");
    }
    
    public void FadeIn()
    {
        fadeAnimator.Play("FadeIn");
    }
}
