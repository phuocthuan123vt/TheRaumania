using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using BehaviorTree;
using UnityEngine.SceneManagement;

public class CustomerAI : MonoBehaviour
{
    public enum CustomerState { Queueing, BeingLed, WalkingToTable, Ordering, WaitingForFood, Eating, CheckingOut, Leaving }

    [Header("Du lieu & Trang thai")]
    public CustomerProfileSO profile;
    
    private CustomerState _currentState;
    public CustomerState currentState
    {
        get => _currentState;
        set
        {
            CustomerState oldState = _currentState;
            if (oldState == value) return;
            _currentState = value;
            OnStateChanged(oldState, _currentState);
        }
    }

    public float currentPatience = 100f;

    [Header("Behavior Tree Flags")]
    public bool hasUpgrade_CallStaff = false;

    private float _checkoutWaitTime = 1.5f;

    [Header("Tham chieu Logic")]
    public Table assignedTable;
    public Slider patienceSlider;
    public Image patienceFillColor;

    private NavMeshAgent _agent;
    private Animator _anim;
    private float _targetSitX;
    private float _dishStars;
    private bool _isPatienceActive = true;
    private bool _hasOrdered = false;
    private bool _hasReceivedFood = false;
    private bool _isDoneEating = false;
    private bool _paymentProcessed = false;
    private float _moodReactionCooldown = 0f;
    public bool isPaid = false;

    private Node _rootNode;

    [Header("Order UI & Emotes")]
    public RecipeSO wantedRecipe;
    public GameObject orderBubble;
    public Image imgOrderIcon;

    [Header("Emote Sprites")]
    public Sprite emoteAngry;
    public Sprite emoteHappy;
    public Sprite emoteWave;
    public Sprite emotePay;
    public Sprite emoteEat;
    public Sprite emoteNauseous;

    public int _mySeatIndex;

    public bool isSpecialRequestMet = false;
    public bool wantsWindowSeat = false;
    private TMPro.TextMeshProUGUI _windowSeatText;
    public GameObject dirtPrefab;

    // --- HÀNG CHỜ TĨNH (QUEUE MANAGEMENT) ---
    public static List<CustomerAI> activeQueue = new List<CustomerAI>();

