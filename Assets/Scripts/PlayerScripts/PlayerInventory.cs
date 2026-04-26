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
            LoadInventoryData();
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
        SaveInventoryData();
    }

    public int GetSpyCoins()
    {
        return spyCoins;
    }

    private void LoadInventoryData()
    {
        GameData savedData = SaveSystem.Load();
        if (savedData != null && savedData.inventory != null)
        {
            spyCoins = savedData.inventory.spyCoins;
            Debug.Log($"[PlayerInventory] Loaded {spyCoins} spy coins from save.");
        }
        else
        {
            spyCoins = 0;
            Debug.Log("[PlayerInventory] No saved inventory data found, starting with 0 spy coins.");
        }
    }

    private void SaveInventoryData()
    {
        GameData data = SaveSystem.Load() ?? new GameData();
        if (data.inventory == null)
        {
            data.inventory = new InventoryData();
        }
        data.inventory.spyCoins = spyCoins;
        SaveSystem.Save(data);
        Debug.Log($"[PlayerInventory] Saved {spyCoins} spy coins.");
    }
}
