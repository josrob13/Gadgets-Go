using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    public event System.Action<int> OnSpyCoinsChanged;

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
        OnSpyCoinsChanged?.Invoke(spyCoins);
    }

    public int GetSpyCoins()
    {
        return spyCoins;
    }

    public InventoryData Export() => new InventoryData { spyCoins = spyCoins };

    public void Import(InventoryData data)
    {
        spyCoins = data?.spyCoins ?? 0;
        OnSpyCoinsChanged?.Invoke(spyCoins);
    }
}
