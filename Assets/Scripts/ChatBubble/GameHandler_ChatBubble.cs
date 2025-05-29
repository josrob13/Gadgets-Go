using CodeMonkey.Utils;
using UnityEngine;

public class GameHandler_ChatBubble : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform[] npcTransformArray;

    private void Start()
    {
        //ChatBubble.Create(playerTransform, new Vector3(3, 3, 10), ChatBubble.IconType.Happy, "Invítame Mariiiito creo sinceramente que la economia global se va a a pique");

        // CodeMonkey Utils simulates actions every 1.5 seconds

        /* THIS IS MONKEY UTILS, MADE FOR EACH NPC TO GET A RANDOM MESSAGE */
        // FunctionPeriodic.Create(() =>
        // {
        //     Transform npcTransform = npcTransformArray[Random.Range(0, npcTransformArray.Length)];
        //     string message = GetRandomMessage();

        //     ChatBubble.IconType[] icons = new ChatBubble.IconType[] {
        //         ChatBubble.IconType.Happy,
        //         ChatBubble.IconType.Neutral,
        //         ChatBubble.IconType.Angry
        //     };
        //     ChatBubble.IconType icon = icons[Random.Range(0, icons.Length)];

        //     ChatBubble.Create(npcTransform, new Vector3(3, 8), icon, message);
        // }, 5.0f);
    }

    private string GetRandomMessage()
    {
        string[] messages = new string[]
        {
            "SpaceX está a punto de llegar a la Extremadura pero de Marte",
            "Getafe no es el centro de España",
            "Corrupción en el Estado de Irán"
        };

        return messages[Random.Range(0, messages.Length)];
    }

    public Transform pfChatBubble;
}
