using UnityEngine;
public class LevelManager : MonoBehaviour 
{
    public static LevelManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        StartCoroutine(RatCheckRoutine());
    }

    private System.Collections.IEnumerator RatCheckRoutine()
    {
        // Chờ một chút lúc đầu game
        yield return new WaitForSeconds(5f);

        while (true)
        {
            yield return new WaitForSeconds(20f);

            // Chỉ chạy sự kiện ngẫu nhiên khi nhà hàng đang mở cửa
            if (HUDManager.Instance != null && HUDManager.Instance.IsRestaurantOpen)
            {
                float hygiene = PlayerData.hygieneScore;
                // Nội suy tuyến tính: Điểm vệ sinh = 10 -> 1% cơ hội, Điểm vệ sinh = 0 -> 35% cơ hội
                float ratChance = Mathf.Lerp(0.35f, 0.01f, hygiene / 10f);

                if (Random.value < ratChance)
                {
                    TriggerEventRat();
                }
            }
        }
    }

    private TMPro.TMP_FontAsset GetFallbackFont()
    {
        if (TMPro.TMP_Settings.defaultFontAsset != null) return TMPro.TMP_Settings.defaultFontAsset;
        TMPro.TextMeshProUGUI anyText = FindObjectOfType<TMPro.TextMeshProUGUI>(true);
        if (anyText != null && anyText.font != null) return anyText.font;
        return null;
    }

    public void ShowScreenWarning(string message, Color color)
    {
        Canvas canvas = null;
        if (HUDManager.Instance != null)
        {
            Canvas[] hudCanvases = HUDManager.Instance.transform.root.GetComponentsInChildren<Canvas>(true);
            foreach (var c in hudCanvases)
            {
                if (c.renderMode != RenderMode.WorldSpace)
                {
                    canvas = c;
                    break;
                }
            }
        }

        if (canvas == null)
        {
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (var c in allCanvases)
            {
                if (c.renderMode != RenderMode.WorldSpace && c.GetComponentInParent<CustomerAI>() == null)
                {
                    canvas = c;
                    break;
                }
            }
        }

        if (canvas == null)
        {
            Debug.LogError("No screen-space canvas found to show screen warning.");
            return;
        }

        GameObject warningGo = new GameObject("txt_ScreenWarning", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        warningGo.transform.SetParent(canvas.transform, false);

        RectTransform rect = warningGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 0f); // Giữa màn hình chính xác
        rect.sizeDelta = new Vector2(1000f, 200f);
        rect.localScale = Vector3.one;

        TMPro.TextMeshProUGUI tmp = warningGo.GetComponent<TMPro.TextMeshProUGUI>();
        tmp.font = GetFallbackFont();
        tmp.fontSize = 64f; // Tăng kích thước chữ gấp đôi (từ 32f lên 64f)
        tmp.color = color;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.text = message;
        tmp.raycastTarget = false;

        tmp.outlineColor = Color.black;
        tmp.outlineWidth = 0.25f;

        Destroy(warningGo, 5f); // Hiển thị khoảng 5s
    }

    public void TriggerEventKitchenFire()
    {
        ShowScreenWarning("CHÁY BẾP DO CHIÊN QUÁ LÂU! KHÁCH HÀNG HOẢNG SỢ BỎ CHẠY!", Color.red);

        CustomerAI[] allCustomers = FindObjectsOfType<CustomerAI>();
        foreach (var customer in allCustomers)
        {
            if (customer != null)
            {
                customer.TriggerPanic("FireEvent", customer.emoteAngry);
            }
        }
    }

    public void TriggerEventRat()
    {
        ShowScreenWarning("PHÁT HIỆN CHUỘT CHẠY TRONG QUÁN! KHÁCH HÀNG HOẢNG SỢ BỎ VỀ!", new Color(1f, 0.5f, 0f));

        CustomerAI[] allCustomers = FindObjectsOfType<CustomerAI>();
        foreach (var customer in allCustomers)
        {
            if (customer != null)
            {
                customer.TriggerPanic("RatEvent", customer.emoteNauseous);
            }
        }
    }

    [Header("Dẫn khách")]
    public CustomerAI currentLedCustomer;

    public void OnPlayerInteractWithCustomer(CustomerAI customer)
    {
        switch (customer.currentState)
        {
            case CustomerAI.CustomerState.Queueing:
                if (currentLedCustomer != null)
                {
                    Debug.Log("Bạn đang dẫn một khách hàng khác rồi!");
                }
                else
                {
                    currentLedCustomer = customer;
                    customer.currentState = CustomerAI.CustomerState.BeingLed;

                    Interactable customerInteractable = customer.GetComponent<Interactable>();
                    if (customerInteractable != null)
                    {
                        customerInteractable.interactMessage = "Nhấn E để hủy dẫn";
                    }

                    Debug.Log("Khách hàng bắt đầu đi theo bạn. Hãy dẫn họ đến bàn trống và nhấn E.");
                }
                break;

            case CustomerAI.CustomerState.BeingLed:
                if (currentLedCustomer == customer)
                {
                    GameObject player = GameObject.FindWithTag("Player");
                    Table nearbyTable = (player != null) ? FindNearbyTable(player.transform.position, 2.2f) : null;
                    if (nearbyTable != null)
                    {
                        OnPlayerInteractWithTable(nearbyTable);
                    }
                    else
                    {
                        currentLedCustomer = null;
                        customer.currentState = CustomerAI.CustomerState.Queueing;

                        Interactable customerInteractable = customer.GetComponent<Interactable>();
                        if (customerInteractable != null)
                        {
                            customerInteractable.interactMessage = "Nhấn E để tương tác";
                        }

                        customer.ReturnToQueue();
                        Debug.Log("Đã hủy dẫn khách hàng này.");
                    }
                }
                break;

            case CustomerAI.CustomerState.Ordering:
                customer.TakeOrder();
                Debug.Log("Alex đã nhận món khách yêu cầu!");
                break;

            case CustomerAI.CustomerState.WaitingForFood:
                int currentSlot = HotbarManager.Instance.SelectedSlotIndex;

                if (HotbarManager.Instance.items.Count > currentSlot && currentSlot >= 0)
                {
                    var itemOnHand = HotbarManager.Instance.items[currentSlot];

                    if (itemOnHand.itemData.id == customer.wantedRecipe.dishResultSO.id)
                    {
                        Debug.Log("<color=green>Đã đưa đúng món!</color>");

                        float stars = itemOnHand.currentFreshness / 20f;
                        customer.ReceiveFood(itemOnHand.itemData, stars);

                        itemOnHand.quantity--;
                        if (itemOnHand.quantity <= 0)
                        {
                            HotbarManager.Instance.items.RemoveAt(currentSlot);
                        }
                        
                        HotbarManager.Instance.RefreshUI();
                    }
                    else
                    {
                        Debug.Log("<color=red>Sai món rồi! Khách muốn: " + customer.wantedRecipe.dishName + "</color>");
                    }
                }
                else
                {
                    Debug.Log("Tay trắng không có món gì để đưa!");
                }
                break;

            case CustomerAI.CustomerState.CheckingOut:
                customer.ReceivePaymentByPlayer();
                Debug.Log("Alex đã nhận thanh toán của khách!");
                break;
        }
    }

    public void OnPlayerInteractWithTable(Table table)
    {
        if (currentLedCustomer == null)
        {
            Debug.Log("Bạn chưa dẫn khách hàng nào cả!");
            return;
        }

        // Tìm ghế trống
        int freeSeatIndex = -1;
        for (int i = 0; i < table.seats.Length; i++)
        {
            if (!table.seats[i].isOccupied)
            {
                freeSeatIndex = i;
                break;
            }
        }

        if (freeSeatIndex == -1)
        {
            Debug.Log("Bàn này đã đầy chỗ!");
            return;
        }

        // Xếp khách vào bàn
        CustomerAI customer = currentLedCustomer;
        currentLedCustomer = null;

        Interactable customerInteractable = customer.GetComponent<Interactable>();
        if (customerInteractable != null)
        {
            customerInteractable.interactMessage = "Nhấn E để tương tác";
        }

        customer.LeadToSeat(table, freeSeatIndex);
        Debug.Log($"Đã xếp khách {customer.name} vào bàn {table.name}, ghế {freeSeatIndex}.");
    }

    private Table FindNearbyTable(Vector3 position, float range)
    {
        Table[] allTables = FindObjectsOfType<Table>();
        Table nearest = null;
        float minDist = range;
        foreach (var t in allTables)
        {
            if (t == null) continue;
            float dist = Vector3.Distance(position, t.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = t;
            }
        }
        return nearest;
    }
}