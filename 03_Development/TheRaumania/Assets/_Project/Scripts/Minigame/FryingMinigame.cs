using UnityEngine;
using UnityEngine.UI;

public class FryingMinigame : MinigameBase
{
    public Slider tempSlider;
    public RectTransform imgTargetZone;
    public Image fillImage;

    public float coolingRate = 0.4f;
    public float heatForce = 0.15f;
    public float zoneSize = 0.15f; // Độ rộng vùng xanh (15% thanh slider)

    private float _targetCenter;
    private float _timer;
    private bool _isActive;

    public override void StartGame(float freshness)
    {
        minigamePanel.SetActive(true);
        _timer = 5f;
        _isActive = true;
        tempSlider.value = 0.2f;

        // 1. Ngẫu nhiên tâm vùng xanh từ 0.2 đến 0.8
        _targetCenter = Random.Range(0.2f, 0.8f);

        // 2. CẬP NHẬT VỊ TRÍ CHI TIẾT (Fix lỗi hiển thị)
        // Ta dùng Anchor để vùng xanh luôn khớp với giá trị Slider
        imgTargetZone.anchorMin = new Vector2(0, _targetCenter - (zoneSize / 2));
        imgTargetZone.anchorMax = new Vector2(1, _targetCenter + (zoneSize / 2));
        // Reset các offset về 0 để anchor có tác dụng
        imgTargetZone.offsetMin = Vector2.zero;
        imgTargetZone.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (!_isActive) return;

        tempSlider.value -= coolingRate * Time.deltaTime;
        if (Input.GetMouseButtonDown(0)) tempSlider.value += heatForce;

        // 3. LOGIC KIỂM TRA (Khớp hoàn toàn với Visual)
        float minZone = _targetCenter - (zoneSize / 2);
        float maxZone = _targetCenter + (zoneSize / 2);

        if (tempSlider.value >= minZone && tempSlider.value <= maxZone)
        {
            fillImage.color = Color.green;
            _timer -= Time.deltaTime;
            if (_timer <= 0) Complete(100);
        }
        else
        {
            fillImage.color = Color.red;
        }
    }
}