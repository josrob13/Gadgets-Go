using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a World Space canvas. Adds Box Colliders to every child Button
/// that is missing one, so VRRayPointer (Physics.Raycast) can detect them.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UIButtonColliderSync : MonoBehaviour
{
    void Start()
    {
        int added = 0;
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponent<BoxCollider>() != null) continue;

            var rt  = button.GetComponent<RectTransform>();
            var col = button.gameObject.AddComponent<BoxCollider>();
            col.size   = new Vector3(rt.rect.width, rt.rect.height, 1f);
            col.center = Vector3.zero;
            added++;
        }

        if (added > 0)
            Debug.Log($"[UIButtonColliderSync] Added Box Colliders to {added} button(s) on '{gameObject.name}'.");
    }
}
