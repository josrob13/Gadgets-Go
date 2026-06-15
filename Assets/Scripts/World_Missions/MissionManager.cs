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
        // En VR no se usa Cinemachine (vCamMission solo se aplica en la rama desktop de RunMission),
        // así que protegemos contra CameraManager ausente para no lanzar NPE y abortar la misión.
        vCamMission = CameraManager.Instance != null
            ? CameraManager.Instance.GetCamera(currentMission.GetCameraID())
            : null;
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
        // ─── Get VR locomotion references ───
        smoothTeleport = smoothTeleport ?? FindObjectOfType<SmoothTeleport>();
        EyeTracking eyeTracker = FindObjectOfType<EyeTracking>();
        eyeTracker?.ResetTracking();
        Debug.Log($"[Analytics] EyeTracking found: {eyeTracker != null}");
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
        // En VR no existe el componente Player (se usa PlayerVR); el movimiento se bloquea
        // vía smoothTeleport / fpLocomotor. Protegemos player para no lanzar NPE.
        if (player != null)
            player.enabled = false;
        if (smoothTeleport != null)
            smoothTeleport.enabled = false;
        if (fpLocomotor != null)
            fpLocomotor.enabled = false;

        // ─── Find the NPCs that belong to this mission (used for player & canvas placement) ───
        NPCMission[] allNPCMissions = FindObjectsOfType<NPCMission>();
        Vector3 npcMidpoint = Vector3.zero;
        int npcCount = 0;
        Vector3 firstNpc = Vector3.zero;
        Vector3 secondNpc = Vector3.zero;

        foreach (NPCMission npcMission in allNPCMissions)
        {
            if (npcMission.GetMission() == currentMission)
            {
                Vector3 npcPos = npcMission.transform.position;
                npcMidpoint += npcPos;
                if (npcCount == 0) firstNpc = npcPos;
                else if (npcCount == 1) secondNpc = npcPos;
                npcCount++;
                Debug.Log($"[MissionManager] NPC encontrado para misión: {npcMission.gameObject.name} en {npcPos}");
            }
        }

        Transform eyeTransform = isVR ? ovrRig.centerEyeAnchor : playerRoot;

        // ─── Position the player in front of the NPCs at a readable distance ───
        // Mission es un ScriptableObject y no puede guardar un Transform de escena, así que la
        // posición se calcula desde el punto medio de los NPCs: el jugador mantiene el lado por el
        // que se acercó y se normaliza la distancia para que vea bien a los personajes y el texto.
        Vector3? canvasPosition = null;
        Quaternion? canvasRotation = null;
        Vector3? missionPlayerPos = null;
        Quaternion? missionPlayerRot = null;

        if (npcCount > 0)
        {
            npcMidpoint /= npcCount;

            // Dirección horizontal desde los NPCs hacia donde está el jugador ahora (lado de aproximación).
            Vector3 directionToPlayer = eyeTransform.position - npcMidpoint;
            directionToPlayer.y = 0f;
            if (directionToPlayer.sqrMagnitude < 0.0001f)
                directionToPlayer = Vector3.back; // fallback: jugador justo sobre el punto medio
            directionToPlayer.Normalize();

            // Dirección sobre la que se coloca al jugador.
            // Con 2 NPCs: la PERPENDICULAR a la línea que los une → el jugador queda centrado
            // (bisectriz, equidistante de ambos), no ladeado. Elegimos el lado por el que se acercó.
            // Con 1 o >2 NPCs: usamos la dirección de aproximación.
            Vector3 standDir = directionToPlayer;
            if (npcCount == 2)
            {
                Vector3 axis = secondNpc - firstNpc;
                axis.y = 0f;
                Vector3 perp = new Vector3(axis.z, 0f, -axis.x); // perpendicular horizontal a la línea NPC-NPC
                if (perp.sqrMagnitude > 0.0001f)
                {
                    perp.Normalize();
                    // Quedarse en el lado donde está el jugador (no teleportarlo detrás de los NPCs).
                    if (Vector3.Dot(perp, directionToPlayer) < 0f)
                        perp = -perp;
                    standDir = perp;
                }
            }

            // Punto deseado para la CABEZA: a 'standDistance' del punto medio, centrado frente a los NPCs.
            float standDistance = currentMission.GetPlayerStandDistance();
            Vector3 desiredHeadXZ = npcMidpoint + standDir * standDistance;

            // La cabeza (centerEyeAnchor) está desplazada respecto a la raíz del rig por el tracking
            // físico; compensamos ese offset XZ para que la cabeza acabe exactamente a la distancia deseada.
            Vector3 headOffset = eyeTransform.position - playerRoot.position;
            headOffset.y = 0f;
            Vector3 targetRootPos = desiredHeadXZ - headOffset;
            targetRootPos.y = npcMidpoint.y; // nivel de suelo de los NPCs (pivote en los pies)

            // Guardamos la posición/rotación objetivo y la aplicamos DESPUÉS del fundido a negro,
            // para que el jugador no vea el salto (confort VR / sensibilidad sensorial TEA).
            missionPlayerPos = targetRootPos;

            // En desktop rotamos el rig para mirar a los NPCs. En VR NO forzamos la rotación
            // (marea y es imposible sobreescribir la orientación física de la cabeza); el usuario
            // ya está mirando al NPC en el momento de interactuar.
            if (!isVR)
                missionPlayerRot = Quaternion.LookRotation(-standDir, Vector3.up);

            Debug.Log($"[MissionManager] Posición de misión calculada: {standDistance}m frente a los NPCs (raíz={targetRootPos}).");

            // ─── Canvas de diálogo: entre los NPCs y el jugador (mirando hacia el jugador) ───
            // Alineado con 'standDir' (donde queda el jugador), así el canvas queda centrado frente a él.
            canvasPosition = npcMidpoint
                + standDir * currentMission.GetCanvasForwardOffset()
                + Vector3.up * currentMission.GetCanvasHeightOffset();
            canvasRotation = Quaternion.LookRotation(-standDir, Vector3.up);
        }
        else
        {
            Debug.LogWarning("[MissionManager] No se encontraron NPCs con esta misión asignada. El jugador no se reposiciona y el canvas usará fallback.");
        }

        // ─── Fade out and preparation ───
        yield return UIManager.Instance.FadeOut(currentMission.GetFadeDuration());

        // Reposicionar al jugador AHORA que la pantalla está en negro (sin salto visible).
        if (missionPlayerPos.HasValue)
            playerRoot.position = missionPlayerPos.Value;
        if (missionPlayerRot.HasValue)
            playerRoot.rotation = missionPlayerRot.Value;

        // Only use Cinemachine if is NOT VR (desktop)
        if (!isVR && vCamMission != null)
            vCamMission.Priority++;

        if (normalUI != null)
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
                currentMission.GetMissionName(),
                canvasPosition,
                canvasRotation
            );
        }
        finally
        {
            DialogueManager.Instance.OnQuestionAnswered -= ToErrorRegister;
        }

        if (eyeTracker != null)
            Debug.Log($"[Analytics] Eye end — Focused: {eyeTracker.FocusedTime:F1}s | Distracted: {eyeTracker.DistractedTime:F1}s | Events: {eyeTracker.DistractionCount}");
        else
            Debug.LogWarning("[Analytics] EyeTracking is null — no eye data registered.");
        AnalyticsManager.Instance?.RegisterMissionEyeData(currentMission.GetMissionName(), eyeTracker);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ─── Fade out and clean up ───
        yield return UIManager.Instance.FadeOut(currentMission.GetFadeDuration());
        currentMission.DeactivateCameras();
        if (normalUI != null)
            normalUI.SetActive(true);

        // Devolver al jugador a su posición previa AHORA que la pantalla está en negro (sin salto visible).
        playerRoot.position = playerStartPosition;
        playerRoot.rotation = playerStartRotation;

        yield return new WaitForSeconds(2f);
        yield return UIManager.Instance.FadeIn(currentMission.GetFadeDuration());

        // ─── Reactivate locomotion (ya con la pantalla visible) ───
        if (player != null)
            player.enabled = true;
        if (smoothTeleport != null)
            smoothTeleport.enabled = true;
        if (fpLocomotor != null)
            fpLocomotor.enabled = true;

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
