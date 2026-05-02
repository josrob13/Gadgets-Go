using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;
    private Mission currentMission;
    private CinemachineCamera vCamMission;

    [Header("Player Scripts")]
    [SerializeField] private Player player;

    [Header("Player UI")]
    [SerializeField] private GameObject normalUI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning($"[MissionManager] Se ha encontrado una segunda instancia de MissionManager en la escena. El objeto '{this.name}' será destruido para mantener el singleton.");
            Destroy(gameObject);
        }
    }

    public void StartMission(Mission mission)
    {
        currentMission = mission;
        vCamMission = CameraManager.Instance.GetCamera(currentMission.GetCameraID());
        Debug.Log($"Starting mission: {currentMission.GetMissionName()}");
        StartCoroutine(RunMission());
    }

    private void ToErrorRegister(bool isCorrect, QuestionNode questionNode)
    {
        if (!isCorrect && !string.IsNullOrEmpty(currentMission?.GetMissionName()))
        {
            PlayerProgress.Instance?.RegisterError(currentMission.GetMissionName());
            AnalyticsManager.Instance?.RegisterInferenceError(questionNode.inferenceCategory);
            AnalyticsManager.Instance?.RegisterCurrentDialogueWrongAnswer();
        }
    }

    private IEnumerator RunMission()
    {
        player.enabled = false;
        yield return UIManager.Instance.FadeOut(currentMission.GetFadeDuration());

        vCamMission.Priority++;
        normalUI.SetActive(false);

        yield return new WaitForSeconds(2f);

        Debug.Log($"Mission Name: {currentMission.GetMissionName()} HASTA AQUI GUAY");

        yield return UIManager.Instance.FadeIn(currentMission.GetFadeDuration());

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DialogueManager.Instance.OnQuestionAnswered += ToErrorRegister;
        try
        {
            yield return DialogueManager.Instance.StartDialogue(currentMission.GetDialogueNode(), currentMission.GetMissionName());
        }
        finally
        {
            DialogueManager.Instance.OnQuestionAnswered -= ToErrorRegister;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Fade, activate the control of player, show normal UI...
        yield return UIManager.Instance.FadeOut(currentMission.GetFadeDuration());
        // OLD VERSION:
        //vCamMission.Priority--;
        currentMission.DeactivateCameras();
        normalUI.SetActive(true);
        yield return new WaitForSeconds(2f);
        yield return UIManager.Instance.FadeIn(currentMission.GetFadeDuration());
        player.enabled = true;

        // Complete the mission
        Debug.Log($"Terminating and saving mission: {currentMission.GetMissionName()}");
        if (PlayerProgress.Instance.CompleteMission(currentMission.GetMissionName()))
        {
            GameHandler.Instance?.NextWorld();
        }
    }

    public string GetCurrentMissionId()
    {
        return currentMission != null ? currentMission.GetMissionName() : "Unkown Mission";
    }
}
