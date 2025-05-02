using System;
using TMPro;
using UnityEngine;

public class ChatBubble : MonoBehaviour
{
    public static void Create(Transform parent, Vector3 localPosition, IconType iconType, String text)
    {
        Transform chatBubbleTransform = Instantiate(GameAssets.i.pfChatBubble, parent);
        chatBubbleTransform.localPosition = localPosition;

        chatBubbleTransform.GetComponent<ChatBubble>().Setup(iconType, text);

        Destroy(chatBubbleTransform.gameObject, 4f);
    }

    public enum IconType {
        Happy,
        Neutral,
        Angry
    }

    [SerializeField] private Sprite angryIcon;
    [SerializeField] private Sprite happyIcon;
    [SerializeField] private Sprite neutralIcon;

    private SpriteRenderer backgroundSpriteRenderer;
    private SpriteRenderer iconSpriteRenderer;
    private TextMeshPro textMeshPro;

    private void Awake()
    {
        backgroundSpriteRenderer = transform.Find("Background").GetComponent<SpriteRenderer>();
        iconSpriteRenderer = transform.Find("Icon").GetComponent<SpriteRenderer>();
        textMeshPro = transform.Find("Text").GetComponent<TextMeshPro>();
    }

    private void Start()
    {
        Setup(IconType.Neutral, "Hello, this is a chat bubble! AAAAAAAAAA, el gobierno chino nos espia cuando menos lo esperamos");
    }

    private void Setup(IconType iconType, String text)
    {
        textMeshPro.SetText(text);
        textMeshPro.ForceMeshUpdate();
        Vector3 textSize = textMeshPro.GetRenderedValues(false);

        Vector3 padding = new Vector3(9f, 3f, 0f);
        backgroundSpriteRenderer.size = textSize + padding;

        Vector3 offset = new Vector3(-14.5f, 0f, 0f);
        backgroundSpriteRenderer.transform.localPosition = new Vector3(backgroundSpriteRenderer.size.x / 2f, 0f, 0f) + offset;

        iconSpriteRenderer.sprite = GetIcon(iconType);
    }

    private Sprite GetIcon(IconType iconType)
    {
        switch (iconType)
        {
            case IconType.Happy:
            {
                textMeshPro.colorGradient = new VertexGradient(
                    Color.green,
                    Color.green,
                    Color.white,
                    Color.white
                );
                return happyIcon;
            }
            case IconType.Neutral:
            {
                textMeshPro.colorGradient = new VertexGradient(
                    Color.gray,
                    Color.gray,
                    Color.white,
                    Color.white
                );
                return neutralIcon;
            }
            case IconType.Angry:
            {
                textMeshPro.colorGradient = new VertexGradient(
                    Color.red,
                    Color.red,
                    Color.white,
                    Color.white
                );
                return angryIcon;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(iconType), iconType, null);
        }
    }
}
