using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Interactable))]
public class CheckoutManager : MonoBehaviour
{
    public static CheckoutManager Instance;

    [Header("Cấu hình Hàng Đợi")]
    [Tooltip("Khoảng cách và hướng giữa mỗi khách đứng chờ (x, y, z).")]
    public Vector3 queueDirection = new Vector3(-1.5f, 0, 0); // VD: xếp dần sang bên trái
    
    public List<CustomerAI> queue = new List<CustomerAI>();

    private void Awake()
    {
        Instance = this;

        // Tự động cấu hình cục Interactable
        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.interactMessage = "Nhấn E để Tính Tiền";
            interactable.onInteract.AddListener(ProcessNextCustomer);
        }
    }

    public void JoinQueue(CustomerAI customer)
    {
        if (!queue.Contains(customer))
        {
            queue.Add(customer);
        }
    }

    public void LeaveQueue(CustomerAI customer)
    {
        if (queue.Contains(customer))
        {
            queue.Remove(customer);
        }
    }

    public Vector3 GetQueuePosition(CustomerAI customer)
    {
        int index = queue.IndexOf(customer);
        if (index == -1) return transform.position; // Nếu không có trong hàng, trả về tọa độ gốc

        // Vị trí sẽ lùi lùi ra phía sau dựa theo index của khách
        return transform.position + (queueDirection * index);
    }

    public void ProcessNextCustomer()
    {
        if (queue.Count > 0)
        {
            CustomerAI firstCustomer = queue[0];
            queue.RemoveAt(0); // Nhấc khách đầu tiên ra khỏi hàng đợi
            
            // Kích hoạt flag báo hiệu đã thu tiền
            firstCustomer.ReceivePaymentByPlayer();
            
            Debug.Log("<color=green>Đã thu tiền một khách! Các khách sau tiến lên trên.</color>");
        }
        else
        {
            Debug.Log("<color=yellow>Chưa có ai đứng đợi ở quầy cả!</color>");
        }
    }
}