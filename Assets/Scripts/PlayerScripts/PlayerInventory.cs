using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    /// <summary>Se dispara cada vez que cambian las SpyCoins; entrega el total actual.</summary>
    public event System.Action<int> OnSpyCoinsChanged;

    // PlayerInventory solo está colocado en la escena legacy RealGame; las escenas de
    // mundo no lo incluyen. Esto garantiza una instancia persistente desde el arranque
    // para que AddSpyCoins funcione y el CoinHUD tenga datos reales.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null) return;
        if (FindObjectOfType<PlayerInventory>() != null) return;
        new GameObject(nameof(PlayerInventory)).AddComponent<PlayerInventory>();
    }

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
        OnSpyCoinsChanged?.Invoke(spyCoins);
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

        OnSpyCoinsChanged?.Invoke(spyCoins);
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
