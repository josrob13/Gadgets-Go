using UnityEngine;

public class MainMenuVRTarget : MonoBehaviour, IVRPointerTarget
{
    public bool IsPointerActive => true;
    public bool BlocksTriggerFallback => false;
    public void OnPointerTriggerFallback() { }
}
