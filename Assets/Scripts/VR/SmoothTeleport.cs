using System.Collections;
using UnityEngine;
using Oculus.Interaction.Locomotion;

public class SmoothTeleport : MonoBehaviour, ILocomotionEventHandler
{
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private LocomotionEventsConnection teleportConnection;
    [SerializeField] private FirstPersonLocomotor firstPersonLocomotor;

    private bool _isMoving = false;

    public event System.Action<LocomotionEvent, Pose> WhenLocomotionEventHandled = delegate { };

    private void OnEnable()
    {
        if (teleportConnection != null)
        {
            teleportConnection.WhenLocomotionPerformed += OnLocomotionPerformed;
            Debug.Log("[SmoothTeleport] Suscrito a WhenLocomotionPerformed");
        }
        else
        {
            Debug.LogError("[SmoothTeleport] teleportConnection es NULL");
        }
    }

    private void OnDisable()
    {
        if (teleportConnection != null)
            teleportConnection.WhenLocomotionPerformed -= OnLocomotionPerformed;
    }

    private void OnLocomotionPerformed(LocomotionEvent locomotionEvent)
    {
        Debug.Log("[SmoothTeleport] OnLocomotionPerformed called. Translation type: " + locomotionEvent.Translation);

        if (locomotionEvent.Translation == LocomotionEvent.TranslationType.Absolute)
        {
            Debug.Log("[SmoothTeleport] Starting smooth move to: " + locomotionEvent.Pose.position);
            if (_isMoving) StopAllCoroutines();
            StartCoroutine(SmoothMove(locomotionEvent.Pose.position));
        }
        else
        {
            firstPersonLocomotor.HandleLocomotionEvent(locomotionEvent);
        }
    }

    private IEnumerator SmoothMove(Vector3 targetFeetPosition)
    {
        _isMoving = true;
        firstPersonLocomotor.enabled = false;

        Transform playerOrigin = firstPersonLocomotor.GetComponentInParent<OVRCameraRig>().transform;
        Vector3 startPosition = playerOrigin.position;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = movementCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            playerOrigin.position = Vector3.Lerp(startPosition, targetFeetPosition, t);
            yield return null;
        }

        playerOrigin.position = targetFeetPosition;
        firstPersonLocomotor.enabled = true;
        _isMoving = false;
    }

    public void HandleLocomotionEvent(LocomotionEvent locomotionEvent)
    {
        OnLocomotionPerformed(locomotionEvent);
    }
}