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

    private void ToErrorRegister(bool isCorrect)
    {
        if (!isCorrect && !string.IsNullOrEmpty(currentMission?.GetMissionName()))
            PlayerProgress.Instance?.RegisterError(currentMission.GetMissionName());
    }

    private IEnumerator RunMission()
    {
        // ─── Get VR locomotion references ───
        smoothTeleport = smoothTeleport ?? FindObjectOfType<SmoothTeleport>();
        OVRCameraRig ovrRig = FindObjectOfType<OVRCameraRig>();
        bool isVR = ovrRig != null;

        // Find FirstPersonLocomotor to lock it during the mission
        Oculus.Interaction.Locomotion.FirstPersonLocomotor fpLocomotor = null;
        if (isVR)
            fpLocomotor = FindObjectOfType<Oculus.Interaction.Locomotion.FirstPersonLocomotor>();

        // ─── Save player's initial position ───
        Transform playerRoot = isVR ? ovrRig.transform : player.transform;
        playerStartPosition = playerRoot.position;
        Quaternion playerStartRotation = playerRoot.rotation;

        // ─── Disable locomotion (but NOT head tracking) ───
        player.enabled = false;
        if (smoothTeleport != null) 
            smoothTeleport.enabled = false;
        if (fpLocomotor != null)
            fpLocomotor.enabled = false;

        // ─── Teleport player to mission position ───
        Vector3 missionStartPosition = currentMission.GetMissionStartPosition();
        Vector3 missionStartRotation = currentMission.GetMissionStartRotation();

        if (missionStartPosition != Vector3.zero)
        {
            playerRoot.position = missionStartPosition;
            playerRoot.rotation = Quaternion.Euler(missionStartRotation);
            Debug.Log($"[MissionManager] Jugador teleportado a posición de misión: {missionStartPosition}");
        }
        else
        {
            Debug.LogWarning("[MissionManager] No se pudo calcular posición de misión. El jugador permanece en su posición actual.");
        }

        // ─── Calculate dialogue canvas position (midpoint between mission NPCs) ───
        Vector3? canvasPosition = null;
        Quaternion? canvasRotation = null;

        // Find all NPCMission in the scene that have this mission assigned
        NPCMission[] allNPCMissions = FindObjectsOfType<NPCMission>();
        Vector3 npcMidpoint = Vector3.zero;
        int npcCount = 0;

        foreach (NPCMission npcMission in allNPCMissions)
        {
            if (npcMission.GetMission() == currentMission)
            {
                npcMidpoint += npcMission.transform.position;
                npcCount++;
                Debug.Log($"[MissionManager] NPC encontrado para misión: {npcMission.gameObject.name} en {npcMission.transform.position}");
            }
        }

        if (npcCount > 0)
        {
            npcMidpoint /= npcCount;

            // Calculate the horizontal direction from the NPCs to the player
            Transform eyeTransform = isVR ? ovrRig.centerEyeAnchor : playerRoot;
            Vector3 directionToPlayer = eyeTransform.position - npcMidpoint;
            directionToPlayer.y = 0; // Only horizontal direction
            directionToPlayer.Normalize();

            // Move the canvas from the midpoint of the NPCs TOWARDS the player
            // This prevents the canvas from appearing inside the 3D models of the NPCs.
            // Adjust "Canvas Forward Offset" in the Mission ScriptableObject.
            Vector3 finalCanvasPosition = npcMidpoint 
                + directionToPlayer * currentMission.GetCanvasForwardOffset()
                + Vector3.up * currentMission.GetCanvasHeightOffset();

            canvasPosition = finalCanvasPosition;

            // The canvas looks back towards the NPCs (+Z points to NPCs, -Z to player).
            // The front face of the Canvas WorldSpace (visible side) is the local -Z face,
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                canvasRotation = Quaternion.LookRotation(-directionToPlayer, Vector3.up);
            }
        }
        else
        {
            Debug.LogWarning("[MissionManager] No se encontraron NPCs con esta misión asignada. El canvas usará fallback.");
        }

        // ─── Fade out and preparation ───
        yield return UIManager.Instance.FadeOut(currentMission.GetFadeDuration());

        // Only use Cinemachine if is NOT VR (desktop)
        if (!isVR && vCamMission != null)
            vCamMission.Priority++;
        
        normalUI.SetActive(false);

        yield return new WaitForSeconds(2f);

        Debug.Log($"Mission Name: {currentMission.GetMissionName()} - Iniciando diálogo");

        yield return UIManager.Instance.FadeIn(currentMission.GetFadeDuration());

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ─── Execute dialogue system with canvas position ───
        DialogueManager.Instance.OnQuestionAnswered += ToErrorRegister;
        try
        {
            yield return DialogueManager.Instance.StartDialogue(
                currentMission.GetDialogueNode(), 
                canvasPosition, 
                canvasRotation
            );
        }
        finally
        {
            DialogueManager.Instance.OnQuestionAnswered -= ToErrorRegister;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ─── Fade out and clean up ───
        yield return UIManager.Instance.FadeOut(currentMission.GetFadeDuration());
        currentMission.DeactivateCameras();
        normalUI.SetActive(true);
        yield return new WaitForSeconds(2f);
        yield return UIManager.Instance.FadeIn(currentMission.GetFadeDuration());

        // ─── Reactivate locomotion and return player to its previous position ───
        player.enabled = true;
        if (smoothTeleport != null) 
            smoothTeleport.enabled = true;
        if (fpLocomotor != null)
            fpLocomotor.enabled = true;
        
        playerRoot.position = playerStartPosition;
        playerRoot.rotation = playerStartRotation;
        Debug.Log($"[MissionManager] Jugador liberado. Movimiento reactivado en posición anterior.");

        // ─── Complete the mission ───
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