    public static void UpdateQueuePositions()
    {
        activeQueue.RemoveAll(item => item == null);
        
        Transform spawnTrans = null;
        CustomerSpawner spawner = FindObjectOfType<CustomerSpawner>();
        if (spawner != null && spawner.spawnPoint != null)
        {
            spawnTrans = spawner.spawnPoint;
        }
        else
        {
            GameObject go = GameObject.Find("SpawnPoint");
            if (go == null) go = GameObject.Find("ExitPoint");
            if (go != null) spawnTrans = go.transform;
        }

        if (spawnTrans == null) return;

        float spacing = 0.8f;
        for (int i = 0; i < activeQueue.Count; i++)
        {
            CustomerAI customer = activeQueue[i];
            if (customer != null && customer.currentState == CustomerState.Queueing)
            {
                Vector3 targetPos = spawnTrans.position + Vector3.up * (i * spacing);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPos, out hit, 10.0f, NavMesh.AllAreas))
                {
                    targetPos = hit.position;
                }
                customer.SetQueueDestination(targetPos);
            }
        }
    }

    public static int GetQueueCount()
    {
        activeQueue.RemoveAll(item => item == null);
        return activeQueue.Count;
    }

    private void OnStateChanged(CustomerState oldState, CustomerState newState)
    {
        if (oldState == CustomerState.Queueing && newState != CustomerState.Queueing)
        {
            if (activeQueue.Contains(this))
            {
                activeQueue.Remove(this);
                UpdateQueuePositions();
            }
        }
        else if (oldState != CustomerState.Queueing && newState == CustomerState.Queueing)
        {
            if (!activeQueue.Contains(this))
            {
                activeQueue.Add(this);
                UpdateQueuePositions();
            }
        }
    }

    public bool SafeSetDestination(Vector3 dest, float stoppingDist = 0f)
    {
        if (_agent == null) return false;
        
        if (!_agent.enabled)
        {
            _agent.enabled = true;
        }

        if (!_agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
            {
                _agent.enabled = false;
                transform.position = hit.position;
                _agent.enabled = true;
            }
        }

        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.stoppingDistance = stoppingDist;
            _agent.SetDestination(dest);
            return true;
        }
        
        Debug.LogWarning($"SafeSetDestination failed: agent is still not on NavMesh at {transform.position}");
        return false;
    }

    public void SetQueueDestination(Vector3 targetPos)
    {
        SafeSetDestination(targetPos, 0f);
    }

    private void OnDestroy()
    {
        if (activeQueue.Contains(this))
        {
            activeQueue.Remove(this);
            UpdateQueuePositions();
        }
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponentInChildren<Animator>();

        _agent.updateRotation = false;
        _agent.updateUpAxis = false;

        if (orderBubble != null)
        {
            if (orderBubble.TryGetComponent<Image>(out Image bgImage))
            {
                bgImage.enabled = false;
            }
            if (orderBubble.TryGetComponent<SpriteRenderer>(out SpriteRenderer bgSprite))
            {
                bgSprite.enabled = false;
            }
        }
    }

    private IEnumerator Start()
    {
        currentState = CustomerState.Queueing;
        ConstructBehaviorTree();

        // Tỷ lệ ngẫu nhiên có nhu cầu ngồi bàn cửa sổ theo Profile
        float windowDemandChance = 0.15f; // Mặc định Chill: 15%
        if (profile != null)
        {
            if (profile.type == CustomerType.RichAndRush) windowDemandChance = 0.5f;     // VIP: 50%
            else if (profile.type == CustomerType.FoodCritic) windowDemandChance = 0.3f; // Critic: 30%
        }
        wantsWindowSeat = Random.value < windowDemandChance;

        if (wantsWindowSeat)
        {
            EnsureWindowSeatUI();
            if (_windowSeatText != null)
            {
                _windowSeatText.gameObject.SetActive(true);
            }
        }

        // Chờ 1 frame để NavMeshAgent và vị trí ổn định trên NavMesh
        yield return null;

        if (currentState == CustomerState.Queueing)
        {
            if (!activeQueue.Contains(this))
            {
                activeQueue.Add(this);
                UpdateQueuePositions();
            }
        }
    }

    private Transform FindCheckoutTransform()
    {
        if (CheckoutManager.Instance != null) return CheckoutManager.Instance.transform;
        GameObject go = GameObject.Find("CheckoutPoint");
        if (go != null) return go.transform;
        return null;
    }

    private Transform FindSpawnPointTransform()
    {
        CustomerSpawner spawner = FindObjectOfType<CustomerSpawner>();
        if (spawner != null && spawner.spawnPoint != null) return spawner.spawnPoint;

        GameObject go = GameObject.Find("SpawnPoint");
        if (go == null) go = GameObject.Find("ExitPoint");
        return go != null ? go.transform : null;
    }

    private void ConstructBehaviorTree()
    {
        ActionNode checkPatienceOut = new ActionNode(() => currentPatience <= 0 ? NodeState.Success : NodeState.Failure);
        ActionNode exitAction = new ActionNode(() =>
        {
            if (currentState != CustomerState.Leaving)
            {
                Debug.Log("<color=red>Khach bo ve vi het kien nhan!</color>");
                _dishStars = 0f;
                SubmitSatisfactionReview();
                ShowEmote(emoteAngry);
                OnLeave();
            }
            return NodeState.Success;
        });
        Sequence forcedExitSequence = new Sequence(new List<Node> { checkPatienceOut, exitAction });

        ActionNode checkIfAtTable = new ActionNode(() =>
        {
            if (currentState == CustomerState.Queueing || currentState == CustomerState.BeingLed) return NodeState.Failure;
            if (currentState == CustomerState.WalkingToTable)
            {
                if (!_agent.pathPending && _agent.remainingDistance < 0.1f)
                {
                    OnArrivedAtTable();
                    return NodeState.Success;
                }
                return NodeState.Failure;
            }
            return NodeState.Success;
        });
        ActionNode waitInQueue = new ActionNode(() =>
        {
            currentPatience -= profile.patienceDecayRate * Time.deltaTime;
            return NodeState.Running;
        });
        Selector waitingForTable = new Selector(new List<Node> { checkIfAtTable, waitInQueue });

        ActionNode checkOrderTaken = new ActionNode(() => _hasOrdered ? NodeState.Success : NodeState.Failure);
        ActionNode waitForStaffAction = new ActionNode(() =>
        {
            currentPatience -= profile.patienceDecayRate * Time.deltaTime;
            return NodeState.Running;
        });
        Selector orderingProcess = new Selector(new List<Node> { checkOrderTaken, waitForStaffAction });

        ActionNode checkFoodServed = new ActionNode(() => _hasReceivedFood ? NodeState.Success : NodeState.Failure);
        ActionNode checkUpgrade = new ActionNode(() => (currentPatience < 50f && hasUpgrade_CallStaff) ? NodeState.Success : NodeState.Failure);
        ActionNode waveHandAction = new ActionNode(() =>
        {
            ShowEmote(emoteWave);
            return NodeState.Running;
        });
        Sequence upgradeInteraction = new Sequence(new List<Node> { checkUpgrade, waveHandAction });
        ActionNode idleWaitAction = new ActionNode(() =>
        {
            currentPatience -= profile.patienceDecayRate * Time.deltaTime;
            return NodeState.Running;
        });
        Selector waitingInteraction = new Selector(new List<Node> { upgradeInteraction, idleWaitAction });
        Selector waitingForFood = new Selector(new List<Node> { checkFoodServed, waitingInteraction });

        ActionNode eatAction = new ActionNode(() =>
        {
            if (_isDoneEating) return NodeState.Success;
            if (currentState != CustomerState.Eating) return NodeState.Failure;
            return NodeState.Running;
        });

        ActionNode waitForPayment = new ActionNode(() =>
        {
            if (!_isDoneEating) return NodeState.Running;

            if (currentState == CustomerState.Eating)
            {
                currentState = CustomerState.CheckingOut;
                ShowEmote(emotePay);

                if (_agent.enabled) _agent.enabled = false;
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.simulated = false;
                    rb.velocity = Vector2.zero;
                }

                if (_anim != null)
                {
                    _anim.SetBool("IsSitting", true);
                    _anim.SetBool("IsMoving", false);
                    _anim.SetFloat("SitX", _targetSitX);
                }
            }

            if (!isPaid)
            {
                currentPatience -= (profile.patienceDecayRate * 0.25f) * Time.deltaTime;
                return NodeState.Running;
            }

            return NodeState.Failure;
        });

        ActionNode payAndTipAction = new ActionNode(() =>
        {
            ProcessPayment();
            return NodeState.Success;
        });

        ActionNode happyAndLeaveAction = new ActionNode(() =>
        {
            ShowEmote(emoteHappy);
            currentState = CustomerState.Leaving;
            OnLeave();
            return NodeState.Success;
        });

        Sequence payAndLeaveSequence = new Sequence(new List<Node> { payAndTipAction, happyAndLeaveAction });
        Selector checkoutSequence = new Selector(new List<Node> { waitForPayment, payAndLeaveSequence });

        Sequence standardServiceLoop = new Sequence(new List<Node>
        {
            waitingForTable,
            orderingProcess,
            waitingForFood,
            eatAction,
            checkoutSequence
        });

        _rootNode = new Selector(new List<Node>
        {
            forcedExitSequence,
            standardServiceLoop
        });
    }

    private void Update()
    {
        if (gameObject.scene != SceneManager.GetActiveScene())
        {
            return;
        }

        UpdateVisuals();
        UpdateContextualReactions();

        if (currentState == CustomerState.BeingLed)
        {
            FollowPlayer();
        }

        if (_isPatienceActive && currentState != CustomerState.Leaving)
        {
            _rootNode?.Evaluate();
        }
    }
    #region AI Logic Flow

    public void OnInteractCalled()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnPlayerInteractWithCustomer(this);
        }
        else
        {
            Debug.LogError("Chua co LevelManager trong scene!");
        }
    }

    public void LeadToSeat(Table table, int seatIndex)
    {
        assignedTable = table;
        _mySeatIndex = seatIndex;
        assignedTable.seats[_mySeatIndex].isOccupied = true;
        _targetSitX = assignedTable.seats[_mySeatIndex].sitDirection;

        // Ensure NPC switches to walking state so the behavior tree detects arrival
        currentState = CustomerState.WalkingToTable;
        _isPatienceActive = true;

        if (assignedTable.seats[_mySeatIndex].point != null)
        {
            SafeSetDestination(assignedTable.seats[_mySeatIndex].point.position, 0f);
        }
        else
        {
            Debug.LogWarning($"CustomerAI.LeadToSeat: seat point is null for table {table?.name} seat {seatIndex}");
        }
    }

    void OnArrivedAtTable()
    {
        currentState = CustomerState.Ordering;
        _agent.enabled = false;
        if (_agent != null) _agent.stoppingDistance = 0f;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        transform.position = assignedTable.seats[_mySeatIndex].point.position;
        _anim.SetBool("IsSitting", true);
        _anim.SetBool("IsMoving", false);
        _anim.SetFloat("SitX", _targetSitX);

        if (_windowSeatText != null)
        {
            _windowSeatText.gameObject.SetActive(false);
        }

        ApplySeatComfortBonus();

        if (wantsWindowSeat)
        {
            if (assignedTable != null && assignedTable.isNearWindow)
            {
                ShowEmote(emoteHappy);
                isSpecialRequestMet = true;
                Debug.Log("<color=cyan>Khách rất thích ngồi bàn gần cửa sổ này!</color>");
            }
            else
            {
                ShowEmote(emoteAngry);
                isSpecialRequestMet = false;
                Debug.Log("<color=red>Khách thất vọng vì không được ngồi bàn gần cửa sổ!</color>");
                _moodReactionCooldown = 2.0f;
            }
        }

        RecipeBookUI recipeBook = FindObjectOfType<RecipeBookUI>(true);
        if (recipeBook != null && recipeBook.allRecipes != null && recipeBook.allRecipes.Count > 0)
        {
            wantedRecipe = recipeBook.allRecipes[Random.Range(0, recipeBook.allRecipes.Count)];
            if (orderBubble != null && imgOrderIcon != null)
            {
                orderBubble.SetActive(true);
                imgOrderIcon.sprite = wantedRecipe.dishIcon;
                Debug.Log("<color=green>Khach muon an: </color>" + wantedRecipe.dishName);
            }
            else
            {
                Debug.LogError("Chua keo OrderBubble hoac imgOrderIcon vao NPC!");
            }
        }
        else
        {
            Debug.LogError("Khong tim thay RecipeBookUI hoac allRecipes trong!");
        }
    }

    public void TakeOrder()
    {
        if (currentState == CustomerState.Ordering)
        {
            _hasOrdered = true;
            currentState = CustomerState.WaitingForFood;
            Debug.Log("Da nhan order. Khach dang doi mon.");
            // Ensure the order bubble shows the requested dish (override any emote)
            if (orderBubble != null && imgOrderIcon != null && wantedRecipe != null)
            {
                orderBubble.SetActive(true);
                imgOrderIcon.sprite = wantedRecipe.dishIcon;
                // prevent immediate mood reactions from overwriting the order icon
                _moodReactionCooldown = 2.5f;
            }
        }
    }

    public void ReceiveFood(BaseItemSO dish, float stars)
    {
        if (currentState == CustomerState.WaitingForFood)
        {
            _hasReceivedFood = true;
            _paymentProcessed = false;
            if (orderBubble != null) orderBubble.SetActive(false);
            _dishStars = stars;
            currentState = CustomerState.Eating;
            isPaid = false;
            currentPatience = Mathf.Clamp(currentPatience + GetFoodSatisfactionPatienceBonus(), 0f, 100f);

            if (emoteEat != null) ShowEmote(emoteEat);
            Debug.Log($"Khach nhan mon: {dish.itemName} ({stars:F1} sao)");

            // Chance to show a nausea emote when the dish is very poor (< 2 stars)
            if (emoteNauseous != null && _dishStars < 2f)
            {
                float nauseaRoll = Random.value;
                float nauseaChance = Random.Range(0.2f, 0.3f);
                if (nauseaRoll <= nauseaChance)
                {
                    float dur = Random.Range(1f, 2f);
                    StartCoroutine(ShowTemporaryEmoteCoroutine(emoteNauseous, dur));
                }
            }

            StartCoroutine(EatRoutine());
        }
    }

    IEnumerator EatRoutine()
    {
        yield return new WaitForSeconds(Random.Range(10f, 15f));
        _isDoneEating = true;
        currentState = CustomerState.CheckingOut;
        ShowEmote(emotePay);
    }

    public void ReceivePaymentByPlayer()
    {
        if (!_paymentProcessed)
        {
            ProcessPayment();
        }
        isPaid = true;
        ShowEmote(emoteHappy);
        OnLeave();
    }

    private void StartLeaveFromTable()
    {
        OnLeave();
    }

    private Vector3 ResolveLeaveDestination()
    {
        Vector3 finalDest = new Vector3(0, -10, 0);

        Transform spawnTrans = FindSpawnPointTransform();
        if (spawnTrans != null)
        {
            finalDest = spawnTrans.position;
        }
        else
        {
            CustomerSpawner spawner = FindObjectOfType<CustomerSpawner>();
            if (spawner != null)
            {
                finalDest = spawner.spawnPoint != null ? spawner.spawnPoint.position : spawner.transform.position;
            }
            else
            {
                GameObject autoDoor = GameObject.Find("SpawnPoint");
                if (autoDoor == null) autoDoor = GameObject.Find("ExitPoint");
                if (autoDoor != null) finalDest = autoDoor.transform.position;
            }
        }

        return finalDest;
    }

    private IEnumerator MonitorLeaveProgress(Vector3 dest)
    {
        float checkInterval = 0.25f;
        float stuckTime = 0f;
        Vector3 lastPos = transform.position;

        while (currentState == CustomerState.Leaving)
        {
            yield return new WaitForSeconds(checkInterval);

            float moved = Vector3.Distance(transform.position, lastPos);
            if (moved < 0.01f)
            {
                stuckTime += checkInterval;
            }
            else
            {
                stuckTime = 0f;
            }

            lastPos = transform.position;

            if (stuckTime >= 0.75f)
            {
                Vector3 escapePoint = FindEscapePointTowards(dest);
                if (escapePoint != Vector3.zero && Vector3.Distance(transform.position, escapePoint) > 0.05f)
                {
                    StartCoroutine(LeaveThroughWaypoints(escapePoint, dest));
                }
                else
                {
                    StartCoroutine(ForceMoveToSpawn(dest));
                }
                yield break;
            }
        }
    }

    private IEnumerator LeaveThroughWaypoints(Vector3 escapePoint, Vector3 finalDest)
    {
        if (_agent != null && !_agent.enabled) _agent.enabled = true;
        if (_agent != null)
        {
            _agent.isStopped = false;
            _agent.autoBraking = true;
            _agent.ResetPath();
            _agent.SetDestination(escapePoint);
        }

        float timeout = 8f;
        bool escapeReached = false;

        while (timeout > 0f && currentState == CustomerState.Leaving)
        {
            timeout -= Time.deltaTime;

            if (!escapeReached && Vector3.Distance(transform.position, escapePoint) <= 0.25f)
            {
                escapeReached = true;
                if (_agent != null && _agent.isOnNavMesh)
                {
                    _agent.ResetPath();
                    _agent.SetDestination(finalDest);
                }
            }

            if (escapeReached && _agent != null && _agent.pathStatus == NavMeshPathStatus.PathComplete)
            {
                yield break;
            }

            yield return null;
        }

        if (currentState == CustomerState.Leaving)
        {
            StartCoroutine(ForceMoveToSpawn(finalDest));
        }
    }

    private IEnumerator ForceMoveToSpawn(Vector3 dest)
    {
        if (_agent != null && _agent.enabled) _agent.enabled = false;

        float timeout = 6f;
        while (timeout > 0f)
        {
            Vector3 dir = (dest - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
                if (_anim != null)
                {
                    _anim.SetBool("IsMoving", true);
                    _anim.SetFloat("MoveX", dir.x);
                    _anim.SetFloat("MoveY", dir.y);
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, dest, 1.5f * Time.deltaTime);
            timeout -= Time.deltaTime;
            yield return null;
        }
    }

    private Vector3 FindEscapePointTowards(Vector3 finalDest)
    {
        Vector3 origin = transform.position;
        Vector3 direction = finalDest - origin;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.up;
        }

        direction.Normalize();

        float[] distances = { 0.35f, 0.55f, 0.8f, 1.1f, 1.4f };
        for (int i = 0; i < distances.Length; i++)
        {
            Vector3 candidate = origin + direction * distances[i];
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, 0.8f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(hit.position, origin) > 0.05f)
                {
                    return hit.position;
                }
            }
        }

        return Vector3.zero;
    }

    private Vector3 FindLeaveStandPoint()
    {
        Vector3 origin = transform.position;
        Vector3 seatOrigin = origin;
        float seatDirection = 0f;

        if (assignedTable != null && _mySeatIndex >= 0 && _mySeatIndex < assignedTable.seats.Length)
        {
            Transform seatPoint = assignedTable.seats[_mySeatIndex].point;
            if (seatPoint != null)
            {
                seatOrigin = seatPoint.position;
                origin = seatOrigin;
            }

            Transform leavePoint = assignedTable.seats[_mySeatIndex].leavePoint;
            if (leavePoint != null)
            {
                NavMeshHit leaveHit;
                if (NavMesh.SamplePosition(leavePoint.position, out leaveHit, 0.75f, NavMesh.AllAreas))
                {
                    return leaveHit.position;
                }
            }

            seatDirection = assignedTable.seats[_mySeatIndex].sitDirection;
        }

        Vector2 primaryDirection = Vector2.up;
        if (seatDirection != 0f)
        {
            primaryDirection = new Vector2(seatDirection, 0f);
        }
        else if (assignedTable != null)
        {
            Vector3 awayFromTable = origin - assignedTable.transform.position;
            if (awayFromTable.sqrMagnitude > 0.0001f)
            {
                primaryDirection = new Vector2(awayFromTable.x, awayFromTable.y).normalized;
            }
        }

        Vector2[] directions = new Vector2[]
        {
            primaryDirection,
            Rotate2D(primaryDirection, 35f),
            Rotate2D(primaryDirection, -35f),
            Rotate2D(primaryDirection, 70f),
            Rotate2D(primaryDirection, -70f),
            Rotate2D(primaryDirection, 110f),
            Rotate2D(primaryDirection, -110f),
            -primaryDirection,
            Vector2.right,
            Vector2.left,
            Vector2.up,
            Vector2.down
        };

        Vector2[] seatOffsets = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 0.12f),
            new Vector2(0f, -0.12f)
        };

        float[] distances = { 0.22f, 0.35f, 0.5f, 0.7f, 0.9f };
        for (int s = 0; s < seatOffsets.Length; s++)
        {
            Vector3 basePoint = seatOrigin + (Vector3)seatOffsets[s];
            for (int d = 0; d < distances.Length; d++)
            {
                Vector3 directCandidate = basePoint + (Vector3)primaryDirection.normalized * distances[d];
                NavMeshHit directHit;
                if (NavMesh.SamplePosition(directCandidate, out directHit, 0.9f, NavMesh.AllAreas))
                {
                    if (Vector3.Distance(directHit.position, origin) > 0.05f)
                    {
                        return directHit.position;
                    }
                }
            }
        }

        for (int d = 0; d < distances.Length; d++)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                Vector3 candidate = origin + (Vector3)(directions[i].normalized * distances[d]);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(candidate, out hit, 0.9f, NavMesh.AllAreas))
                {
                    if (Vector3.Distance(hit.position, origin) > 0.05f)
                    {
                        return hit.position;
                    }
                }
            }
        }

        NavMeshHit fallbackHit;
        if (NavMesh.SamplePosition(origin, out fallbackHit, 1.0f, NavMesh.AllAreas))
        {
            return fallbackHit.position;
        }

        return Vector3.zero;
    }

    private Vector2 Rotate2D(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    void ProcessPayment()
    {
        if (_paymentProcessed) return;
        _paymentProcessed = true;

        int baseRC = (wantedRecipe != null && wantedRecipe.dishResultSO != null) ? wantedRecipe.dishResultSO.basePrice : 100;
        float tip = (_dishStars * 10f) * profile.tipMultiplier * GetServiceTipMultiplier();
        PlayerData.AddCredit(Mathf.RoundToInt(baseRC + tip));

        Debug.Log($"Khach tra {baseRC + tip} RC va chuan bi Review.");

        SubmitSatisfactionReview();
    }

    void SubmitSatisfactionReview()
    {
        float patienceScore = (Mathf.Max(0, currentPatience) / 100f) * 10f;
        float dishScore = Mathf.Clamp(_dishStars * 2f, 0f, 10f);
        float specialScore = isSpecialRequestMet ? 10f : 0f;
        float hygieneScore = GetHygieneMoodScore();
        float seatComfortScore = GetSeatComfortScore();

        float satisfaction = 0f;

        switch (profile.type)
        {
            case CustomerType.Chill:
                satisfaction = (patienceScore * 0.20f) + (dishScore * 0.55f) + (hygieneScore * 0.15f) + (seatComfortScore * 0.10f);
                break;
            case CustomerType.RichAndRush:
                satisfaction = (patienceScore * 0.10f) + (dishScore * 0.55f) + (specialScore * 0.15f) + (hygieneScore * 0.10f) + (seatComfortScore * 0.10f);
                break;
            case CustomerType.FoodCritic:
                satisfaction = (patienceScore * 0.10f) + (dishScore * 0.55f) + (specialScore * 0.15f) + (hygieneScore * 0.15f) + (seatComfortScore * 0.05f);
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
        currentState = CustomerState.Leaving;
        _isPatienceActive = false;

        if (LevelManager.Instance != null && LevelManager.Instance.currentLedCustomer == this)
        {
            LevelManager.Instance.currentLedCustomer = null;
        }

        if (dirtPrefab != null && Random.value < 0.9f)
        {
            Instantiate(dirtPrefab, transform.position, Quaternion.identity);
            if (RestaurantRatingManager.Instance != null)
                RestaurantRatingManager.Instance.DecreaseHygiene(0.5f);
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
        }

        Vector3 standPoint = FindLeaveStandPoint();
        if (standPoint != Vector3.zero)
        {
            transform.position = standPoint;
        }

        if (CheckoutManager.Instance != null) CheckoutManager.Instance.LeaveQueue(this);
        if (assignedTable != null) assignedTable.seats[_mySeatIndex].isOccupied = false;

        if (_anim != null)
        {
            _anim.SetBool("IsSitting", false);
            _anim.SetBool("IsMoving", true);
            _anim.CrossFade("Movement", 0.05f);
        }

        Vector3 finalDest = ResolveLeaveDestination();
        SafeSetDestination(finalDest, 0f);
        Debug.Log($"CustomerAI: angry leave to SpawnPoint at {finalDest} | onNavMesh={_agent.isOnNavMesh} | pathStatus={_agent.pathStatus}");

        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh && _agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Vector3 escapePoint = FindEscapePointTowards(finalDest);
            if (escapePoint != Vector3.zero && Vector3.Distance(transform.position, escapePoint) > 0.05f)
            {
                StartCoroutine(LeaveThroughWaypoints(escapePoint, finalDest));
            }
            else
            {
                StartCoroutine(ForceMoveToSpawn(finalDest));
            }
        }
        else
        {
            StartCoroutine(MonitorLeaveProgress(finalDest));
        }

        Destroy(gameObject, 8f);
    }

    private void UpdateContextualReactions()
    {
        if (!_isPatienceActive || currentState == CustomerState.Leaving)
        {
            return;
        }

        _moodReactionCooldown -= Time.deltaTime;
        if (_moodReactionCooldown > 0f)
        {
            return;
        }

        bool waitingForService = currentState == CustomerState.Ordering || currentState == CustomerState.WaitingForFood || currentState == CustomerState.CheckingOut;
        if (!waitingForService)
        {
            return;
        }

        float patiencePressure = Mathf.Clamp01((60f - currentPatience) / 60f);
        float hygienePressure = Mathf.Clamp01((10f - PlayerData.hygieneScore) / 10f);
        float chance = (profile != null ? profile.randomReactionChance : 0.04f) + (patiencePressure * 0.18f) + (hygienePressure * 0.22f);

        if (assignedTable != null && assignedTable.isNearWindow)
        {
            chance *= 0.8f;
        }

        if (currentState == CustomerState.WaitingForFood && currentPatience < 50f && hasUpgrade_CallStaff && !_hasOrdered)
        {
            ShowEmote(emoteWave);
            _moodReactionCooldown = 2.25f;
            return;
        }

        // Nausea emote: while waiting for food and hygiene is low (<4), small chance to show nausea
        if (currentState == CustomerState.WaitingForFood && PlayerData.hygieneScore < 4f && emoteNauseous != null)
        {
            float nauseaChance = Random.Range(0.2f, 0.3f);
            if (Random.value <= nauseaChance)
            {
                float dur = Random.Range(1f, 2f);
                StartCoroutine(ShowTemporaryEmoteCoroutine(emoteNauseous, dur));
                _moodReactionCooldown = dur + 0.5f;
                return;
            }
        }

        if (Random.value <= chance)
        {
            if (hygienePressure > 0.5f)
            {
                ShowEmote(emoteAngry);
            }
            else if (currentState == CustomerState.CheckingOut)
            {
                ShowEmote(emotePay);
            }
            else if (_hasOrdered && currentState == CustomerState.WaitingForFood)
            {
                // preserve the order icon after ordering; skip showing wave
                _moodReactionCooldown = Random.Range(2.5f, 5.5f);
                return;
            }
            else
            {
                ShowEmote(emoteWave);
            }

            _moodReactionCooldown = Random.Range(2.5f, 5.5f);
        }
    }

    private float GetPatienceDecayRate()
    {
        float baseRate = profile != null ? profile.patienceDecayRate : 1f;
        float hygieneMultiplier = Mathf.Lerp(0.9f, 1.55f, Mathf.Clamp01((10f - PlayerData.hygieneScore) / 10f) * (profile != null ? profile.hygieneSensitivity : 1f));
        
        float seatMultiplier = 1f;
        if (assignedTable != null)
        {
            bool gotWindow = assignedTable.isNearWindow;
            if (wantsWindowSeat)
            {
                if (gotWindow)
                {
                    // Ngồi đúng bàn cửa sổ -> Giảm tốc độ mất kiên nhẫn
                    switch (profile?.type)
                    {
                        case CustomerType.Chill: seatMultiplier = 0.85f; break;
                        case CustomerType.RichAndRush: seatMultiplier = 0.6f; break; // VIP kiên nhẫn hơn hẳn (-40%)
                        case CustomerType.FoodCritic: seatMultiplier = 0.7f; break;   // Critic kiên nhẫn hơn (-30%)
                        default: seatMultiplier = 0.8f; break;
                    }
                }
                else
                {
                    // Muốn ngồi cửa sổ nhưng phải ngồi bàn thường -> Sốt ruột hơn
                    switch (profile?.type)
                    {
                        case CustomerType.Chill: seatMultiplier = 1.05f; break;
                        case CustomerType.RichAndRush: seatMultiplier = 1.25f; break; // VIP sốt ruột hơn (+25%)
                        case CustomerType.FoodCritic: seatMultiplier = 1.3f; break;    // Critic sốt ruột hơn (+30%)
                        default: seatMultiplier = 1.1f; break;
                    }
                }
            }
            else
            {
                seatMultiplier = gotWindow ? 0.9f : 1f;
            }
        }
        
        return baseRate * hygieneMultiplier * seatMultiplier;
    }

    private void ApplySeatComfortBonus()
    {
        if (assignedTable == null)
        {
            return;
        }

        if (wantsWindowSeat)
        {
            if (assignedTable.isNearWindow)
            {
                currentPatience = Mathf.Min(100f, currentPatience + 10f); // Cộng hẳn 10 điểm kiên nhẫn khi được ngồi đúng chỗ yêu thích
            }
            else
            {
                currentPatience = Mathf.Min(100f, currentPatience - 5f); // Trừ 5 điểm kiên nhẫn nếu bị thất vọng
            }
        }
        else
        {
            if (assignedTable.isNearWindow)
            {
                currentPatience = Mathf.Min(100f, currentPatience + 5f);
            }
            else
            {
                currentPatience = Mathf.Min(100f, currentPatience + 2f);
            }
        }
    }

    private float GetHygieneMoodScore()
    {
        float hygiene = Mathf.Clamp(PlayerData.hygieneScore, 0f, 10f);
        float seatBonus = assignedTable != null && assignedTable.isNearWindow ? 1.0f : 0f;
        return Mathf.Clamp(hygiene + seatBonus * (profile != null ? profile.windowSeatMoodBonus * 10f : 1f), 0f, 10f);
    }

    private float GetSeatComfortScore()
    {
        if (assignedTable == null)
        {
            return 5f;
        }

        bool gotWindow = assignedTable.isNearWindow;
        float seatScore = 5.5f;

        if (wantsWindowSeat)
        {
            if (gotWindow)
            {
                // Thỏa mãn nhu cầu ngồi cửa sổ
                switch (profile?.type)
                {
                    case CustomerType.Chill: seatScore = 8.5f; break;
                    case CustomerType.RichAndRush: seatScore = 9.5f; break; // VIP rất thích
                    case CustomerType.FoodCritic: seatScore = 10f; break;   // Critic cực thích
                    default: seatScore = 8.5f; break;
                }
            }
            else
            {
                // Có nhu cầu nhưng không được ngồi bàn cửa sổ -> bị thất vọng
                switch (profile?.type)
                {
                    case CustomerType.Chill: seatScore = 4.5f; break;
                    case CustomerType.RichAndRush: seatScore = 3.5f; break; // VIP thất vọng
                    case CustomerType.FoodCritic: seatScore = 2.0f; break;   // Critic trừ nặng điểm
                    default: seatScore = 4.0f; break;
                }
            }
        }
        else
        {
            // Không có nhu cầu cửa sổ, ngồi đâu cũng được nhưng ngồi cửa sổ vẫn cộng nhẹ
            seatScore = gotWindow ? 7.5f : 5.5f;
        }

        seatScore += Mathf.Clamp(_targetSitX, -1f, 1f) * 0.5f;
        return Mathf.Clamp(seatScore, 0f, 10f);
    }

    private float GetFoodSatisfactionPatienceBonus()
    {
        float hygieneFactor = Mathf.Clamp01(PlayerData.hygieneScore / 10f);
        float seatBonus = 1.5f;
        if (assignedTable != null)
        {
            bool gotWindow = assignedTable.isNearWindow;
            if (wantsWindowSeat)
            {
                seatBonus = gotWindow ? 5f : -2f; // Trừ kiên nhẫn nếu không thỏa mãn
            }
            else
            {
                seatBonus = gotWindow ? 3f : 1.5f;
            }
        }
        return 8f + (hygieneFactor * 4f) + seatBonus;
    }

    private float GetServiceTipMultiplier()
    {
        float multiplier = 1f;
        if (assignedTable != null)
        {
            bool gotWindow = assignedTable.isNearWindow;
            if (wantsWindowSeat)
            {
                if (gotWindow)
                {
                    // Thỏa mãn nhu cầu cửa sổ -> Cộng tip nhiều
                    switch (profile?.type)
                    {
                        case CustomerType.Chill: multiplier *= 1.15f; break;
                        case CustomerType.RichAndRush: multiplier *= 1.5f; break;  // VIP tip cực nhiều (+50%)
                        case CustomerType.FoodCritic: multiplier *= 1.2f; break;
                        default: multiplier *= 1.15f; break;
                    }
                }
                else
                {
                    // Muốn cửa sổ nhưng ngồi bàn thường -> Bớt tip
                    switch (profile?.type)
                    {
                        case CustomerType.Chill: multiplier *= 0.95f; break;
                        case CustomerType.RichAndRush: multiplier *= 0.8f; break; // VIP bớt tip (-20%)
                        case CustomerType.FoodCritic: multiplier *= 0.85f; break;
                        default: multiplier *= 0.9f; break;
                    }
                }
            }
            else
            {
                multiplier *= gotWindow ? 1.12f : 1f;
            }
        }

        multiplier *= Mathf.Lerp(0.9f, 1.15f, Mathf.Clamp01(PlayerData.hygieneScore / 10f));
        return multiplier;
    }

    public void ShowEmote(Sprite emoteSprite)
    {
        if (orderBubble != null && imgOrderIcon != null && emoteSprite != null)
        {
            orderBubble.SetActive(true);
            imgOrderIcon.sprite = emoteSprite;
        }
    }

    public IEnumerator ShowTemporaryEmoteCoroutine(Sprite emoteSprite, float duration)
    {
        if (orderBubble == null || imgOrderIcon == null || emoteSprite == null) yield break;

        Sprite previous = imgOrderIcon.sprite;
        orderBubble.SetActive(true);
        imgOrderIcon.sprite = emoteSprite;

        yield return new WaitForSeconds(duration);

        if (orderBubble == null || imgOrderIcon == null) yield break;

        if (wantedRecipe != null && wantedRecipe.dishIcon != null)
        {
            orderBubble.SetActive(true);
            imgOrderIcon.sprite = wantedRecipe.dishIcon;
        }
        else
        {
            imgOrderIcon.sprite = previous;
        }
    }

    public void TriggerPanic(string reason, Sprite customEmote = null)
    {
        if (currentState == CustomerState.CheckingOut || currentState == CustomerState.Leaving) return;

        Debug.Log($"Customer panicked and is leaving due to: {reason}");
        _dishStars = 0f;
        currentPatience = 0f;
        SubmitSatisfactionReview();

        if (customEmote != null)
        {
            ShowEmote(customEmote);
        }
        else
        {
            ShowEmote(emoteAngry);
        }

        StopAllCoroutines();
        OnLeave();
    }

    private void FollowPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            SafeSetDestination(player.transform.position, 1.5f);
        }
    }

    public void ReturnToQueue()
    {
        Transform spawnTrans = FindSpawnPointTransform();
        if (spawnTrans != null)
        {
            SafeSetDestination(spawnTrans.position, 0f);
        }
    }

    private void EnsureWindowSeatUI()
    {
        if (_windowSeatText != null) return;

        Transform statusCanvasTrans = transform.Find("StatusCanvas");
        if (statusCanvasTrans == null) return;

        GameObject txtGo = new GameObject("txt_WindowSeatDemand", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        txtGo.transform.SetParent(statusCanvasTrans, false);

        RectTransform rect = txtGo.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, 150f);
        rect.sizeDelta = new Vector2(500f, 80f);
        rect.localScale = Vector3.one;

        TMPro.TextMeshProUGUI tmp = txtGo.GetComponent<TMPro.TextMeshProUGUI>();
        tmp.font = GetFallbackFont();
        tmp.fontSize = 28f;
        tmp.color = new Color(0.2f, 0.9f, 1f, 1f); // Cyan
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.text = "Muốn ngồi gần cửa sổ";
        tmp.raycastTarget = false;

        tmp.outlineColor = Color.black;
        tmp.outlineWidth = 0.2f;

        _windowSeatText = tmp;
    }

    private TMPro.TMP_FontAsset GetFallbackFont()
    {
        if (TMPro.TMP_Settings.defaultFontAsset != null) return TMPro.TMP_Settings.defaultFontAsset;
        TMPro.TextMeshProUGUI anyText = FindObjectOfType<TMPro.TextMeshProUGUI>(true);
        if (anyText != null && anyText.font != null) return anyText.font;
        return null;
    }
    #endregion

    #region Visuals
    void UpdateVisuals()
    {
        if (patienceSlider != null)
        {
            patienceSlider.value = currentPatience / 100f;
        }

        if (patienceFillColor != null)
        {
            patienceFillColor.color = Color.Lerp(Color.red, Color.green, currentPatience / 100f);
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
