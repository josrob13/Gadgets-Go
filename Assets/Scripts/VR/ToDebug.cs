using UnityEngine;

public class ToDebug : MonoBehaviour
{
    #if UNITY_EDITOR
    private void Awake()
    {
        Debug.Log("ToDebug script started. Disabling OVRManager and OVRCameraRig for debugging in editor.");
        var ovrManager = GetComponent<OVRManager>();
        if (ovrManager != null) ovrManager.enabled = false;

        // También desactivar el tracking del CameraRig
        var ovrCameraRig = GetComponent<OVRCameraRig>();
        if (ovrCameraRig != null) ovrCameraRig.enabled = false;
    }
    #endif
}
