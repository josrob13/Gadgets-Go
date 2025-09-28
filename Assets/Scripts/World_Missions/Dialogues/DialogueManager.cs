using System.Collections;
using UnityEngine;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public event Action<bool> OnQuestionAnswered;

    [Header("UI References")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private QuestionUI questionUI;
    [SerializeField] private float textSpeed = 1f;

    [SerializeField] private DialogueNodeEvents events;

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
            Debug.Log("-------------------Se mete antes del INVOKE...");
            events?.InvokeFor(node);
            if (node is QuestionNode questionNode)
            {
                yield return ShowQuestion(questionNode);
                bool isCorrect = questionUI.SelectedIndex == questionNode.correctOptionIndex;

                // Call to the event, in which the errors will be registered
                OnQuestionAnswered?.Invoke(isCorrect);
                node = isCorrect ? questionNode.onCorrect : questionNode.onIncorrect;
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
