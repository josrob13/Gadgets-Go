using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveData
{
    public int saveVersion;
    public string sessionId = "";
    public string playerName = "";
    public int savedWorldIndex = 0;
    public List<string> completedMissions = new();
    public InventoryData inventory = new();
}

[Serializable]
public class InventoryData
{
    public int spyCoins = 0;
    public List<string> itemIDs = new();
}
