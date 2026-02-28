using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

[CreateAssetMenu(fileName = "Mission", menuName = "Scriptable Objects/Mission")]
public class Mission : ScriptableObject
{
    [SerializeField] private string missionName;
    [SerializeField] private string description;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Dialogue Tree")]
    [SerializeField] private DialogueNode dialogueNode;

    [Header("Camera")]
    [SerializeField] private string cameraID;
    [SerializeField] private string cameraContainerName;

    public string GetMissionName()
    {
        return missionName;
    }

    public string GetMissionDescription()
    {
        return description;
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

    public void DeactivateCameras()
    {
        // Validate that we've got a name to search for
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
