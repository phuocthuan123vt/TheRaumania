using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;
    [Header("Money UI")]
    public TextMeshProUGUI txtMoney;

    [Header("Rating UI")]
    public TextMeshProUGUI txtRating; // Kéo thả Text hiển thị sao vào đây
    public TextMeshProUGUI txtRatingBreakdown;
    public Slider sliderFood;
    public Slider sliderCleanliness;
    public Slider sliderDecor;
    public Slider sliderCustomerService;

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
    private float _displayedFoodScore = -1f;
    private float _displayedCleanlinessScore = -1f;
    private float _displayedDecorScore = -1f;
    private float _displayedCustomerServiceScore = -1f;
    private readonly Color _foodColor = new Color(1f, 0.58f, 0.12f, 1f);
    private readonly Color _cleanlinessColor = new Color(0.18f, 0.62f, 1f, 1f);
    private readonly Color _decorColor = new Color(0.68f, 0.36f, 1f, 1f);
    private readonly Color _customerServiceColor = new Color(0.22f, 0.82f, 0.35f, 1f);

    public int CurrentDay => _day;
    public int CurrentHour => _hour;
    public int CurrentMinute => _minute;
    public bool IsRestaurantOpen => _hour >= 7 && _hour < 22;

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

        RefreshRatingBreakdownUI();
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
        if (txtRatingBreakdown == null) txtRatingBreakdown = RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(root, "txt_RatingBreakdown", "txt_RatingDetail", "txt_RatingInfo");
        if (sliderFood == null) sliderFood = RuntimeReferenceFinder.FindDeepComponent<Slider>(root, "slider_food", "slider_Food", "food_slider", "FoodSlider");
        if (sliderCleanliness == null) sliderCleanliness = RuntimeReferenceFinder.FindDeepComponent<Slider>(root, "slider_cleanliness", "slider_Cleanliness", "cleanliness_slider", "CleanlinessSlider");
        if (sliderDecor == null) sliderDecor = RuntimeReferenceFinder.FindDeepComponent<Slider>(root, "slider_decor", "slider_Decor", "decor_slider", "DecorSlider");
        if (sliderCustomerService == null) sliderCustomerService = RuntimeReferenceFinder.FindDeepComponent<Slider>(root, "slider_customerservice", "slider_customerService", "slider_customer_service", "customer_service_slider", "CustomerServiceSlider");
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

    private void RefreshRatingBreakdownUI()
    {
        if (txtRatingBreakdown == null)
        {
            return;
        }

        if (RestaurantRatingManager.Instance != null)
        {
            txtRatingBreakdown.text = RestaurantRatingManager.Instance.GetScoreBreakdownText();
        }
        else
        {
            txtRatingBreakdown.text = "<b>Điểm nhà hàng</b>\nĐang chờ manager...";
        }
    }

    private void RefreshRatingSlidersUI()
    {
        if (RestaurantRatingManager.Instance == null)
        {
            return;
        }

        AnimateRatingSlider(sliderFood, ref _displayedFoodScore, PlayerData.foodQualityScore, _foodColor, Time.unscaledDeltaTime);
        AnimateRatingSlider(sliderCleanliness, ref _displayedCleanlinessScore, PlayerData.hygieneScore, _cleanlinessColor, Time.unscaledDeltaTime);
        AnimateRatingSlider(sliderDecor, ref _displayedDecorScore, RestaurantRatingManager.Instance.GetDecorationScore(), _decorColor, Time.unscaledDeltaTime);
        AnimateRatingSlider(sliderCustomerService, ref _displayedCustomerServiceScore, RestaurantRatingManager.Instance.GetAttitudeScore(), _customerServiceColor, Time.unscaledDeltaTime);
    }

    private void AnimateRatingSlider(Slider slider, ref float displayedScore, float targetScore, Color targetColor, float deltaTime)
    {
        if (slider == null)
        {
            return;
        }

        float normalizedTargetScore = Mathf.Clamp(targetScore, 0f, 10f);
        if (displayedScore < 0f)
        {
            displayedScore = normalizedTargetScore;
        }

        displayedScore = Mathf.MoveTowards(displayedScore, normalizedTargetScore, deltaTime * 8f);
        float normalizedScore = Mathf.Clamp01(displayedScore / 10f);
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;
        slider.SetValueWithoutNotify(normalizedScore);

        Color lowColor = Color.red;
        Color activeColor = Color.Lerp(lowColor, targetColor, normalizedScore);
        Color trackColor = Color.Lerp(new Color(0.28f, 0.08f, 0.08f, 0.55f), activeColor, 0.85f);

        if (slider.targetGraphic != null)
        {
            slider.targetGraphic.color = Color.Lerp(slider.targetGraphic.color, trackColor, deltaTime * 10f);
        }

        if (slider.fillRect != null)
        {
            Image fillImage = slider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(fillImage.color, activeColor, deltaTime * 10f);
            }
        }

        if (slider.handleRect != null)
        {
            Image handleImage = slider.handleRect.GetComponent<Image>();
            if (handleImage != null)
            {
                handleImage.color = Color.Lerp(handleImage.color, Color.Lerp(activeColor, Color.white, 0.2f), deltaTime * 10f);
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
        RefreshRatingSlidersUI();
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

        RefreshRatingSlidersUI();
    }
}
