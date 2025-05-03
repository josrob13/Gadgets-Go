using UnityEngine;

public class GameHandler_ChatBubble : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform[] npcTransformArray;

    private void Start()
    {
        ChatBubble.Create(playerTransform, new Vector3(3, 3, 10), ChatBubble.IconType.Happy, "Invítame Mariiiito creo sinceramente que la economia global se va a a pique");
    }

    public Transform pfChatBubble;
}
