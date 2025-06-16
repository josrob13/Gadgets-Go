using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject normalUI;

    public void GamePause()
    {
        pauseMenuUI.SetActive(true);
        normalUI.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void GameResume()
    {
        pauseMenuUI.SetActive(false);
        normalUI.SetActive(true);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GameQuit()
    {
        Debug.Log("Game is quitting...");
        UnityEditor.EditorApplication.isPlaying = false; // Detiene el modo Play en el editor
        Application.Quit();
    }
}
