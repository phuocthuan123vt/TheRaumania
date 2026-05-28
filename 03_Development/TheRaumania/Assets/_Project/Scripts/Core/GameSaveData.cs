using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public string saveFileName; 
    public int rCredit;

    // Time system
    public int dayCount = 1;
    public int hourOfDay = 5;
    public int minuteOfHour = 0;
    public bool hasTimeState;

    // Player position and scene
    public string sceneName;
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    public bool hasPlayerPosition;

    // Đánh giá nhà hàng
    public float foodQualityScore = 5f; // Mặc định 5/10
    public float hygieneScore = 10f;    // Mặc định sạch sẽ 10/10
    public float decorationScore = 0f;  // Mặc định chưa trang trí 0/10
    public List<float> satisfactionHistory = new List<float>(); // Lưu queue 50 khách

    // Warehouse and hotbar
    public List<StoredItemData> coldStorage = new List<StoredItemData>();
    public List<StoredItemData> dryStorage = new List<StoredItemData>();
    public List<StoredItemData> hotbarItems = new List<StoredItemData>();

    // Upgrade system: highest restaurant level the player has unlocked (1..3)
    public int highestUnlockedRestaurantLevel = 1;
    // Current prices for upgrades (index = level target, e.g., index 2 is price to reach level 2)
    public List<int> upgradeCurrentPrices = new List<int>();
    // Whether bargaining is still allowed for that target level (index aligned with upgradeCurrentPrices)
    public List<bool> upgradeBargainAllowed = new List<bool>();

    public GameSaveData(string fileName, int money)
    {
        this.saveFileName = fileName;
        this.rCredit = money;
        this.dayCount = 1;
        this.hourOfDay = 5;
        this.minuteOfHour = 0;
        this.hasTimeState = true;
    }
}

[Serializable]
public class StoredItemData
{
    public string itemId;
    public int quantity;
    public float freshness;

    public StoredItemData(string id, int qty, float fresh)
    {
        itemId = id;
        quantity = qty;
        freshness = fresh;
    }
}