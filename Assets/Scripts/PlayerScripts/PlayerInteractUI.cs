using UnityEditor.Search;
using UnityEngine;
using TMPro;

public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private TextMeshProUGUI interactionTextMeshProGUI;

    private void Update()
    {
        IInteractable i = playerInteraction.GetInteractableObject();
        if (i != null)
            Show(i);
        else
            Hide();
    }

    private void Show(IInteractable interactable)
    {
        interactionTextMeshProGUI.text = interactable.GetInteractText();
        container.SetActive(true);
    }
    
    private void Hide()
    {
        container.SetActive(false);
    }
}
