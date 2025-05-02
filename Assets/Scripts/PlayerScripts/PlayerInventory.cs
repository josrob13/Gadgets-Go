using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Keybinds")]
    [SerializeField] private KeyCode inventoryKey = KeyCode.Tab; // Key to press for interaction
    private int spyCoins = 0; // Number of spy coins in the inventory

    [Header("Inventory Gadgets and Collectibles")]
    // !!!!! SHOULD BE LIST OF GADGETS AND NOT GAMEOBJETS !!!!!
    // !!!!!!!!!!!!!!!!!!!!!
    [SerializeField] private List<GameObject> gadgets = new List<GameObject>(); // List of gadgets
    [SerializeField] private List<GameObject> collectibles = new List<GameObject>(); // List of collectibles

    void ShowInventoryMenu()
    {
        // Show the inventory UI
    }
}
