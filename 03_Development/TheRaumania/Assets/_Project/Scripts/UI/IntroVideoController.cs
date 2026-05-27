using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class IntroVideoController : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoClip introClip;

    private VideoPlayer _player;
    private RenderTexture _rt;
    private GameObject _root;
    private RawImage _rawImage;
    private Button _skipButton;
    private CanvasGroup _group;
    private Action _onFinished;
    private bool _isPlaying;

    public void PlayIntro(Action onFinished)
    {
        _onFinished = onFinished;
        EnsureUI();

        if (introClip == null)
        {
            Debug.LogWarning("IntroVideoController: introClip is not assigned. Skipping intro.");
            Finish();
            return;
        }

        _isPlaying = true;
        _root.SetActive(true);
        _group.alpha = 1f;
        _group.interactable = true;
        _group.blocksRaycasts = true;

        _player.clip = introClip;
        _player.isLooping = false;
        _player.loopPointReached -= OnVideoFinished;
        _player.loopPointReached += OnVideoFinished;
        _player.Play();
    }

    private void EnsureUI()
    {
        if (_root != null) return;

        Canvas canvas = FindObjectOfType<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("IntroVideoCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        _root = new GameObject("IntroVideoOverlay");
        _root.transform.SetParent(canvas.transform, false);
        _root.SetActive(false);

        RectTransform rootRt = _root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        _group = _root.AddComponent<CanvasGroup>();

        // RawImage for video
        GameObject imgGo = new GameObject("IntroVideo");
        imgGo.transform.SetParent(_root.transform, false);
        _rawImage = imgGo.AddComponent<RawImage>();
        RectTransform imgRt = _rawImage.rectTransform;
        imgRt.anchorMin = Vector2.zero;
        imgRt.anchorMax = Vector2.one;
        imgRt.offsetMin = Vector2.zero;
        imgRt.offsetMax = Vector2.zero;

        // VideoPlayer
        _player = _root.AddComponent<VideoPlayer>();
        _player.playOnAwake = false;
        _player.renderMode = VideoRenderMode.RenderTexture;

        _rt = new RenderTexture(Mathf.Max(16, Screen.width), Mathf.Max(16, Screen.height), 0);
        _player.targetTexture = _rt;
        _rawImage.texture = _rt;

        // Skip button (bottom-right)
        GameObject btnGo = new GameObject("btn_skip");
        btnGo.transform.SetParent(_root.transform, false);
        Image btnImage = btnGo.AddComponent<Image>();
        btnImage.color = new Color(0f, 0f, 0f, 0.25f);
        _skipButton = btnGo.AddComponent<Button>();
        _skipButton.onClick.AddListener(Skip);

        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 0f);
        btnRt.anchorMax = new Vector2(1f, 0f);
        btnRt.pivot = new Vector2(1f, 0f);
        btnRt.sizeDelta = new Vector2(120f, 40f);
        btnRt.anchoredPosition = new Vector2(-20f, 20f);

        GameObject txtGo = new GameObject("txt_skip");
        txtGo.transform.SetParent(btnGo.transform, false);
        TextMeshProUGUI txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = "Skip";
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 22f;
        txt.color = Color.white;

        RectTransform txtRt = txt.rectTransform;
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        Finish();
    }

    private void Skip()
    {
        if (_player != null && _player.isPlaying)
        {
            _player.Stop();
        }
        Finish();
    }

    private void Finish()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
        _isPlaying = false;
        _onFinished?.Invoke();
    }
}