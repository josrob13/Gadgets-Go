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

    private Vector3 playerStartPosition;
    private SmoothTeleport smoothTeleport;

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
        vCamMission = CameraManager.Instance.GetCamera(currentMission.GetCameraID());
        Debug.Log($"Starting mission: {currentMission.GetMissionName()}");
        StartCoroutine(RunMission());
    }

    private void ToErrorRegister(bool isCorrect)
    {
        if (!isCorrect && !string.IsNullOrEmpty(currentMission?.GetMissionName()))
            PlayerProgress.Instance?.RegisterError(currentMission.GetMissionName());
    }

    private IEnumerator RunMission()
    {
        // Obtener referencias necesarias
        smoothTeleport = smoothTeleport ?? FindObjectOfType<SmoothTeleport>();
        
        // Guardar posición inicial del jugador
        playerStartPosition = player.transform.position;

        // Desactivar movimiento del jugador (teleportación)
        player.enabled = false;
        if (smoothTeleport != null) 
            smoothTeleport.enabled = false;

        // Teleportar jugador a posición de misión si está definida en el ScriptableObject
        Vector3 missionStartPosition = currentMission.GetMissionStartPosition();
        Vector3 missionStartRotation = currentMission.GetMissionStartRotation();

        if (missionStartPosition != Vector3.zero)
        {
            player.transform.position = missionStartPosition;
            player.transform.rotation = Quaternion.Euler(missionStartRotation);
            Debug.Log($"[MissionManager] Jugador teleportado a posición de misión: {missionStartPosition}");
        }
        else
        {
            Debug.LogWarning("[MissionManager] No se pudo calcular posición de misión. El jugador permanece en su posición actual.");
        }

        yield return UIManager.Instance.FadeOut(currentMission.GetFadeDuration());

        vCamMission.Priority++;
        normalUI.SetActive(false);

        yield return new WaitForSeconds(2f);

        Debug.Log($"Mission Name: {currentMission.GetMissionName()} - Iniciando diálogo");

        yield return UIManager.Instance.FadeIn(currentMission.GetFadeDuration());

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DialogueManager.Instance.OnQuestionAnswered += ToErrorRegister;
        try
        {
            // Iniciar sistema de diálogos
            yield return DialogueManager.Instance.StartDialogue(currentMission.GetDialogueNode());
        }
        finally
        {
            DialogueManager.Instance.OnQuestionAnswered -= ToErrorRegister;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Fade out y limpieza
        yield return UIManager.Instance.FadeOut(currentMission.GetFadeDuration());
        currentMission.DeactivateCameras();
        normalUI.SetActive(true);
        yield return new WaitForSeconds(2f);
        yield return UIManager.Instance.FadeIn(currentMission.GetFadeDuration());

        // Reactivar movimiento del jugador y devolverlo a su posición anterior
        player.enabled = true;
        if (smoothTeleport != null) 
            smoothTeleport.enabled = true;
        
        player.transform.position = playerStartPosition;
        Debug.Log($"[MissionManager] Jugador liberado. Movimiento reactivado en posición anterior.");

        // Completar la misión
        Debug.Log($"Finalizando y guardando misión: {currentMission.GetMissionName()}");
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
