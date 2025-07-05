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
        // 2) Posicionar y empezar paneo de cámara
        vCamMission.Priority++;
        normalUI.SetActive(false);
        // un pequeño delay para que el jugador lo perciba
        yield return new WaitForSeconds(2f);

        // 3) Ejecutar diálogo / preguntas
        // yield return DialogueManager.Instance.RunDialogue(currentMission.GetDialogueNodes());

        // // 4) Detener paneo si lo deseas
        // CameraController.Instance.StopPan();

        // 5) Fade in y fin
        yield return UIManager.Instance.FadeIn(currentMission.GetFadeDuration());
    }
}
