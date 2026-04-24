using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FryingMinigame : MinigameBase
{
    [Header("UI References")]
    public Slider tempSlider;
    public RectTransform imgTargetZone;
    public Image fillImage;

    [Header("Physics Settings")]
    public float gravity = 0.6f;        // Tốc độ rơi
    public float liftForce = 0.9f;      // Lực bay lên khi giữ chuột
    public float zoneSize = 0.2f;      // Độ rộng vùng xanh (18% thanh)

    [Header("Target AI Movement")]
    public float smoothTime = 0.8f; 
    public float changeInterval = 2.5f;  // Thời gian đổi mục tiêu

    private float _targetCenter;        // Vị trí mục tiêu (0-1)
    private float _currentVisualPos;    // Vị trí hiện tại của vùng xanh
    private float _yVelocity = 0f;         
    private float _timer;
    private bool _isActive;
    private float _nextChangeTime;
    private List<float> _cookSamples = new List<float>();
    private float _sampleTimer;

    public override void StartGame(float freshness)
    {
        minigamePanel.SetActive(true);
        _timer = 5f; // Cần giữ 5 giây thành công
        _isActive = true;
        _sampleTimer = 0;
        _cookSamples.Clear();

        tempSlider.value = 0.2f;
        _targetCenter = 0.5f;
        _currentVisualPos = 0.5f;
        UpdateTargetVisual(0.5f);
    }

    void Update()
    {
        if (!_isActive) return;
        if (Time.time > _nextChangeTime)
        {
            _targetCenter = Random.Range(0.25f, 0.75f);
            _nextChangeTime = Time.time + Random.Range(1.5f, changeInterval);
        }

        _currentVisualPos = Mathf.SmoothDamp(_currentVisualPos, _targetCenter, ref _yVelocity, smoothTime);
        UpdateTargetVisual(_currentVisualPos);

        // 2. NGƯỜI CHƠI ĐIỀU KHIỂN
        if (Input.GetMouseButton(0))
            tempSlider.value += liftForce * Time.deltaTime;
        else
            tempSlider.value -= gravity * Time.deltaTime;

        // 3. QUÉT ĐIỂM MỖI 0.1 GIÂY
        _sampleTimer += Time.deltaTime;
        if (_sampleTimer >= 0.1f)
        {
            CheckSuccess();
            _sampleTimer = 0;
        }
    }

    void UpdateTargetVisual(float yCenter)
    {
        float min = Mathf.Clamp01(yCenter - (zoneSize / 2));
        float max = Mathf.Clamp01(yCenter + (zoneSize / 2));

        // ÉP CHUẨN ANCHOR ĐỂ KHÔNG BỊ MÓP HÌNH
        imgTargetZone.anchorMin = new Vector2(0.5f, min);
        imgTargetZone.anchorMax = new Vector2(0.5f, max);

        // Reset offsets để hình ảnh khít vào anchor
        imgTargetZone.offsetMin = new Vector2(-imgTargetZone.rect.width / 2, 0);
        imgTargetZone.offsetMax = new Vector2(imgTargetZone.rect.width / 2, 0);
    }

    void CheckSuccess()
    {
        float minSafe = imgTargetZone.anchorMin.y;
        float maxSafe = imgTargetZone.anchorMax.y;

        if (tempSlider.value >= minSafe && tempSlider.value <= maxSafe)
        {
            _cookSamples.Add(1.0f);
            fillImage.color = Color.green;
            _timer -= 0.1f;
            if (_timer <= 0) { _isActive = false; FinishGame(); }
        }
        else
        {
            _cookSamples.Add(0.0f);
            fillImage.color = Color.red;
        }
    }

    void FinishGame()
    {
        float sum = 0;
        foreach (var s in _cookSamples) sum += s;
        float finalM = (sum / _cookSamples.Count) * 100f;
        Complete(finalM);
    }
}