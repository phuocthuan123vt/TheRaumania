using UnityEngine;
using UnityEngine.UI;

public class SlicingMinigame : MinigameBase
{
    public Slider slider;
    public RectTransform imgTargetZone;
    public float baseSpeed = 2f;
    public float tolerance = 0.05f;

    private float _targetValue;
    private int _totalSlices;
    private int _currentSliceCount;
    private float _scoreSum;
    private bool _isMoving;

    public override void StartGame(float freshness)
    {
        minigamePanel.SetActive(true);
        _totalSlices = Random.Range(4, 7); // Thái ngẫu nhiên 4 đến 6 lần
        _currentSliceCount = 0;
        _scoreSum = 0;

        StartNewSlice(freshness);
    }

    void StartNewSlice(float freshness)
    {
        float speedMult = (freshness < 50) ? 1.5f : 1f;
        _targetValue = Random.Range(0.1f, 0.9f);

        // Cập nhật vị trí vùng xanh
        float sliderWidth = slider.GetComponent<RectTransform>().rect.width;
        imgTargetZone.anchoredPosition = new Vector2((_targetValue - 0.5f) * sliderWidth, 0);

        _isMoving = true;
    }

    void Update()
    {
        if (!_isMoving) return;

        slider.value = Mathf.PingPong(Time.time * baseSpeed, 1f);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isMoving = false;
            float diff = Mathf.Abs(slider.value - _targetValue);
            float currentScore = (diff <= tolerance) ? 100 : (diff <= tolerance * 3f) ? 50 : 10;

            _scoreSum += currentScore;
            _currentSliceCount++;

            if (_currentSliceCount < _totalSlices)
            {
                StartNewSlice(30); // Tiếp tục nhịp tiếp theo
            }
            else
            {
                Complete(_scoreSum / _totalSlices); // Trả về điểm trung bình
            }
        }
    }
}