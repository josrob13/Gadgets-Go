using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [System.Serializable]
    public struct CameraEntry
    {
        public string ID;
        public CinemachineCamera VirtualCamera;
    }

    [SerializeField]
    private List<CameraEntry> sceneCameras = new List<CameraEntry>();

    private Dictionary<string, CinemachineCamera> cameraMap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeCameraMap();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCameraMap()
    {
        cameraMap = new Dictionary<string, CinemachineCamera>();
        foreach (var entry in sceneCameras)
        {
            if (!cameraMap.ContainsKey(entry.ID))
            {
                cameraMap.Add(entry.ID, entry.VirtualCamera);
            }
        }
    }

    public CinemachineCamera GetCamera(string cameraID)
    {
        if (cameraMap.TryGetValue(cameraID, out CinemachineCamera vcam))
        {
            return vcam;
        }
        
        Debug.LogWarning($"[CameraManager] No se encontró ninguna cámara con el ID: {cameraID}");
        return null;
    }

    public void SetCameraPriority(string cameraID, int priority)
    {
        CinemachineCamera vcam = GetCamera(cameraID);
        if (vcam != null)
        {
            vcam.Priority = priority;
        }
    }
}