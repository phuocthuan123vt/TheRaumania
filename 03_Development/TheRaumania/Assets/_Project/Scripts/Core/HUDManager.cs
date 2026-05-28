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
    public RectTransform imgClockHand;
    [Header("Time Settings")]
    public float timeSpeed = 1f;
    [SerializeField] private int _day = 1;
    [SerializeField] private int _hour = 5;
    [SerializeField] private int _minute = 0;
    private float _minuteAccumulator;

    public int CurrentDay => _day;
    public int CurrentHour => _hour;
    public int CurrentMinute => _minute;
    public bool IsRestaurantOpen => _hour >= 9 && _hour < 22;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoMapUI();
        RefreshTimeUI();
    }

    void Update()
    {
        TickClock(Time.deltaTime * timeSpeed);
    }
    void OnRatingChangedHandler(float stars)
    {
        if (txtRating != null)
        {
            txtRating.text = string.Format("{0:F1}", stars);
        }
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
        txtMoney.text = FormatRC(currentCredit);
    }

    public void ApplyTimeState(int day, int hour, int minute)
    {
        _day = Mathf.Max(1, day);
        _hour = Mathf.Clamp(hour, 0, 23);
        _minute = Mathf.Clamp(minute, 0, 59);
        _minuteAccumulator = 0f;
        RefreshTimeUI();
    }

    public void SkipToNextDayMorning()
    {
        ApplyTimeState(_day + 1, 5, 0);
    }

    private void TickClock(float minuteDelta)
    {
        _minuteAccumulator += minuteDelta;

        while (_minuteAccumulator >= 1f)
        {
            _minuteAccumulator -= 1f;
            _minute++;

            if (_minute >= 60)
            {
                _minute = 0;
                _hour++;

                if (WarehouseManager.Instance != null)
                {
                    WarehouseManager.Instance.OnHourPassed();
                }

                if (_hour >= 24)
                {
                    _hour = 0;
                    _day++;
                }
            }
        }

        RefreshTimeUI();
    }

    private void AutoMapUI()
    {
        Transform root = transform.root;

        if (txtMoney == null) txtMoney = RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(root, "txt_Money");
        if (txtRating == null) txtRating = RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(root, "txt_Rating");
        if (txtTime == null) txtTime = RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(root, "txt_Time");
        if (txtDay == null) txtDay = RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(root, "txt_Date");

        if (imgClockHand == null)
        {
            Transform found = RuntimeReferenceFinder.FindDeepTransform(root, "img_ClockHand");
            if (found != null)
            {
                imgClockHand = found as RectTransform;
            }
        }
    }

    private string FormatRC(int amount)
    {
        int clampedAmount = Mathf.Clamp(amount, 0, 999999);
        string raw = clampedAmount.ToString("D6");
        return string.Join(" ", raw.ToCharArray());
    }

    private void UpdateClockHandRotation()
    {
        if (imgClockHand == null)
        {
            return;
        }

        float totalMinutes = (_hour * 60f) + _minute;
        float normalized = Mathf.Repeat(totalMinutes, 720f) / 720f;
        float zRotation = -normalized * 360f;
        imgClockHand.localEulerAngles = new Vector3(0f, 0f, zRotation);
    }

    private void RefreshTimeUI()
    {
        if (txtTime != null)
        {
            string ampm = _hour >= 12 ? "PM" : "AM";
            int displayHour = _hour % 12;
            if (displayHour == 0) displayHour = 12;
            txtTime.text = string.Format("{0:00}:{1:00} {2}", displayHour, _minute, ampm);
        }

        if (txtDay != null)
        {
            txtDay.text = "Ngày " + _day;
        }

        UpdateClockHandRotation();
    }

    private void LateUpdate()
    {
        RefreshTimeUI();
    }

    private void Start()
    {
        AutoMapUI();
        UpdateMoney(PlayerData.RCredit);
        RefreshTimeUI();
        // Initialize rating display from manager if available
        if (RestaurantRatingManager.Instance != null)
        {
            OnRatingChangedHandler(RestaurantRatingManager.Instance.GetRestaurantStars());
        }
    }
}
