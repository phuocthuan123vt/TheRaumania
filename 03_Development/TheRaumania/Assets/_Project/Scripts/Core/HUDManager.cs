using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("Money UI")]
    public TextMeshProUGUI txtMoney;

    [Header("Time UI")]
    public TextMeshProUGUI txtTime;
    public TextMeshProUGUI txtDay;

    [Header("Time Settings")]
    public float timeSpeed = 1f; // 1 giây thật = 1 phút game
    private float _minute, _hour;
    private int _day = 1;

    private void Awake() { Instance = this; }

    void Update()
    {
        UpdateClock();
        UpdateMoney();
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

        // Định dạng hiển thị: 08:30 AM/PM
        string ampm = _hour >= 12 ? "PM" : "AM";
        float displayHour = _hour % 12;
        if (displayHour == 0) displayHour = 12;

        txtTime.text = string.Format("{0:00}:{1:00} {2}", displayHour, _minute, ampm);
        txtDay.text = "Ngày " + _day;
    }

    void UpdateMoney()
    {
        // Luôn cập nhật theo số tiền trong PlayerData
        txtMoney.text = PlayerData.rCredit.ToString("N0") + " RC";
    }
}