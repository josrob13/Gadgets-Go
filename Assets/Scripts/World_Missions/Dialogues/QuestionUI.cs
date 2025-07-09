using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI questionComp;
    [SerializeField] private Button[] optionButtons;

    public bool HasAnswered { get; private set; }
    public int SelectedIndex { get; private set; }

    private void Awake()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int idx = i;
            optionButtons[i].onClick.AddListener(() => OnOptionClicked(idx));
        }
    }

    public void ShowQuestion(string question, string[] options)
    {
        panel.SetActive(true);
        questionComp.text = question;
        HasAnswered = false;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(i < options.Length);
            if (i < options.Length)
                optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = options[i];
        }
    }

    private void OnOptionClicked(int idx)
    {
        SelectedIndex = idx;
        HasAnswered   = true;
    }

    public void Hide() => panel.SetActive(false);
}
