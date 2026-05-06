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

    [Header("Behavior Tree Flags")]
    public bool hasUpgrade_CallStaff = false;
    public Transform checkoutCounter;

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
        // Logic trừ Patience giờ sẽ do BT quản lý (không còn dùng Coroutine)
        ConstructBehaviorTree();
    }

    private void ConstructBehaviorTree()
    {
        // ============================================
        // NHÁNH 1: FORCED EXIT (Ưu tiên cao nhất)
        // ============================================
        ActionNode checkPatienceOut = new ActionNode(() => currentPatience <= 0 ? NodeState.Success : NodeState.Failure);
        ActionNode exitAction = new ActionNode(() => 
        {
            if (currentState != CustomerState.Leaving)
            {
                Debug.Log("<color=red>Khách bỏ về vì hết kiên nhẫn!</color>");
                // TODO: Chèn logic / animation Angry Emote tại đây
                OnLeave();
            }
            return NodeState.Success; 
        });
        Sequence forcedExitSequence = new Sequence(new List<Node> { checkPatienceOut, exitAction });

        // ============================================
        // NHÁNH 2: VÒNG LẶP PHỤC VỤ (Standard Service)
        // ============================================

        // 2.1 Wait For Table (Chờ bàn)
        ActionNode checkIfAtTable = new ActionNode(() => 
        {
            if (currentState == CustomerState.Queueing) return NodeState.Failure; 
            if (currentState == CustomerState.WalkingToTable)
            {
                if (!_agent.pathPending && _agent.remainingDistance < 0.1f)
                {
                    OnArrivedAtTable();
                    return NodeState.Success;
                }
                return NodeState.Failure;
            }
            return NodeState.Success; // Đã tới bàn
        });
        ActionNode waitInQueue = new ActionNode(() => 
        {
            currentPatience -= profile.patienceDecayRate * Time.deltaTime; // Giảm kiên nhẫn
            return NodeState.Running;
        });
        Selector waitingForTable = new Selector(new List<Node> { checkIfAtTable, waitInQueue });

        // 2.2 Ordering Process (Quá trình chọn món)
        ActionNode checkOrderTaken = new ActionNode(() => _hasOrdered ? NodeState.Success : NodeState.Failure);
        ActionNode waitForStaffAction = new ActionNode(() => 
        {
            currentPatience -= profile.patienceDecayRate * Time.deltaTime;
            return NodeState.Running;
        });
        Selector orderingProcess = new Selector(new List<Node> { checkOrderTaken, waitForStaffAction });

        // 2.3 Waiting For Food (Chờ đồ ăn lên)
        ActionNode checkFoodServed = new ActionNode(() => _hasReceivedFood ? NodeState.Success : NodeState.Failure);
        
        // 2.3.1 Tương tác khi chờ (Waiting Interaction)
        ActionNode checkUpgrade = new ActionNode(() => (currentPatience < 50f && hasUpgrade_CallStaff) ? NodeState.Success : NodeState.Failure);
        ActionNode waveHandAction = new ActionNode(() => 
        {
            // TODO: Chạy logic vẫy tay gọi nhân viên (Ask Staff / Wave Hand)
            return NodeState.Running;
        });
        Sequence upgradeInteraction = new Sequence(new List<Node> { checkUpgrade, waveHandAction });
        ActionNode idleWaitAction = new ActionNode(() => 
        {
            currentPatience -= profile.patienceDecayRate * Time.deltaTime;
            // TODO: Animation Look Around (nếu có)
            return NodeState.Running;
        });
        Selector waitingInteraction = new Selector(new List<Node> { upgradeInteraction, idleWaitAction });
        Selector waitingForFood = new Selector(new List<Node> { checkFoodServed, waitingInteraction });

        // 2.4 Ăn & tăng Mood (Eat & Boost Mood)
        ActionNode eatAction = new ActionNode(() => 
        {
            if (currentState != CustomerState.Eating) return NodeState.Failure;
            if (_isDoneEating) return NodeState.Success; 
            return NodeState.Running; // Trạng thái này sẽ kết thúc bởi Coroutine EatRoutine
        });

        // 2.5 Checkout Sequence
        ActionNode moveToCounter = new ActionNode(() => 
        {
            if (currentState == CustomerState.Eating) currentState = CustomerState.CheckingOut;
            if (checkoutCounter != null)
            {
                _agent.enabled = true;
                _anim.SetBool("IsSitting", false);
                _agent.SetDestination(checkoutCounter.position);
                if (!_agent.pathPending && _agent.remainingDistance <= 0.1f) return NodeState.Success;
                return NodeState.Running;
            }
            return NodeState.Success; // Nếu chưa gán counter, tính như thể đã tự đi tới quầy
        });
        ActionNode payAndTipAction = new ActionNode(() => 
        {
            ProcessPayment(); // Gọi hàm thanh toán
            return NodeState.Success;
        });
        ActionNode happyAndLeaveAction = new ActionNode(() => 
        {
            // TODO: Play "Happy" Emote
            currentState = CustomerState.Leaving;
            return NodeState.Success;
        });
        Sequence checkoutSequence = new Sequence(new List<Node> { moveToCounter, payAndTipAction, happyAndLeaveAction });

        // Lắp các Selector lại vào Vòng Lặp Phục Vụ tiêu chuẩn
        Sequence standardServiceLoop = new Sequence(new List<Node>
        {
            waitingForTable,
            orderingProcess,
            waitingForFood,
            eatAction,
            checkoutSequence
        });

        // ============================================
        // LẮP RÁP BỘ NÃO TỔNG THỂ (ROOT)
        // ============================================
        _rootNode = new Selector(new List<Node>
        {
            forcedExitSequence,
            standardServiceLoop
        });
    }

    private void Update()
    {
        UpdateVisuals();

        // Chạy qua Root nếu khách chưa bỏ về
        if (_isPatienceActive && currentState != CustomerState.Leaving)
        {
            _rootNode?.Evaluate();
        }
    }

    #region AI Logic Flow

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
        _isDoneEating = true; // Báo hiệu ActionNode "eatAction" là đã ăn xong !
    }

    void ProcessPayment()
    {
        int baseRC = (wantedRecipe != null && wantedRecipe.dishResultSO != null) ? wantedRecipe.dishResultSO.basePrice : 100;
        float tip = (_dishStars * 10f) * profile.tipMultiplier;
        PlayerData.rCredit += Mathf.RoundToInt(baseRC + tip);

        Debug.Log($"Khách trả {baseRC + tip} RC và rời quán.");
        
        OnLeave();
    }

    void OnLeave()
    {
        _isPatienceActive = false;
        if (assignedTable != null) assignedTable.seats[_mySeatIndex].isOccupied = false;

        _anim.SetBool("IsSitting", false);
        _agent.enabled = true;
        _agent.SetDestination(new Vector3(0, -10, 0));
        Destroy(gameObject, 10f); // Thay đổi vector này thành điểm End Map cho hợp lý.
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