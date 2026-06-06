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

    [Header("Thanh toán")]
    [Tooltip("Khoảng cách tối đa (đơn vị Unity) để chấp nhận thanh toán từ khách ở đầu hàng.")]
    public float acceptPaymentRange = 1.0f;

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
            // Immediately set the customer's NavMesh target to the calculated queue position
            if (customer != null)
            {
                var agent = customer.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = true;
                    agent.SetDestination(GetQueuePosition(customer));
                }
            }
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
        if (queue.Count == 0)
        {
            Debug.Log("<color=yellow>Chưa có ai đứng đợi ở quầy cả!</color>");
            return;
        }

        // Find the first customer in queue who is within acceptPaymentRange of the checkout point
        CustomerAI customerToPay = null;
        int indexToRemove = -1;
        for (int i = 0; i < queue.Count; i++)
        {
            var c = queue[i];
            if (c == null) continue;
            float dist = Vector2.Distance(new Vector2(c.transform.position.x, c.transform.position.y), new Vector2(transform.position.x, transform.position.y));
            if (dist <= acceptPaymentRange)
            {
                customerToPay = c;
                indexToRemove = i;
                break;
            }
        }

        if (customerToPay != null)
        {
            // Remove that specific customer from the queue
            queue.RemoveAt(indexToRemove);
            customerToPay.ReceivePaymentByPlayer();
            Debug.Log("<color=green>Đã thu tiền một khách! Các khách sau tiến lên trên.</color>");
            // After removing, update destinations for remaining customers so they step forward
            for (int i = 0; i < queue.Count; i++)
            {
                var c = queue[i];
                if (c == null) continue;
                var agent = c.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    agent.SetDestination(GetQueuePosition(c));
                }
            }
        }
        else
        {
            Debug.Log("<color=yellow>Không có khách nào đến quầy để nhận thanh toán (chưa đến chỗ).</color>");
        }
    }
}