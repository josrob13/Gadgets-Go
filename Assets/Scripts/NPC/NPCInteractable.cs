using System.Collections;
using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    private NPCLookAt npcLookAt;
    private bool isOnCooldown = false;

    private void Awake()
    {
        npcLookAt = GetComponent<NPCLookAt>();
    }

    public void Interact(Transform playerTransform)
    {
        if (isOnCooldown)
        {
            Debug.Log("Interaction is on cooldown.");
            return;
        }

        Debug.Log("Interacting with NPC: ");
        ChatBubble.Create(transform.transform, new Vector3(-0.3f, 1.7f, 0f), ChatBubble.IconType.Happy, "Hello! How can I help you today?");

        float playerHeight = 1.7f;
        npcLookAt.LookAtTarget(playerTransform.position + Vector3.up * playerHeight);

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(5f); // Cooldown duration
        isOnCooldown = false;
    }
}
