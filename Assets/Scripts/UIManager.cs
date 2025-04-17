using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject pauseMenuUI;

    public void GameResume()
    {
        pauseMenuUI.SetActive(false);
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

    public void GamePause()
    {
        pauseMenuUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }
}
