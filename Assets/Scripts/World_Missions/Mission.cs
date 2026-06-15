using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

[CreateAssetMenu(fileName = "Mission", menuName = "Scriptable Objects/Mission")]
public class Mission : ScriptableObject
{
    [SerializeField] private string missionName;
    [SerializeField] private string description;

    [Header("Guía del Jugador")]
    [Tooltip("Texto que ve el jugador en la lista de misiones del guía VR. Redáctalo claro y " +
             "orientado a la acción (ej. \"Habla con Mario y Angela en el gimnasio\"). Si se deja " +
             "vacío, la guía mostrará el nombre interno de la misión como respaldo.")]
    [TextArea(2, 4)]
    [SerializeField] private string userGuide;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Dialogue Tree")]
    [SerializeField] private DialogueNode dialogueNode;

    [Header("Camera")]
    [SerializeField] private string cameraID;
    [SerializeField] private string cameraContainerName;

    [Header("Posición del Jugador en Misión")]
    [Tooltip("Distancia (m) a la que se coloca al jugador frente a los NPCs al empezar la misión. " +
             "La posición se calcula automáticamente desde el punto medio de los NPCs — el jugador " +
             "mantiene el lado por el que se acercó y se normaliza la distancia para ver bien personajes y texto.")]
    [SerializeField] private float playerStandDistance = 2.5f;
    [SerializeField] private bool lockPlayerDuringMission = true;

    [Header("OBSOLETO — no funciona desde un ScriptableObject")]
    [Tooltip("Un asset (ScriptableObject) no puede guardar referencias a objetos de escena, así que " +
             "este Transform siempre queda en null en runtime. Se conserva por compatibilidad; el " +
             "posicionamiento real usa 'playerStandDistance' + la posición de los NPCs.")]
    [SerializeField] private Transform missionStartTransform;
    [SerializeField] private bool useGlobalCoordinates = true;

    [Header("Dialogue Canvas Settings")]
    [Tooltip("Altura extra sobre el punto medio de los NPCs donde se coloca el canvas de diálogo.")]
    [SerializeField] private float canvasHeightOffset = 1.5f;
    [Tooltip("Distancia que el canvas se desplaza desde el punto medio de los NPCs HACIA el jugador. Aumenta este valor si el canvas se mete dentro de los modelos.")]
    [SerializeField] private float canvasForwardOffset = 1.0f;

    public string GetMissionName()
    {
        return missionName;
    }

    public string GetMissionDescription()
    {
        return description;
    }

    /// <summary>
    /// Texto orientado al jugador para la lista de misiones del guía VR.
    /// Si 'userGuide' está vacío, devuelve el nombre interno como respaldo.
    /// </summary>
    public string GetUserGuide()
    {
        return string.IsNullOrWhiteSpace(userGuide) ? missionName : userGuide;
    }

    public DialogueNode GetDialogueNode()
    {
        return dialogueNode;
    }

    public float GetFadeDuration()
    {
        return fadeDuration;
    }
    
    public string GetCameraID()
    {
        return cameraID;
    }

    public float GetCanvasHeightOffset()
    {
        return canvasHeightOffset;
    }

    public float GetCanvasForwardOffset()
    {
        return canvasForwardOffset;
    }

    public float GetPlayerStandDistance()
    {
        // Fallback por si un asset antiguo se deserializa con 0 (colocaría al jugador dentro de los NPCs).
        return playerStandDistance > 0f ? playerStandDistance : 2.5f;
    }

    public Vector3 GetMissionStartPosition()
    {
        if (missionStartTransform != null)
        {
            // Usar coordenadas globales (world space) para posiciones absolutas en el mundo
            // Transform.position ya es global, Transform.localPosition es relativo al padre
            return useGlobalCoordinates ? missionStartTransform.position : missionStartTransform.localPosition;
        }

        Debug.LogWarning($"[Mission] No se encontró Transform asignado para la posición de misión. Usando posición del jugador actual.");
        return Vector3.zero;
    }

    public Vector3 GetMissionStartRotation()
    {
        if (missionStartTransform != null)
        {
            // Para rotación: rotation es global (world space), localRotation es relativa al padre
            // Para posiciones absolutas, queremos que el jugador mire en una dirección absoluta
            Quaternion rotation = useGlobalCoordinates ? missionStartTransform.rotation : missionStartTransform.localRotation;
            return rotation.eulerAngles;
        }

        return Vector3.zero;
    }

    public bool ShouldLockPlayerDuringMission()
    {
        return lockPlayerDuringMission;
    }

    public void DeactivateCameras()
    {
        if (string.IsNullOrEmpty(cameraContainerName))
        {
            Debug.LogWarning("Camera container name is null or empty.");
            return;
        }

        // Search the object in the scene by its name
        GameObject container = GameObject.Find(cameraContainerName);
        if (container != null)
        {
            Debug.Log($"Found camera container: {cameraContainerName}. Deactivating cameras...");
            foreach (Transform camTransform in container.transform)
            {
                CinemachineCamera cam = camTransform.GetComponent<CinemachineCamera>();
                if (cam != null)
                {
                    Debug.Log($"Deactivating camera: {cam.name}. Priority = 0");
                    cam.Priority = 0;
                }
            }
        }else
        {
            Debug.LogWarning($"No se encontró el contenedor de cámaras con el nombre '{cameraContainerName}' en la escena.");
        }
    }
}
