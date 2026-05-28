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

    [Header("Du lieu & Trang thai")]
    public CustomerProfileSO profile;
    public CustomerState currentState;
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

    public int _mySeatIndex;

    public bool isSpecialRequestMet = false;
    public GameObject dirtPrefab;

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

    private void Start()
    {
        currentState = CustomerState.Queueing;
        ConstructBehaviorTree();
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
        UpdateVisuals();

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

        currentState = CustomerState.WalkingToTable;
        _agent.enabled = true;
        _agent.SetDestination(assignedTable.seats[_mySeatIndex].point.position);
    }

    void OnArrivedAtTable()
    {
        currentState = CustomerState.Ordering;
        _agent.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

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
            currentPatience = 100f;

            if (emoteEat != null) ShowEmote(emoteEat);
            Debug.Log($"Khach nhan mon: {dish.itemName} ({stars:F1} sao)");

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
        StartLeaveFromTable();
    }

    private void StartLeaveFromTable()
    {
        if (currentState == CustomerState.Leaving) return;

        currentState = CustomerState.Leaving;
        _isPatienceActive = false;

        if (_anim != null)
        {
            _anim.SetBool("IsSitting", false);
            _anim.SetBool("IsMoving", true);
            _anim.CrossFade("Movement", 0.05f);
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }

        _agent.enabled = true;
        _agent.isStopped = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        Vector3 standPoint = FindLeaveStandPoint();
        if (standPoint != Vector3.zero)
        {
            transform.position = standPoint;
        }

        Vector3 finalDest = ResolveLeaveDestination();

        if (!_agent.isOnNavMesh)
        {
            NavMeshHit agentHit;
            if (NavMesh.SamplePosition(transform.position, out agentHit, 3.0f, NavMesh.AllAreas))
            {
                _agent.Warp(agentHit.position);
            }
        }

        NavMeshHit destHit;
        if (NavMesh.SamplePosition(finalDest, out destHit, 3.0f, NavMesh.AllAreas))
        {
            finalDest = destHit.position;
        }

        _agent.SetDestination(finalDest);
        Debug.Log($"CustomerAI: leaving to SpawnPoint at {finalDest} | onNavMesh={_agent.isOnNavMesh} | pathStatus={_agent.pathStatus}");

        if (_agent.pathStatus != NavMeshPathStatus.PathComplete)
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
        float tip = (_dishStars * 10f) * profile.tipMultiplier;
        PlayerData.AddCredit(Mathf.RoundToInt(baseRC + tip));

        Debug.Log($"Khach tra {baseRC + tip} RC va chuan bi Review.");

        SubmitSatisfactionReview();
    }

    void SubmitSatisfactionReview()
    {
        float patienceScore = (Mathf.Max(0, currentPatience) / 100f) * 10f;
        float dishScore = Mathf.Clamp(_dishStars * 2f, 0f, 10f);
        float specialScore = isSpecialRequestMet ? 10f : 0f;

        float satisfaction = 0f;

        switch (profile.type)
        {
            case CustomerType.Chill:
                satisfaction = (patienceScore * 0.25f) + (dishScore * 0.75f);
                break;
            case CustomerType.RichAndRush:
                satisfaction = (patienceScore * 0.15f) + (dishScore * 0.65f) + (specialScore * 0.20f);
                break;
            case CustomerType.FoodCritic:
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
        currentState = CustomerState.Leaving;
        _isPatienceActive = false;

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

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }

        _agent.enabled = true;
        _agent.isStopped = false;

        Vector3 finalDest = ResolveLeaveDestination();

        if (_agent.isOnNavMesh)
        {
            _agent.ResetPath();
        }

        _agent.SetDestination(finalDest);
        Debug.Log($"CustomerAI: angry leave to SpawnPoint at {finalDest} | onNavMesh={_agent.isOnNavMesh} | pathStatus={_agent.pathStatus}");

        if (_agent.pathStatus != NavMeshPathStatus.PathComplete)
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
