using System.Collections;
using UnityEngine;
using TMPro;
using System;
using UnityEditor.Animations;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public event Action<bool> OnQuestionAnswered;

    [Header("UI References")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private QuestionUI questionUI;
    [SerializeField] private DialogueCanvasController canvasController;
    [SerializeField] private float textSpeed = 0.035f;

    [SerializeField] private DialogueNodeEvents events;
    private DialogueAnimator currentSpeaker = null;
    private Dictionary<string, DialogueAnimator> speakers = new();

    // Two independent pause sources — dialogue only runs when neither is active.
    private bool _faceTrackingPaused = false;
    private bool _guidePaused = false;
    private bool IsPaused => _faceTrackingPaused || _guidePaused;

    private bool _lastAnswerWasCorrect = false;

    public QuestionNode ActiveQuestion { get; private set; }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CacheSpeakerAnimators();

            if (canvasController == null)
                canvasController = GetComponentInChildren<DialogueCanvasController>();
            if (canvasController == null)
                canvasController = FindFirstObjectByType<DialogueCanvasController>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (FaceTrackingManager.Instance != null)
            FaceTrackingManager.Instance.OnDiscomfortStateChanged += OnDiscomfortStateChanged;
        else
            Debug.LogWarning("[DialogueManager] FaceTrackingManager no encontrado — la pausa por detección facial está desactivada.");
    }

    private void OnDestroy()
    {
        if (FaceTrackingManager.Instance != null)
            FaceTrackingManager.Instance.OnDiscomfortStateChanged -= OnDiscomfortStateChanged;
    }

    /// <summary>
    /// Pauses or resumes the dialogue in response to facial discomfort detection.
    /// </summary>
    private void OnDiscomfortStateChanged(bool isDiscomfort)
    {
        _faceTrackingPaused = isDiscomfort;
        if (isDiscomfort)
            DialogueAudioManager.Instance?.PauseVoice();
        else if (!_guidePaused)
            DialogueAudioManager.Instance?.ResumeVoice();
        Debug.Log($"[DialogueManager] Di\u00e1logo {(isDiscomfort ? "pausado" : "reanudado")} por detecci\u00f3n facial.");
    }

    /// <summary>
    /// Called by VRGuideController when the guide panel opens or closes.
    /// Pauses typing and audio so the player can read without missing text.
    /// </summary>
    public void SetGuidePause(bool paused)
    {
        Debug.Log($"[DialogueManager] SetGuidePause({paused}) — _faceTrackingPaused={_faceTrackingPaused}, IsPaused will be={paused || _faceTrackingPaused}");
        _guidePaused = paused;
        if (paused)
            DialogueAudioManager.Instance?.PauseVoice();
        else if (!_faceTrackingPaused)
            DialogueAudioManager.Instance?.ResumeVoice();
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

    public IEnumerator StartDialogue(DialogueNode start, Vector3? canvasPosition = null, Quaternion? canvasRotation = null)
    {
        if (canvasController != null)
            canvasController.OnDialogueStart(canvasPosition, canvasRotation);
        
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

            Transform speakerTransform = currentSpeaker != null ? currentSpeaker.transform : null;

            if (node is QuestionNode questionNode)
            {
                // Read the question aloud while choices are displayed
                DialogueAudioManager.Instance?.PlayVoice(questionNode.voiceClip, speakerTransform);

                yield return ShowQuestion(questionNode);

                // Stop question voice once the player has answered
                DialogueAudioManager.Instance?.StopVoice(fade: true);

                bool isCorrect = questionUI.SelectedIndex == questionNode.correctOptionIndex;
                _lastAnswerWasCorrect = isCorrect;

                // Call to the event, in which the errors will be registered
                OnQuestionAnswered?.Invoke(isCorrect);
                if (questionNode.nextNode is PostAnswerNode p)
                {
                    node = p;
                    node.SetText(isCorrect ? p.correctAnswer : p.badAnswer);
                }
            }
            else
            {
                // PostAnswerNode uses its own correct/bad clips; all others use voiceClip
                AudioClip clipToPlay = node.voiceClip;
                if (node is PostAnswerNode pan)
                    clipToPlay = _lastAnswerWasCorrect ? pan.correctVoiceClip : pan.badVoiceClip;

                // PlayVoice stops any currently playing clip before starting the new one,
                // making overlapping voices architecturally impossible.
                DialogueAudioManager.Instance?.PlayVoice(clipToPlay, speakerTransform);

                yield return ShowDialogueLine(node.text);
                node = node.nextNode;
            }
        }

        // Fade out any remaining audio and deactivate the canvas
        DialogueAudioManager.Instance?.StopVoice(fade: true);
        if (canvasController != null)
            canvasController.OnDialogueEnd();
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
            yield return new WaitUntil(() => !IsPaused);

            if (dialogueUI.NextPressed)
            {
                dialogueUI.SetText(line);
                break;
            }

            dialogueUI.GetTextComponent().text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        dialogueUI.SetText(line);
        dialogueUI.NextPressed = false;

        yield return new WaitUntil(() => dialogueUI.NextPressed && !IsPaused);


        dialogueUI.Hide();
    }

    private IEnumerator ShowQuestion(QuestionNode questionNode)
    {
        ActiveQuestion = questionNode;
        questionUI.ShowQuestion(questionNode.questionText, questionNode.options);
        yield return new WaitUntil(() => questionUI.HasAnswered);
        ActiveQuestion = null;

        int selectedIndex = questionUI.SelectedIndex;
        Debug.Log($"Respuesta seleccionada: {selectedIndex} – '{questionNode.options[selectedIndex]}'");

        questionUI.Hide();
    }
}
