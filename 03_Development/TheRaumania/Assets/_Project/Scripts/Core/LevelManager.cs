using UnityEngine;
public class LevelManager : MonoBehaviour 
{
    public static LevelManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
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