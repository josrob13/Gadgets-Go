using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DialogueUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI textComp;
    [HideInInspector] public bool NextPressed { get; set; } = false;

    public void Show() => panel.SetActive(true);
    public void Hide() => panel.SetActive(false);

    public void OnNextButton() => NextPressed = true;

    public void SetText(string txt) => textComp.text = txt;

    public TextMeshProUGUI GetTextComponent() => textComp;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("DialogueUI: OnPointerClick called");
        NextPressed = true;
    }
}
