using UnityEngine;

[CreateAssetMenu(fileName = "Mission", menuName = "Scriptable Objects/Mission")]
public class Mission : ScriptableObject
{
    [SerializeField] private string missionName;
    [SerializeField] private string missionDescription;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Camera Settings")]
    [SerializeField] private Vector3 cameraPosition;
    [SerializeField] private Vector3 cameraRotation;
    [SerializeField] private float panAngle = 10f;
    [SerializeField] private float panSpeed = 1f;

    [Header("Dialogue Tree")]
    [SerializeField] private DialogueNode[] dialogueNodes;

    public string GetMissionName()
    {
        return missionName;
    }

    public string GetMissionDescription()
    {
        return missionDescription;
    }

    public Vector3 GetCameraPosition()
    {
        return cameraPosition;
    }

    public Vector3 GetCameraRotation()
    {
        return cameraRotation;
    }

    public float GetPanAngle()
    {
        return panAngle;
    }

    public float GetPanSpeed()
    {
        return panSpeed;
    }

    public DialogueNode[] GetDialogueNodes()
    {
        return dialogueNodes;
    }

    public float GetFadeDuration()
    {
        return fadeDuration;
    }
}
