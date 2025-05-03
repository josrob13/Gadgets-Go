using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextWriter : MonoBehaviour
{
    private static TextWriter instance;
    private List<TextWriterSingle> textWriterSingleList;

    private void Awake()
    {
        instance = this;
        textWriterSingleList = new List<TextWriterSingle>();
    }

    public static void AddWriter_Static(TextMeshPro uiText, string textToWrite, float timePerCharacter, bool invisibleCharacters, bool removeWriterBeforeAdd)
    {
        if (removeWriterBeforeAdd)
            instance.RemoveWriter(uiText);
        instance.AddWriter(uiText, textToWrite, timePerCharacter, invisibleCharacters);
    }

    private void AddWriter(TextMeshPro uiText, string textToWrite, float timePerCharacter, bool invisibleCharacters)
    {
        TextWriterSingle textWriterSingle = new TextWriterSingle(uiText, textToWrite, timePerCharacter, invisibleCharacters);
        textWriterSingleList.Add(textWriterSingle);
    }

    public static void RemoveWriter_Static(TextMeshPro uiText)
    {
        instance.RemoveWriter(uiText);
    }

    public static TextWriter Instance()
    {
        return instance;
    }

    public bool isTextActive(TextMeshPro uiText)
    {
        for (int i = 0; i < textWriterSingleList.Count; i++)
        {
            if (textWriterSingleList[i].GetTextMesh() == uiText)
            {
                return textWriterSingleList[i].IsActive();
            }
        }
        return false;
    }

    private void RemoveWriter(TextMeshPro uiText)
    {
        for (int i = 0; i < textWriterSingleList.Count; i++)
        {
            if (textWriterSingleList[i].GetTextMesh() == uiText)
            {
                textWriterSingleList.RemoveAt(i);
                i--;
            }
        }
    }

    private void Update()
    {
        for (int i = 0; i < textWriterSingleList.Count; i++)
        {
            bool destroyInstance = textWriterSingleList[i].Update();
            if (destroyInstance)
            {
                textWriterSingleList.RemoveAt(i);
                i--;
            }
        }
    }

    /*
     * A single TextWriter instance
     * */
    public class TextWriterSingle
    {
        private TextMeshPro textMesh;
        private string textToWrite;
        private int characterIndex;
        private float timePerCharacter;
        private float timer;
        private bool invisibleCharacters;

        public TextWriterSingle(TextMeshPro uiText, string textToWrite, float timePerCharacter, bool invisibleCharacters)
        {
            this.textMesh = uiText;
            this.textToWrite = textToWrite;
            this.timePerCharacter = timePerCharacter;
            this.invisibleCharacters = invisibleCharacters;
            timer = 0f;
        }

        // Return true on complete
        public bool Update()
        {
            timer -= Time.deltaTime;
            // While makes the Update function 'print' more than one character per frame
            // Therefore, the timer will be reduced until it is less than 0f in every frame
            // If it was an if statement, it would only print one character per frame
            // The limit is always the frames
            while (timer <= 0f)
            {
                timer += timePerCharacter;
                characterIndex++;
                string text = textToWrite.Substring(0, characterIndex);
                if (invisibleCharacters)
                {
                    text += "<color=#00000000>" + textToWrite.Substring(characterIndex) + "</color>";
                }
                textMesh.text = text;
                textMesh.text = textToWrite.Substring(0, characterIndex);

                if (characterIndex >= textToWrite.Length)
                    return true;
            }

            return false;
        }

        public TextMeshPro GetTextMesh()
        {
            return textMesh;
        }

        public bool IsActive()
        {
            return characterIndex < textToWrite.Length;
        }

        public void WriteAllDestroy()
        {
            textMesh.text = textToWrite;
            characterIndex = textToWrite.Length;
            timer = 0f;
            RemoveWriter_Static(textMesh);
        }
    }
}
