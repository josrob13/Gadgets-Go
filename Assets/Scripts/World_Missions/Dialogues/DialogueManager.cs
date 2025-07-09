using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private QuestionUI questionUI;
    [SerializeField] private float textSpeed = 0.6f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator StartDialogue(DialogueNode start)
    {
        Debug.Log("Starting dialogue...");
        var node = start;
        while (node != null)
        {
            if (node is QuestionNode questionNode)
            {
                yield return ShowQuestion(questionNode);
                node = (questionUI.SelectedIndex == questionNode.correctOptionIndex)
                    ? questionNode.onCorrect : questionNode.onIncorrect;
            }
            else
            {
                yield return ShowDialogueLine(node.text);
                node = node.nextNode;
            }
        }
    }

    public IEnumerator ShowDialogueLine(string line)
    {
        dialogueUI.Show();
        dialogueUI.SetText(string.Empty);
        dialogueUI.NextPressed = false;

        foreach (char c in line)
        {
            if (dialogueUI.NextPressed)
            {
                dialogueUI.SetText(line);
                break;
            }

            dialogueUI.GetTextComponent().text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        dialogueUI.SetText(line);
        // Espera a que el usuario presione el botón "Siguiente"
        dialogueUI.NextPressed = false;

        yield return new WaitUntil(() => dialogueUI.NextPressed);

        dialogueUI.Hide();
    }

    private IEnumerator ShowQuestion(QuestionNode questionNode)
    {
        questionUI.ShowQuestion(questionNode.questionText, questionNode.options);
        yield return new WaitUntil(() => questionUI.HasAnswered);

        int selectedIndex = questionUI.SelectedIndex;
        Debug.Log($"Respuesta seleccionada: {selectedIndex} – “{questionNode.options[selectedIndex]}”");

        questionUI.Hide();
    }
}
