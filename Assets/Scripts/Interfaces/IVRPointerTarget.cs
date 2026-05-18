/// <summary>
/// Implemented by any UI panel that wants to participate in VR pointer interaction.
/// VRRayPointer queries all IVRPointerTarget instances in the scene each frame.
/// No hardcoded panel references needed — just implement this interface.
/// </summary>
public interface IVRPointerTarget
{
    bool IsPointerActive { get; }
    bool BlocksTriggerFallback { get; }
    void OnPointerTriggerFallback();
}
