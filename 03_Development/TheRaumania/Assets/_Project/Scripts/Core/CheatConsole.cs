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
        
        // Format lại nếu lỡ dính dấu ~ khi vừa bật console
        input = input.Replace("`", "");

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

            default:
                Debug.Log($"<color=red>Lệnh không tồn tại: {command}</color>");
                break;
        }
    }

    private void GiveItem(string itemId, int amount)
    {
        // Tìm vật phẩm bằng id, hoặc nếu nhập nhầm id thì tìm vớt bằng file name/itemName
        BaseItemSO foundItem = itemDatabase.Find(x => 
            x.id.ToLower() == itemId.ToLower() || 
            x.name.ToLower() == itemId.ToLower() ||
            x.itemName.ToLower() == itemId.ToLower());

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