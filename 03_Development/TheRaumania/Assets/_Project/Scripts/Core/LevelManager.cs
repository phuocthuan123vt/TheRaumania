using UnityEngine;
public class LevelManager : MonoBehaviour 
{
    public static LevelManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    public void OnPlayerInteractWithCustomer(CustomerAI customer)
    {
        switch (customer.currentState)
        {
            case CustomerAI.CustomerState.Queueing:
                HandleLeadToTable(customer);
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
    private void HandleLeadToTable(CustomerAI customer)
    {
        Table[] allTables = FindObjectsOfType<Table>();

        foreach (var t in allTables)
        {
            for (int i = 0; i < t.seats.Length; i++)
            {
                if (!t.seats[i].isOccupied)
                {
                    t.seats[i].isOccupied = true;
                    customer.LeadToSeat(t, i);
                    return;
                }
            }
        }
        Debug.Log("Không còn cái ghế nào trống cả!");
    }
}