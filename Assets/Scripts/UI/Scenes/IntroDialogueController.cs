using System.Collections;
using UnityEngine;

public class IntroDialogueController : MonoBehaviour
{
    [SerializeField] private DialogueNode introDialogue;
    [SerializeField] private string dialogueId = "intro";
    [Tooltip("Assign the dialogue Canvas transform here to lock its scene position/rotation " +
             "instead of letting DialogueCanvasController auto-calculate it at runtime.")]
    [SerializeField] private Transform canvasOverrideTransform;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => DialogueManager.Instance != null);

        Vector3? pos = null;
        Quaternion? rot = null;
        if (canvasOverrideTransform != null)
        {
            pos = canvasOverrideTransform.position;
            rot = canvasOverrideTransform.rotation;
        }

        yield return DialogueManager.Instance.StartDialogue(introDialogue, dialogueId, pos, rot);

        GameHandler.Instance?.LoadCurrentWorld();
    }
}
