using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    [Header("Inventory Keybinds")]
    [SerializeField] private KeyCode inventoryKey = KeyCode.Tab; // Key to press for interaction
    [SerializeField] private int spyCoins = 0; // Number of spy coins in the inventory

    [Header("Inventory Gadgets and Collectibles")]
    // !!!!! SHOULD BE LIST OF GADGETS AND NOT GAMEOBJETS !!!!!
    // !!!!!!!!!!!!!!!!!!!!!
    [SerializeField] private List<GameObject> gadgets = new List<GameObject>(); // List of gadgets
    [SerializeField] private List<GameObject> collectibles = new List<GameObject>(); // List of collectibles

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void ShowInventoryMenu()
    {
        // Show the inventory UI
    }

    public void AddSpyCoins(int amount)
    {
        spyCoins += amount;
        Debug.Log($"Added {amount} spy coins. Total now: {spyCoins}");
    }
}
