using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_DialoguePrueba : MonoBehaviour
{
    private TextMeshPro messageText;
    private TextWriter.TextWriterSingle textWriterSingle;
    
    private void Awake()
    {
        messageText = transform.Find("MessageText").GetComponent<TextMeshPro>();

        // transform.Find("message").GetComponent<Button_UI>().ClickFunc = () =>
        // {
        //     if (textWriterSingle != null && textWriterSingle.IsActive())
        //     {
        //         textWriterSingle.WriteAllDestroy()
        //     }else {
        //         string[] messages = new string[] {
        //             "SpaceX está a punto de llegar a la Extremadura pero de Marte",
        //             "Getafe no es el centro de España",
        //             "Corrupción en el Estado de Irán"
        //         };

        //         string randomMessage = messages[Random.Range(0, messages.Length)];
        //         textWriterSingle = TextWriter.AddWriter_Static(messageText, randomMessage, 0.05f, true);
        //     }
        // };
    }

    private void Start()
    {
    }
}
