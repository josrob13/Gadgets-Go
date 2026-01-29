using UnityEngine;

public class EmotionController : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Animator animator;
    [SerializeField] private string layerName = "Emotion_Real";
    [SerializeField] private float transitionDuration = 0.25f;

    private const string PREFIX = "A_FacePose_"; 
    private int layerIndex;

    void Start()
    {
        Debug.LogError("ENTRA EN EMOITION CONTROLLER...");
        if (animator == null) animator = GetComponent<Animator>();
        Debug.LogError("SALE SEGUNDA LINEA EN EMOITION CONTROLLER...");

        layerIndex = animator.GetLayerIndex(layerName);

        if (layerIndex == -1)
        {
            Debug.LogError($"No se encontró la capa '{layerName}' en el Animator. Revisa el nombre.");
        }
        else
        {
            animator.SetLayerWeight(layerIndex, 1f);
            SetExpression("Neutral");
        }
    }

    public void SetExpression(string emotionName)
    {
        if (layerIndex == -1)
        {
            Debug.Log($"Cannot set expression because layer '{layerName}' was not found.");
            return;
        }
        
        string fullStateName;
        fullStateName = emotionName.StartsWith(PREFIX) ? emotionName : PREFIX + emotionName;

        if (HasState(fullStateName))
        {
            Debug.Log($"Playing body animation: {fullStateName} on Animator: {animator.name}");
            animator.CrossFadeInFixedTime(fullStateName, transitionDuration, layerIndex);
            animator.SetLayerWeight(layerIndex, 1f);
        }
        else
        {
            Debug.LogWarning($"La emoción '{fullStateName}' no existe en el Animator.");
        }
    }

    private bool HasState(string stateName)
    {
        int hash = Animator.StringToHash(stateName);
        return animator.HasState(layerIndex, hash);
    }
}