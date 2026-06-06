using UnityEngine;
using System.Collections.Generic;

public class CheatConsole : MonoBehaviour
{
    public static CheatConsole Instance;

    [Header("Item Database (Kéo tất cả các file SO vật phẩm vào đây)")]
    public List<BaseItemSO> itemDatabase = new List<BaseItemSO>();

    private bool showConsole = false;
    private string input = "";

    // Public variable for other scripts to check if the console is active
    public bool IsConsoleOpen => showConsole;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // Phím ~ hoặc ` để bật/tắt (BackQuote), plus inputString fallback for keyboard layouts
        if (IsToggleKeyPressed())
        {
            showConsole = !showConsole;
            if (showConsole) input = ""; // Reset khi mở lại
        }
    }

    private bool IsToggleKeyPressed()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote)) return true;
        if (Input.GetKeyDown(KeyCode.F1)) return true; // Hỗ trợ F1 làm phím mở cheat console dự phòng cực kỳ an toàn

        string typed = Input.inputString;
        if (!string.IsNullOrEmpty(typed) && (typed.Contains("`") || typed.Contains("~")))
        {
            return true;
        }

        return false;
    }

    private void OnGUI()
    {
        if (!showConsole) return;

        // Bắt sự kiện nhấn Enter TRƯỚC khi vẽ TextField (TextField hay nuốt mất event)
        Event e = Event.current;
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
        {
            ProcessCommand(input);
            showConsole = false;
            input = ""; // Clear sau khi nhập xong
            GUI.FocusControl(null); // Bỏ focus
            return; // Thoát ngay GUI frame này
        }

        // Vẽ một hộp thoại đơn giản phía dưới màn hình
        GUI.Box(new Rect(0, Screen.height - 50, Screen.width, 50), "");
        
        // Đặt tên cho ô input để tự động focus
        GUI.SetNextControlName("CheatInput");
        input = GUI.TextField(new Rect(10, Screen.height - 40, Screen.width - 20, 30), input);
        
        // Format lại nếu lỡ dính dấu ~ hoặc ` khi vừa bật console
        input = input.Replace("`", "").Replace("~", "");

        // Tự động focus vào ô này để gõ được liền
        GUI.FocusControl("CheatInput");
    }

    private void ProcessCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return;

        // Cắt theo dấu cách (space)
        string[] args = commandLine.Trim().Split(' ');
        string command = args[0].ToLower();

        switch (command)
        {
            case "give":
                if (args.Length >= 2)
                {
                    string itemId = args[1];
                    int amount = 1; // Mặc định là 1 nếu không chỉ định số lượng
                    
                    if (args.Length >= 3)
                    {
                        int.TryParse(args[2], out amount);
                    }
                    
                    GiveItem(itemId, amount);
                }
                else
                {
                    Debug.Log("<color=red>Lỗi cú pháp! Cách dùng: give [id_hoặc_tên] [số_lượng]</color>");
                }
                break;

            // TODO: Bạn có thể thêm nhiều case ở đây sau này như "addmoney", "setrating", v.v.
            case "money":
            case "addmoney":
                if (args.Length >= 2 && int.TryParse(args[1], out int moneyAmount))
                {
                    PlayerData.AddCredit(moneyAmount);
                    Debug.Log($"<color=green>Cheat: Đã thêm {moneyAmount} RC!</color>");
                }
                else
                {
                    Debug.Log("<color=red>Lỗi cú pháp! Cách dùng: money [số_tiền]</color>");
                }
                break;

            case "time":
            case "addtime":
                if (args.Length >= 2 && int.TryParse(args[1], out int hoursToAdd))
                {
                    if (HUDManager.Instance != null)
                    {
                        int newHour = HUDManager.Instance.CurrentHour + hoursToAdd;
                        int newDay = HUDManager.Instance.CurrentDay;
                        while (newHour >= 24)
                        {
                            newHour -= 24;
                            newDay += 1;
                        }
                        HUDManager.Instance.ApplyTimeState(newDay, newHour, HUDManager.Instance.CurrentMinute);
                        Debug.Log($"<color=green>Cheat: Đã tua thêm {hoursToAdd} giờ!</color>");
                    }
                    else
                    {
                        Debug.Log("<color=red>Lỗi: Không tìm thấy HUDManager.Instance!</color>");
                    }
                }
                else
                {
                    Debug.Log("<color=red>Lỗi cú pháp! Cách dùng: time [số_giờ]</color>");
                }
                break;

            case "skipday":
            case "nextday":
                if (HUDManager.Instance != null)
                {
                    HUDManager.Instance.SkipToNextDayMorning();
                    Debug.Log("<color=green>Cheat: Đã bỏ qua ngày và bắt đầu sáng hôm sau!</color>");
                }
                else
                {
                    Debug.Log("<color=red>Lỗi: Không tìm thấy HUDManager.Instance!</color>");
                }
                break;

            case "hygiene":
            case "sethygiene":
                if (args.Length >= 2 && float.TryParse(args[1], out float hygieneVal))
                {
                    PlayerData.hygieneScore = Mathf.Clamp(hygieneVal, 0f, 10f);
                    if (RestaurantRatingManager.Instance != null)
                    {
                        RestaurantRatingManager.Instance.RefreshRating();
                    }
                    Debug.Log($"<color=green>Cheat: Đã đặt điểm vệ sinh thành {PlayerData.hygieneScore}/10!</color>");
                }
                else
                {
                    Debug.Log("<color=red>Lỗi cú pháp! Cách dùng: hygiene [0..10]</color>");
                }
                break;

            case "speed":
            case "setspeed":
                if (args.Length >= 2 && float.TryParse(args[1], out float speedVal))
                {
                    PlayerMovement pm = FindObjectOfType<PlayerMovement>();
                    if (pm != null)
                    {
                        pm.MoveSpeed = speedVal;
                        Debug.Log($"<color=green>Cheat: Đã đặt tốc độ di chuyển người chơi thành {pm.MoveSpeed}!</color>");
                    }
                    else
                    {
                        Debug.Log("<color=red>Lỗi: Không tìm thấy người chơi (PlayerMovement) trong cảnh hiện tại!</color>");
                    }
                }
                else
                {
                    Debug.Log("<color=red>Lỗi cú pháp! Cách dùng: speed [tốc_độ]</color>");
                }
                break;

            case "food":
            case "setfood":
                if (args.Length >= 2 && float.TryParse(args[1], out float foodVal))
                {
                    PlayerData.foodQualityScore = Mathf.Clamp(foodVal, 0f, 10f);
                    if (RestaurantRatingManager.Instance != null)
                    {
                        RestaurantRatingManager.Instance.RefreshRating();
                    }
                    Debug.Log($"<color=green>Cheat: Đã đặt điểm món ăn thành {PlayerData.foodQualityScore}/10!</color>");
                }
                else
                {
                    Debug.Log("<color=red>Lỗi cú pháp! Cách dùng: food [0..10]</color>");
                }
                break;

            case "decor":
            case "setdecor":
                if (args.Length >= 2 && float.TryParse(args[1], out float decorVal))
                {
                    PlayerData.decorationScore = Mathf.Clamp(decorVal, 0f, 10f);
                    if (UpgradeManager.Instance != null)
                    {
                        if (decorVal < 5f)
                            UpgradeManager.Instance.highestUnlockedLevel = 1;
                        else if (decorVal < 8f)
                            UpgradeManager.Instance.highestUnlockedLevel = 2;
                        else
                            UpgradeManager.Instance.highestUnlockedLevel = 3;
                    }
                    if (RestaurantRatingManager.Instance != null)
                    {
                        RestaurantRatingManager.Instance.RefreshRating();
                    }
                    Debug.Log($"<color=green>Cheat: Đã đặt điểm trang trí thành {PlayerData.decorationScore}/10 (Cấp độ: {(UpgradeManager.Instance != null ? UpgradeManager.Instance.highestUnlockedLevel.ToString() : "N/A")})!</color>");
                }
                else
                {
                    Debug.Log("<color=red>Lỗi cú pháp! Cách dùng: decor [0..10]</color>");
                }
                break;

            case "service":
            case "setservice":
            case "attitude":
            case "setattitude":
                if (args.Length >= 2 && float.TryParse(args[1], out float serviceVal))
                {
                    float clampedVal = Mathf.Clamp(serviceVal, 0f, 10f);
                    PlayerData.satisfactionHistory.Clear();
                    for (int i = 0; i < 5; i++)
                    {
                        PlayerData.satisfactionHistory.Enqueue(clampedVal);
                    }
                    if (RestaurantRatingManager.Instance != null)
                    {
                        RestaurantRatingManager.Instance.RefreshRating();
                    }
                    Debug.Log($"<color=green>Cheat: Đã đặt điểm thái độ phục vụ thành {clampedVal}/10!</color>");
                }
                else
                {
                    Debug.Log("<color=red>Lỗi cú pháp! Cách dùng: service [0..10]</color>");
                }
                break;

            default:
                Debug.Log($"<color=red>Lệnh không tồn tại: {command}</color>");
                break;
        }
    }

    private void GiveItem(string itemId, int amount)
    {
        BaseItemSO foundItem = null;
        if (itemDatabase != null)
        {
            foundItem = itemDatabase.Find(x => 
                x != null && (x.id.ToLower() == itemId.ToLower() || 
                x.name.ToLower() == itemId.ToLower() ||
                x.itemName.ToLower() == itemId.ToLower()));
        }

        if (foundItem == null && ShopManager.Instance != null && ShopManager.Instance.allItems != null)
        {
            foundItem = ShopManager.Instance.allItems.Find(x => 
                x != null && (x.id.ToLower() == itemId.ToLower() || 
                x.name.ToLower() == itemId.ToLower() ||
                x.itemName.ToLower() == itemId.ToLower()));
        }

        if (foundItem != null)
        {
            if (PlayerInventory.Instance == null)
            {
                Debug.Log("<color=red>Lỗi: Không tìm thấy PlayerInventory.Instance trong scene!</color>");
                return;
            }

            // Gọi lặp AddItem để nó trigger logic Hotbar và Inventory an toàn
            for (int i = 0; i < amount; i++)
            {
                StoredItem newItem = new StoredItem(foundItem, 1);
                newItem.currentFreshness = 100f; // Mặc định cheat ra là tươi nhất (100)
                PlayerInventory.Instance.AddItem(newItem);
            }
            
            Debug.Log($"<color=green>Cheat: Đã thêm {amount}x {foundItem.itemName} vào túi!</color>");
        }
        else
        {
            Debug.Log($"<color=red>Lỗi: Không có vật phẩm nào tên là '{itemId}' nằm trong danh sách ItemDatabase của CheatConsole.</color>");
        }
    }
}