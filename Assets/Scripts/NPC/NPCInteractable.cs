using System.Collections;
using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactText;

    private NPCLookAt npcLookAt;
    private bool isOnCooldown = false;

    private void Awake()
    {
        npcLookAt = GetComponent<NPCLookAt>();
    }

    public void Interact(Transform playerTransform)
    {
        if (gameObject.CompareTag("MainNPC"))
        {
            // Do the MainNPC interaction
        }
        else if (gameObject.CompareTag("BasicNPC"))
            BasicNPCInteract(playerTransform);
    }

    private void BasicNPCInteract(Transform playerTransform)
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
    
    public string GetInteractText()
    {
        return interactText;
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
