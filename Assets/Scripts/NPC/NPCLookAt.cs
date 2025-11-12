using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class NPCLookAt : MonoBehaviour
{
    [SerializeField] private Transform target; // The target to look at
    [SerializeField] private Rig rig;
    [SerializeField] private float maxHorizontalAngle = 90f;
    [SerializeField] private Transform npcRootTransform;
    private bool isLookingAtTarget = false;
    private bool isOnCooldown = false;

    private void Update()
    {
        float targetWeight = isLookingAtTarget ? 1f : 0f;
        float lerpSpeed = 2f; // Speed of transition
        rig.weight = Mathf.Lerp(rig.weight, targetWeight, Time.deltaTime * lerpSpeed);
    }

    public void LookAtTarget(Vector3 lookAtPosition)
    {
        if (isOnCooldown)
            return;

        isLookingAtTarget = true;

        // We get the root position of the NPC and its forward
        Vector3 npcRootPosition = npcRootTransform.position;
        Vector3 npcForward = npcRootTransform.forward;

        // We flat the positions for a 2D comparision
        Vector3 lookAtHorizontal = new Vector3(lookAtPosition.x, npcRootPosition.y, lookAtPosition.z);

        // Calculate horizontal direction from NPC to target
        Vector3 directionToTarget = (lookAtHorizontal - npcRootPosition).normalized;

        // Calculate the angle between NPC forward and target
        float angle = Vector3.SignedAngle(npcForward, directionToTarget, Vector3.up);

        Vector3 finalTargetPosition;

        if (Mathf.Abs(angle) > maxHorizontalAngle)
        {
            // If exceeds, calculate new limited direction
            float clampedAngle = Mathf.Clamp(angle, -maxHorizontalAngle, maxHorizontalAngle);
            Quaternion clampedRotation = Quaternion.AngleAxis(clampedAngle, Vector3.up);
            Vector3 clampedDirection = clampedRotation * npcForward;

            // Place the target in that direction but keeping original height
            float originalDistance = Vector3.Distance(npcRootPosition, lookAtHorizontal);
            Vector3 clampedTargetOnPlane = npcRootPosition + clampedDirection * originalDistance;
            
            finalTargetPosition = new Vector3(clampedTargetOnPlane.x, lookAtPosition.y, clampedTargetOnPlane.z);
        }
        else
        {
            finalTargetPosition = lookAtPosition;
        }

        target.position = finalTargetPosition;
        StartCoroutine(NormalLooking());
    }

    private IEnumerator NormalLooking()
    {
        yield return new WaitForSeconds(2.5f);
        isLookingAtTarget = false;
    }
}
