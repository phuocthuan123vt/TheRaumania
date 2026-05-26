using UnityEngine;
using TMPro;
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;
    [Header("Money UI")]
    public TextMeshProUGUI txtMoney;

    [Header("Rating UI")]
    public TextMeshProUGUI txtRating; // Kéo thả Text hiển thị sao vào đây

    [Header("Time UI")]
    public TextMeshProUGUI txtTime;
    public TextMeshProUGUI txtDay;
    [Header("Time Settings")]
    public float timeSpeed = 1f;
    private float _minute, _hour;
    private int _day = 1;
    private void Awake() { Instance = this; }

    void Update()
    {
        UpdateClock();
    }
    void OnRatingChangedHandler(float stars)
    {
        if (txtRating != null)
        {
            txtRating.text = string.Format("{0:F1}", stars);
        }
    }

    void UpdateClock()
    {
        _minute += Time.deltaTime * timeSpeed;
        if (_minute >= 60)
        {
            _minute = 0;
            _hour++;
            WarehouseManager.Instance.OnHourPassed();
        }
        if (_hour >= 24)
        {
            _hour = 0;
            _day++;
        }
        string ampm = _hour >= 12 ? "PM" : "AM";
        float displayHour = _hour % 12;
        if (displayHour == 0) displayHour = 12;
        txtTime.text = string.Format("{0:00}:{1:00} {2}", displayHour, _minute, ampm);
        txtDay.text = "Ngày " + _day;
    }

    private void OnEnable()
    {
        PlayerData.OnCreditChanged += UpdateMoney;
        RestaurantRatingManager.OnRatingChanged += OnRatingChangedHandler;
    }

    private void OnDisable()
    {
        PlayerData.OnCreditChanged -= UpdateMoney;
        RestaurantRatingManager.OnRatingChanged -= OnRatingChangedHandler;
    }

    void UpdateMoney(int currentCredit)
    {
        txtMoney.text = currentCredit.ToString("N0") + " RC";
    }

    // Call once at start to initialize UI
    private void Start()
    {
        UpdateMoney(PlayerData.RCredit);
        // Initialize rating display from manager if available
        if (RestaurantRatingManager.Instance != null)
        {
            OnRatingChangedHandler(RestaurantRatingManager.Instance.GetRestaurantStars());
        }
    }
}
