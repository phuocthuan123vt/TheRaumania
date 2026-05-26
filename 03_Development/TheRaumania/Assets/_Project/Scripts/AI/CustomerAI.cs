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
    public Transform exitPoint; // Điểm đi ra về (SpawnPoint)

    private float _checkoutWaitTime = 1.5f;     // Chờ 1.5s thanh toán tại bàn nếu ko có quầy thu ngân

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
    public bool isPaid = false; // Flag đợi thu ngân nhấp E
    
    // Behavior Tree
    private Node _rootNode;

    [Header("Order UI & Emotes")]
    public RecipeSO wantedRecipe;
    public GameObject orderBubble;
    public Image imgOrderIcon;
    
    [Header("Emote Sprites (Kéo hình vào đây)")]
    public Sprite emoteAngry;
    public Sprite emoteHappy;
    public Sprite emoteWave;
    public Sprite emotePay;

    public int _mySeatIndex;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponentInChildren<Animator>();

        // Thiết lập Agent chạy trong môi trường 2D giả lập
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;

        // Tự động ẩn khung nền giả của Bubble Chat (vi ảnh emote đã tự có bubble)
        if (orderBubble != null)
        {
            if (orderBubble.TryGetComponent<Image>(out Image bgImage))
            {
                bgImage.enabled = false; // Tắt component Image chứa khung
            }
            if (orderBubble.TryGetComponent<SpriteRenderer>(out SpriteRenderer bgSprite))
            {
                bgSprite.enabled = false; // Dành cho trường hợp dùng SpriteRenderer
            }
        }
    }

    private void Start()
    {
        currentState = CustomerState.Queueing;
        
        // Tự động tìm Quầy thu ngân cực mạnh (bất chấp sai tên)
        if (checkoutCounter == null)
        {
            CheckoutManager mgr = FindObjectOfType<CheckoutManager>();
            if (mgr != null) checkoutCounter = mgr.transform;
            else
            {
                GameObject counter = GameObject.Find("CheckoutPoint");
                if (counter != null) checkoutCounter = counter.transform;
            }
        }

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
                // Cho ăn 0 điểm toàn diện vì đợi quá lâu
                _dishStars = 0f; 
                SubmitSatisfactionReview();

                // Hiển thị Emote Tức giận
                ShowEmote(emoteAngry);
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
            // Hiển thị bóng vẫy tay
            ShowEmote(emoteWave);
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
            if (_isDoneEating) return NodeState.Success;
            if (currentState != CustomerState.Eating) return NodeState.Failure;
            return NodeState.Running; // Trạng thái này sẽ kết thúc bởi Coroutine EatRoutine
        });

        // 2.5 Checkout Sequence
        ActionNode moveToCounter = new ActionNode(() => 
        {
            if (!_isDoneEating) return NodeState.Failure;

            // Bảo hiểm: Tìm lại quầy thu ngân nếu mất kết nối
            if (CheckoutManager.Instance == null) CheckoutManager.Instance = FindObjectOfType<CheckoutManager>();

            if (currentState == CustomerState.Eating)
            {
                currentState = CustomerState.CheckingOut;
                ShowEmote(emotePay);
                
                _anim.SetBool("IsSitting", false);

                // MẸO CHỮA KẸT NAVMESH BẰNG CODE: Tìm vùng xanh an toàn gần nhất
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    transform.position = hit.position; // Trượt nhẹ NPC ra khỏi hố đen của ghế
                }

                _agent.enabled = true;
                
                // Giải quyết triệt để lỗi Trượt Y: Trọng lực phải bằng 0 trong game Top-Down!
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.simulated = true;
                    rb.gravityScale = 0f; // Fix cứng ngắc: Không bao giờ rơi xuống hố nữa
                    rb.velocity = Vector2.zero;
                }

                if (CheckoutManager.Instance != null)
                {
                    CheckoutManager.Instance.JoinQueue(this);
                }
            }
            
            // XỬ LÝ ĐỨNG XẾP HÀNG THU NGÂN
            if (CheckoutManager.Instance != null)
            {
                Vector3 targetPos = CheckoutManager.Instance.GetQueuePosition(this);
                _agent.SetDestination(targetPos);
                
                // Đứng đây trừ kiên nhẫn dần dần cho đến khi isPaid = true (Player ấn E)
                if (isPaid) return NodeState.Success;
                
                currentPatience -= (profile.patienceDecayRate * 0.5f) * Time.deltaTime; // Trừ chậm hơn một chút
                return NodeState.Running;
            }

            // --- TRƯỜNG HỢP AUTO FALLBACK NẾU CHƯA LÀM CHECKOUT MANAGER ---
            if (checkoutCounter != null)
            {
                _agent.SetDestination(checkoutCounter.position);
                // Fix lỗi NavMesh nhận nhầm khoảng cách: dùng Distance thực tế thay vì _agent.remainingDistance
                if (Vector2.Distance(transform.position, checkoutCounter.position) <= 1.0f) 
                    return NodeState.Success;
                
                return NodeState.Running;
            }
            
            _checkoutWaitTime -= Time.deltaTime;
            if (_checkoutWaitTime <= 0) return NodeState.Success;

            return NodeState.Running;
        });
        ActionNode payAndTipAction = new ActionNode(() => 
        {
            ProcessPayment(); // Gọi hàm thanh toán
            return NodeState.Success;
        });
        ActionNode happyAndLeaveAction = new ActionNode(() => 
        {
            ShowEmote(emoteHappy);
            currentState = CustomerState.Leaving;
            OnLeave(); // <--- Đưa hàm gọi đi về cửa (Exit) vào ĐÚNG NƠI NÀY
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
            
            // 🔥 TRỌNG TÂM FIX BUG ĐÂY: Hồi phục kiên nhẫn khi có đồ ăn + Dừng trừ chờ!
            currentPatience = 100f; 
            
            if (txtStatus != null)
                Debug.Log($"Khách nhận món: {dish.itemName} ({stars:F1} sao)");

            StartCoroutine(EatRoutine());
        }
    }

    public bool isSpecialRequestMet = false; // Để mảng Request xử lý
    public GameObject dirtPrefab; // Rớt rác

    IEnumerator EatRoutine()
    {
        yield return new WaitForSeconds(5f); 
        _isDoneEating = true; // Báo hiệu ActionNode "eatAction" là đã ăn xong !
    }

    // --- Các hàm tương tác với Player / Môi trường ---

    public void ReceivePaymentByPlayer()
    {
        isPaid = true;
    }

    void ProcessPayment()
    {
        int baseRC = (wantedRecipe != null && wantedRecipe.dishResultSO != null) ? wantedRecipe.dishResultSO.basePrice : 100;
        float tip = (_dishStars * 10f) * profile.tipMultiplier;
        PlayerData.AddCredit(Mathf.RoundToInt(baseRC + tip));

        Debug.Log($"Khách trả {baseRC + tip} RC và chuẩn bị Review.");

        SubmitSatisfactionReview();
    }

    void SubmitSatisfactionReview()
    {
        // 1. Tính % Kiên nhẫn còn lại (ra 10 điểm)
        float patienceScore = (Mathf.Max(0, currentPatience) / 100f) * 10f;
        
        // 2. Tính Điểm Món (dishStars là thang 5 -> nhân 2 ra 10 điểm)
        float dishScore = Mathf.Clamp(_dishStars * 2f, 0f, 10f);

        // 3. Yêu cầu đặc biệt (Nếu khách VIP có request thì max 10 điểm, không met thì 0)
        float specialScore = isSpecialRequestMet ? 10f : 0f;

        float satisfaction = 0f;

        switch (profile.type)
        {
            case CustomerType.Chill: // Normal
                satisfaction = (patienceScore * 0.25f) + (dishScore * 0.75f);
                break;
            case CustomerType.RichAndRush: // VIP
                satisfaction = (patienceScore * 0.15f) + (dishScore * 0.65f) + (specialScore * 0.20f);
                break;
            case CustomerType.FoodCritic: // Phê bình
                satisfaction = (patienceScore * 0.10f) + (dishScore * 0.75f) + (specialScore * 0.15f);
                break;
        }

        satisfaction = Mathf.Clamp(satisfaction, 0f, 10f);
        
        if (RestaurantRatingManager.Instance != null)
        {
            RestaurantRatingManager.Instance.SubmitCustomerReview(satisfaction, dishScore);
        }
    }

    void OnLeave()
    {
        currentState = CustomerState.Leaving; // Đảm bảo khóa cứng Trạng Thái
        _isPatienceActive = false;
        
        // --- HYGIENE SYSTEM (ĐÁNH RỚT RÁC LÀM TUỘT VỆ SINH) ---
        if (dirtPrefab != null && Random.value < 0.9f) // 90% rớt rác
        {
            Instantiate(dirtPrefab, transform.position, Quaternion.identity);
            if (RestaurantRatingManager.Instance != null)
                RestaurantRatingManager.Instance.DecreaseHygiene(0.5f); // Làm tuột nửa điểm vệ sinh
        }

        // --- BẬT LẠI VẬT LÝ NẾU ĐANG BỊ KHÓA TRÊN GHẾ ---
        if (GetComponent<Rigidbody2D>() != null)
        {
            GetComponent<Rigidbody2D>().simulated = true;
        }

        // --- XÓA KHỎI HÀNG ĐỢI NẾU CÓ ---
        if (CheckoutManager.Instance != null) CheckoutManager.Instance.LeaveQueue(this);

        if (assignedTable != null) assignedTable.seats[_mySeatIndex].isOccupied = false;

        _anim.SetBool("IsSitting", false);

        // Chữa kẹt NavMesh lúc tức giận bỏ ngang
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }

        _agent.enabled = true;
        
        // --- XỬ LÝ LỐI RA (THÔNG MINH HƠN) ---
        Vector3 finalDest = new Vector3(0, -10, 0); // Failsafe
        
        if (exitPoint != null) 
        {
            finalDest = exitPoint.position;
        }
        else 
        {
            // Bắt sóng trực tiếp Spawner dể lấy điểm đi về (đỡ lo gõ sai tên)
            CustomerSpawner spawner = FindObjectOfType<CustomerSpawner>();
            if (spawner != null)
            {
                finalDest = spawner.transform.position;
            }
            else
            {
                GameObject autoDoor = GameObject.Find("SpawnPoint");
                if (autoDoor == null) autoDoor = GameObject.Find("ExitPoint");
                if (autoDoor != null) finalDest = autoDoor.transform.position;
            }
        }

        _agent.SetDestination(finalDest);
        Destroy(gameObject, 8f);
    }

    public void ShowEmote(Sprite emoteSprite)
    {
        if (orderBubble != null && imgOrderIcon != null && emoteSprite != null)
        {
            orderBubble.SetActive(true);
            imgOrderIcon.sprite = emoteSprite;
        }
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