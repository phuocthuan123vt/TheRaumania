using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class CustomerAI : MonoBehaviour
{
    public enum CustomerState { Queueing, WalkingToTable, Ordering, WaitingForFood, Eating, CheckingOut, Leaving }

    [Header("Dữ liệu & Trạng thái")]
    public CustomerProfileSO profile;
    public CustomerState currentState;
    public float currentPatience = 100f;

    [Header("Tham chiếu Logic")]
    public Table assignedTable;
    public TextMeshProUGUI txtStatus; // Kéo Text trên đầu NPC vào đây

    private NavMeshAgent _agent;
    private Animator _anim;
    private float _targetSitX; // Lưu hướng nhìn khi ngồi (-1 hoặc 1)
    private float _dishStars;  // Lưu số sao món ăn nhận được
    private bool _isPatienceActive = true;

    [Header("Order UI")]
    public RecipeSO wantedRecipe;
    public GameObject orderBubble;
    public Image imgOrderIcon;

    public int _mySeatIndex;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponentInChildren<Animator>();

        // Thiết lập Agent chạy trong môi trường 2D giả lập
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
    }

    private void Start()
    {
        currentState = CustomerState.Queueing;
        StartCoroutine(PatienceRoutine());
    }

    private void Update()
    {
        UpdateVisuals();

        // Kiểm tra nếu đã đi đến bàn thành công
        if (currentState == CustomerState.WalkingToTable)
        {
            if (!_agent.pathPending && _agent.remainingDistance < 0.1f)
            {
                OnArrivedAtTable();
            }
        }
    }

    #region AI Logic Flow

    // Giảm kiên nhẫn mỗi giây (GDD trang 10)
    IEnumerator PatienceRoutine()
    {
        while (currentPatience > 0 && _isPatienceActive)
        {
            yield return new WaitForSeconds(1f);
            currentPatience -= profile.patienceDecayRate;

            if (currentPatience <= 0) OnPatienceOut();
        }
    }


    public void LeadToSeat(Table table, int seatIndex)
    {
        assignedTable = table;
        _mySeatIndex = seatIndex;
        assignedTable.seats[_mySeatIndex].isOccupied = true;
        _targetSitX = assignedTable.seats[_mySeatIndex].sitDirection;

        currentState = CustomerState.WalkingToTable;
        _agent.enabled = true;
        _agent.SetDestination(assignedTable.seats[_mySeatIndex].point.position);
    }

    void OnArrivedAtTable()
    {
        currentState = CustomerState.Ordering;
        _agent.enabled = false;
        if (GetComponent<Rigidbody2D>() != null)
            GetComponent<Rigidbody2D>().simulated = false;
        transform.position = assignedTable.seats[_mySeatIndex].point.position;
        _anim.SetBool("IsSitting", true);
        _anim.SetBool("IsMoving", false);
        _anim.SetFloat("SitX", _targetSitX);

        RecipeBookUI recipeBook = FindObjectOfType<RecipeBookUI>(true);

        if (recipeBook != null && recipeBook.allRecipes != null && recipeBook.allRecipes.Count > 0)
        {
            wantedRecipe = recipeBook.allRecipes[Random.Range(0, recipeBook.allRecipes.Count)];

            if (orderBubble != null && imgOrderIcon != null)
            {
                orderBubble.SetActive(true);
                imgOrderIcon.sprite = wantedRecipe.dishIcon;
                Debug.Log("<color=green>Khách muốn ăn: </color>" + wantedRecipe.dishName);
            }
            else
            {
                Debug.LogError("Thuận ơi! Ông chưa kéo OrderBubble hoặc imgOrderIcon vào NPC rồi!");
            }
        }
        else
        {
            Debug.LogError("Không tìm thấy RecipeBookUI hoặc danh sách món ăn (allRecipes) đang trống!");
        }
    }

    // Alex nhấn E lần 2 tại bàn để lấy order
    public void TakeOrder()
    {
        if (currentState == CustomerState.Ordering)
        {
            currentState = CustomerState.WaitingForFood;
            Debug.Log("Alex đã nhận order. Khách đang đợi món.");
        }
    }

    // Alex bưng món ra nhấn E lần 3
    public void ReceiveFood(BaseItemSO dish, float stars) // Thêm tham số dish
    {
        if (currentState == CustomerState.WaitingForFood)
        {
            orderBubble.SetActive(false);
            _dishStars = stars;
            currentState = CustomerState.Eating;

            // Hiện thông báo món ăn nhận được
            if (txtStatus != null)
                Debug.Log($"Khách nhận món: {dish.itemName} ({stars:F1} sao)");

            StartCoroutine(EatRoutine());
        }
    }

    IEnumerator EatRoutine()
    {
        yield return new WaitForSeconds(5f); // Thời gian ăn
        ProcessPayment();
    }

    void ProcessPayment()
    {
        // Tính tiền: Base + Tip dựa trên Stars và Profile (GDD trang 5)
        int baseRC = 100;
        float tip = (_dishStars * 10f) * profile.tipMultiplier;
        PlayerData.rCredit += Mathf.RoundToInt(baseRC + tip);

        Debug.Log($"Khách trả {baseRC + tip} RC và rời quán.");

        currentState = CustomerState.Leaving;
        OnLeave();
    }

    void OnLeave()
    {
        _isPatienceActive = false;
        if (assignedTable != null) assignedTable.seats[_mySeatIndex].isOccupied = false;

        _anim.SetBool("IsSitting", false);
        _agent.enabled = true;
        _agent.SetDestination(new Vector3(0, -10, 0)); // Đi ra cửa
        Destroy(gameObject, 10f);
    }

    void OnPatienceOut()
    {
        Debug.Log("<color=red>Khách bỏ về vì hết kiên nhẫn!</color>");
        OnLeave();
    }
    #endregion

    #region Visuals
    void UpdateVisuals()
    {
        if (txtStatus != null)
        {
            txtStatus.text = $"{currentState}\nPat: {Mathf.Round(currentPatience)}%";
            txtStatus.color = Color.Lerp(Color.red, Color.green, currentPatience / 100f);
        }

        if (_anim == null || !_agent.enabled) return;

        if (currentState == CustomerState.Ordering || currentState == CustomerState.WaitingForFood || currentState == CustomerState.Eating)
        {
            _anim.SetBool("IsSitting", true);
            _anim.SetBool("IsMoving", false);
            _anim.SetFloat("SitX", _targetSitX);
            return;
        }

        if (_agent.velocity.magnitude > 0.1f)
        {
            Vector2 dir = _agent.velocity.normalized;
            _anim.SetBool("IsMoving", true);
            _anim.SetFloat("MoveX", dir.x);
            _anim.SetFloat("MoveY", dir.y);
        }
        else
        {
            _anim.SetBool("IsMoving", false);
        }
    }
    #endregion
}