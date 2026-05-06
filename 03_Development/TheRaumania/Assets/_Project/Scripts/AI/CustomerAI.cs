using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using BehaviorTree;

public class CustomerAI : MonoBehaviour
{
    public enum CustomerState { Queueing, WalkingToTable, Ordering, WaitingForFood, Eating, CheckingOut, Leaving }

    [Header("Dữ liệu & Trạng thái")]
    public CustomerProfileSO profile;
    public CustomerState currentState;
    public float currentPatience = 100f;

    [Header("Tham chiếu Logic")]
    public Table assignedTable;
    public TextMeshProUGUI txtStatus;

    private NavMeshAgent _agent;
    private Animator _anim;
    private float _targetSitX; 
    private float _dishStars;  
    private bool _isPatienceActive = true;
    private bool _hasOrdered = false;
    private bool _hasReceivedFood = false;
    private bool _isDoneEating = false;
    
    // Behavior Tree
    private Node _rootNode;

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
        ConstructBehaviorTree();
    }

    private void ConstructBehaviorTree()
    {
        // 1. Nhánh Xếp hàng & Chờ bàn (Tượng trưng, thường do System bên ngoài cấp bàn)
        ActionNode queueNode = new ActionNode(() =>
        {
            if (currentState == CustomerState.Queueing) return NodeState.Running;
            return NodeState.Success;
        });

        // 2. Nhánh Đi tới bàn
        ActionNode walkToTableNode = new ActionNode(() =>
        {
            if (currentState != CustomerState.WalkingToTable) return NodeState.Failure;

            if (!_agent.pathPending && _agent.remainingDistance < 0.1f)
            {
                OnArrivedAtTable();
                return NodeState.Success;
            }
            return NodeState.Running;
        });

        // 3. Nhánh Order & Chờ phục vụ
        ActionNode waitOrderTakeNode = new ActionNode(() =>
        {
            if (currentState == CustomerState.Ordering) return NodeState.Running;
            if (_hasOrdered) return NodeState.Success;
            return NodeState.Failure;
        });

        ActionNode waitFoodNode = new ActionNode(() =>
        {
            if (currentState == CustomerState.WaitingForFood) return NodeState.Running;
            if (_hasReceivedFood) return NodeState.Success;
            return NodeState.Failure;
        });

        Sequence eatingSequence = new Sequence(new List<Node>() 
        { 
            waitOrderTakeNode, 
            waitFoodNode 
        });

        // 4. Ghép cây
        _rootNode = new Sequence(new List<Node>
        {
            queueNode,
            walkToTableNode,
            eatingSequence
        });
    }

    private void Update()
    {
        UpdateVisuals();
        _rootNode?.Evaluate();
    }

    #region AI Logic Flow

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

    public void TakeOrder()
    {
        if (currentState == CustomerState.Ordering)
        {
            _hasOrdered = true;
            currentState = CustomerState.WaitingForFood;
            Debug.Log("Alex đã nhận order. Khách đang đợi món.");
        }
    }

    public void ReceiveFood(BaseItemSO dish, float stars) 
    {
        if (currentState == CustomerState.WaitingForFood)
        {
            _hasReceivedFood = true;
            orderBubble.SetActive(false);
            _dishStars = stars;
            currentState = CustomerState.Eating;
            if (txtStatus != null)
                Debug.Log($"Khách nhận món: {dish.itemName} ({stars:F1} sao)");

            StartCoroutine(EatRoutine());
        }
    }

    IEnumerator EatRoutine()
    {
        yield return new WaitForSeconds(5f); 
        ProcessPayment();
    }

    void ProcessPayment()
    {
        int baseRC = (wantedRecipe != null && wantedRecipe.dishResultSO != null) ? wantedRecipe.dishResultSO.basePrice : 100;
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
        _agent.SetDestination(new Vector3(0, -10, 0));
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