using System.Collections;
using UnityEngine;
using TMPro;
using System;
using UnityEditor.Animations;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public event Action<bool, QuestionNode> OnQuestionAnswered;

    [Header("UI References")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private QuestionUI questionUI;
    [SerializeField] private float textSpeed = 0.035f;

    [SerializeField] private DialogueNodeEvents events;
    private DialogueAnimator currentSpeaker = null;
    private Dictionary<string, DialogueAnimator> speakers = new();
    private Dictionary<QuestionNode, int> questionAttempts = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CacheSpeakerAnimators();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CacheSpeakerAnimators()
    {
        DialogueAnimator[] allSpeakers = FindObjectsOfType<DialogueAnimator>();

        foreach (DialogueAnimator speaker in allSpeakers)
        {
            // La clave es el nombre del GameObject asociado al componente
            string nameKey = speaker.gameObject.name;
            
            if (!speakers.ContainsKey(nameKey))
            {
                speakers.Add(nameKey, speaker);
                Debug.Log($"[DialogueManager] Caching speaker: {nameKey}");
            }
            else
            {
                Debug.LogWarning($"Duplicate speaker name found: {nameKey}. Skipping.");
            }
        }
    }

    public IEnumerator StartDialogue(DialogueNode start, string dialogueId = null)
    {
        if (!string.IsNullOrEmpty(dialogueId))
            AnalyticsManager.Instance?.BeginDialogue(dialogueId);

        Debug.Log("Starting dialogue...");
        questionAttempts.Clear();
        DialogueNode node = start;
        while (node != null)
        {
            Debug.Log("-------------------Se mete antes del INVOKE...");
            events?.InvokeFor(node);
            /* ANTIGUA VERSION:
            if (speakers.TryGetValue(node.GetSpeaker(), out DialogueAnimator newSpeaker))
            {
                HandleAnimations(newSpeaker, node);
            }
            */
            // NUEVA VERSION:
            UpdateSpeakerState(node);

            if (node is QuestionNode questionNode)
            {
                // Initialize attempts if not present
                if (!questionAttempts.ContainsKey(questionNode))
                    questionAttempts[questionNode] = 0;

                bool answeredCorrectly = false;
                while (!answeredCorrectly && questionAttempts[questionNode] < 3)
                {
                    yield return ShowQuestion(questionNode);
                    bool isCorrect = questionUI.SelectedIndex == questionNode.correctOptionIndex;
                    questionAttempts[questionNode]++;

                    // Call to the event, in which the errors will be registered
                    OnQuestionAnswered?.Invoke(isCorrect, questionNode);

                    if (isCorrect)
                    {
                        answeredCorrectly = true;
                        // Show correct answer message
                        yield return ShowDialogueLine(questionNode.correctText);
                    }
                    else if (questionAttempts[questionNode] < 3)
                    {
                        // Show wrong answer message
                        yield return ShowDialogueLine(questionNode.wrongText);
                    }
                }

                if (!answeredCorrectly)
                {
                    // Failed 3 times, show reveal
                    yield return ShowDialogueLine(questionNode.revealText);
                }

                // Proceed to next node
                node = questionNode.nextNode;
            }
            else
            {
                yield return ShowDialogueLine(node.text);
                node = node.nextNode;
            }
        }

        if (!string.IsNullOrEmpty(dialogueId))
            AnalyticsManager.Instance?.EndDialogue();
    }

    private void UpdateSpeakerState(DialogueNode node) {
        string speakerName = node.GetSpeaker();
        Debug.Log($"ENTRA AL UPDATE SPEAKER STATE{speakerName}");

        // Search for the character in the map
        if (speakers.TryGetValue(speakerName, out DialogueAnimator nextSpeaker))
        {
            Debug.Log($"TENEMOS EL NUEVO SPEAKER: {nextSpeaker.gameObject.name}");
            // Updating reference of current speaker
            if (currentSpeaker != nextSpeaker)
            {
                Debug.Log("EL CURRENT NO ES IGUAL AL NEXT...");
                currentSpeaker = nextSpeaker;
            }

            // Give the whole complete state to the Animator
            if (currentSpeaker != null)
            {
                Debug.Log($"Applying state for speaker: {speakerName}");
                currentSpeaker.ApplyState(node.actorState);
            }else{
                Debug.LogWarning($"Current speaker is null for speaker name: {speakerName}");
            }
        }
    }


    /*
    private void HandleAnimations(DialogueAnimator newSpeaker, DialogueNode node)
    {
        if (currentSpeaker != null && currentSpeaker != newSpeaker)
        {
            Debug.Log("Poniendo a false el trigger de hablar...");
            currentSpeaker.TurnSpeakingAnimation(node.speakingTrigger, false);
        }

        Debug.Log(newSpeaker == null ? "Nuevo speaker es null" : "Nuevo speaker NO es null");
        if (newSpeaker != null)
        {
            Debug.Log("ACTIVANDO SPEAKER...");
            newSpeaker.TurnSpeakingAnimation(node.speakingTrigger, true);
            currentSpeaker = newSpeaker;
        }
        if (node.nextNode == null)
        {
            currentSpeaker.TurnSpeakingAnimation(node.speakingTrigger, false);
            currentSpeaker = null;
        }
    }
    */

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
