using UnityEngine;

public class InteractiveMenu : MonoBehaviour
{
    [SerializeField] private GameMap gameMap;
    [SerializeField] private PlayerInventory playerInventory;
    
    // GUESS SINCE THE MENU IS ACTIVATED IT WILL BE CONTROLLED BY KEYBOARD INPUTS
    // UNTIL IT IS CLOSED !!!!!!!!!!!!!

    public void ShowInteractiveMenu()
    {
        ActivateInteractiveMenu();
        ShowFirstScreen();
    }

    private void CloseInteractiveMenu()
    {
        // Deactivate the interactive menu UI
        // This could be a UI panel or a specific game object that represents the interactive menu
        Debug.Log("Closing interactive menu.");
    }

    private void ActivateInteractiveMenu()
    {
        // Activate the interactive menu UI
        // This could be a UI panel or a specific game object that represents the interactive menu
        Debug.Log("Activating interactive menu.");
    }

    private void ShowFirstScreen()
    {
        // Show the first screen of the interactive menu
        // This could be a UI panel or a specific game object that represents the first screen
        Debug.Log("Showing first screen of the interactive menu.");
    }
}
