using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class NPCLookAt : MonoBehaviour
{
    [SerializeField] private Transform target; // The target to look at
    [SerializeField] private Rig rig;

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
        target.position = lookAtPosition;
        StartCoroutine(NormalLooking());
    }

    private IEnumerator NormalLooking()
    {
        yield return new WaitForSeconds(2.5f);
        isLookingAtTarget = false;
    }
}
