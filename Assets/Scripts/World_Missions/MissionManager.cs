using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;
    private Mission currentMission;

    [Header("Mission Cameras")]
    [SerializeField] private CinemachineCamera vCamMission;

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
            Destroy(gameObject);
        }
    }

    public void StartMission(Mission mission)
    {
        currentMission = mission;
        Debug.Log($"Starting mission: {currentMission.name}");
        StartCoroutine(RunMission());
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

        System.Action<bool> handler = (isCorrect) =>
        {
            if (!isCorrect)
                PlayerProgress.Instance?.RegisterError(currentMission.name);
        };
        DialogueManager.Instance.OnQuestionAnswered += handler;

        // Activate dialogue system
        yield return DialogueManager.Instance.StartDialogue(currentMission.GetDialogueNode());

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Fade, activate the control of player, show normal UI...
        yield return UIManager.Instance.FadeOut(currentMission.GetFadeDuration());
        vCamMission.Priority--;
        normalUI.SetActive(true);
        yield return new WaitForSeconds(2f);
        yield return UIManager.Instance.FadeIn(currentMission.GetFadeDuration());
        player.enabled = true;

        // Complete the mission
        Debug.Log($"Terminating and saving mission: {currentMission.name}");
        PlayerProgress.Instance.CompleteMission(currentMission.name);
    }
    
    public string GetCurrentMissionId()
    {
        return currentMission != null ? currentMission.name : "Unkown Mission";
    }
}
