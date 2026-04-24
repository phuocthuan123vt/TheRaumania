using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class SlicingMinigame : MinigameBase
{
    [Header("UI References")]
    public Slider slider;
    public RectTransform imgTargetZone;

    [Header("Settings")]
    public float baseSpeed = 2.5f;
    public float tolerance = 0.06f; 

    private float _targetValue;
    private int _totalSlices;
    private int _currentSliceCount;
    private List<float> _sliceScores = new List<float>();
    private bool _isMoving;

    public override void StartGame(float freshness)
    {
        minigamePanel.SetActive(true);
        _totalSlices = Random.Range(4, 7); 
        _currentSliceCount = 0;
        _sliceScores.Clear();

        float speedMult = (freshness < 50) ? 0.8f : 0.25f;
        baseSpeed *= speedMult;

        StartNextSlice();
    }

    void StartNextSlice()
    {
        _targetValue = Random.Range(0.2f, 0.8f);

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

            float score = 0;
            if (diff <= tolerance) score = 100;
            else if (diff <= tolerance * 2.5f) score = 60;
            else score = 10;

            _sliceScores.Add(score);
            _currentSliceCount++;

            if (_currentSliceCount < _totalSlices)
                Invoke(nameof(StartNextSlice), 0.2f);
            else
                Complete(_sliceScores.Average());
        }
    }
}