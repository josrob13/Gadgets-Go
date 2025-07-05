using System.Collections;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    // [Header("UI References")]
    // [SerializeField] private DialogueUI dialogueUI;
    // [SerializeField] private QuestionUI questionUI;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ESTO HAY QUE CAMBIARLOA AAAAAAAAAAAAA
            // if (dialogueUI == null) dialogueUI = FindObjectOfType<DialogueUI>();
            // if (questionUI == null) questionUI = FindObjectOfType<QuestionUI>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /*
    public IEnumerator RunDialogue(DialogueNode[] nodes)
    {
        foreach (var node in nodes)
        {
            // Si es un QuestionNode, mostramos las opciones
            if (node is QuestionNode qNode)
            {
                // Mostrar la UI de pregunta y esperar respuesta
                questionUI.ShowQuestion(qNode.questionText, qNode.options);
                yield return new WaitUntil(() => questionUI.HasAnswered);

                // (Opcional) obtén el índice elegido:
                int selectedIndex = questionUI.SelectedIndex;
                Debug.Log($"Respuesta seleccionada: {selectedIndex} – “{qNode.options[selectedIndex]}”");

                questionUI.Hide();
            }
            else
            {
                // Nodo de diálogo genérico
                dialogueUI.ShowDialogue(node.text);
                yield return new WaitUntil(() => dialogueUI.NextPressed);

                dialogueUI.ResetNext();  // preparamos para el siguiente nodo
                dialogueUI.Hide();
            }
        }
    }
    */
}
