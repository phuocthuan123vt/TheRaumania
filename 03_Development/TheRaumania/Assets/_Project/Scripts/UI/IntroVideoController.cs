using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroVideoController : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoClip introClip;

    private VideoPlayer _player;
    private Canvas _overlayCanvas;
    private RenderTexture _rt;
    private GameObject _canvasRoot;
    private MonoBehaviour _overlayHost;
    private GameObject _root;
    private Image _backdropImage;
    private RawImage _rawImage;
    private Button _skipButton;
    private CanvasGroup _group;
    private Action _onFinished;
    private bool _isPlaying;
    private Coroutine _watchdogRoutine;
    private Coroutine _hideRoutine;
    private const float IntroFallbackTimeout = 12f;
    private const float IntroCoverHideDelay = 0.35f;

    public void PlayIntro(Action onFinished)
    {
        _onFinished = onFinished;
        EnsureUI();

        if (_isPlaying)
        {
            StopCurrentPlayback();
        }

        if (introClip == null)
        {
            Debug.LogWarning("IntroVideoController: introClip is not assigned. Skipping intro.");
            Finish();
            return;
        }

        _isPlaying = true;
        _root.SetActive(true);
        if (_backdropImage != null) _backdropImage.enabled = true;
        if (_rawImage != null) _rawImage.enabled = true;
        if (_skipButton != null) _skipButton.gameObject.SetActive(true);
        _group.alpha = 1f;
        _group.interactable = true;
        _group.blocksRaycasts = true;

        _player.Stop();
        _player.clip = introClip;
        _player.isLooping = false;
        _player.errorReceived -= OnVideoError;
        _player.errorReceived += OnVideoError;
        _player.prepareCompleted -= OnVideoPrepared;
        _player.prepareCompleted += OnVideoPrepared;
        _player.loopPointReached -= OnVideoFinished;
        _player.loopPointReached += OnVideoFinished;
        _player.Prepare();

        if (_watchdogRoutine != null) StopCoroutine(_watchdogRoutine);
        float timeout = IntroFallbackTimeout;
        if (introClip.length > 0d)
        {
            timeout = Mathf.Max(IntroFallbackTimeout, (float)introClip.length + 2f);
        }
        _watchdogRoutine = StartCoroutine(IntroWatchdog(timeout));
    }

    private void EnsureUI()
    {
        if (_root != null) return;

        // Create a dedicated top-most overlay canvas so intro is always visible.
        _canvasRoot = new GameObject("IntroVideoCanvas");
        _overlayCanvas = _canvasRoot.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.overrideSorting = true;
        _overlayCanvas.sortingOrder = 5000;

        var scaler = _canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        _canvasRoot.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(_canvasRoot);
        _overlayHost = _canvasRoot.AddComponent<IntroOverlayHost>();

        _root = new GameObject("IntroVideoOverlay");
        _root.transform.SetParent(_canvasRoot.transform, false);
        _root.SetActive(false);

        RectTransform rootRt = _root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        _group = _root.AddComponent<CanvasGroup>();

        // Dark backdrop to prevent the default Unity blue flash during scene transitions.
        GameObject backdropGo = new GameObject("Backdrop");
        backdropGo.transform.SetParent(_root.transform, false);
        _backdropImage = backdropGo.AddComponent<Image>();
        _backdropImage.color = Color.black;
        RectTransform backdropRt = _backdropImage.rectTransform;
        backdropRt.anchorMin = Vector2.zero;
        backdropRt.anchorMax = Vector2.one;
        backdropRt.offsetMin = Vector2.zero;
        backdropRt.offsetMax = Vector2.zero;

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

    private void OnVideoPrepared(VideoPlayer player)
    {
        if (!_isPlaying) return;

        player.Play();
        if (!player.isPlaying)
        {
            Debug.LogWarning("IntroVideoController: Video failed to start playback after prepare. Skipping intro.");
            Finish();
        }
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogWarning("IntroVideoController: Video error '" + message + "'. Skipping intro.");
        Finish();
    }

    private IEnumerator IntroWatchdog(float timeout)
    {
        yield return new WaitForSecondsRealtime(timeout);

        if (_isPlaying)
        {
            Debug.LogWarning("IntroVideoController: Intro timeout reached. Skipping intro to avoid stuck state.");
            Finish();
        }
    }

    private void StopCurrentPlayback()
    {
        if (_watchdogRoutine != null)
        {
            StopCoroutine(_watchdogRoutine);
            _watchdogRoutine = null;
        }

        if (_player != null)
        {
            _player.loopPointReached -= OnVideoFinished;
            _player.prepareCompleted -= OnVideoPrepared;
            _player.errorReceived -= OnVideoError;

            if (_player.isPlaying)
            {
                _player.Stop();
            }
        }
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
        StopCurrentPlayback();

        // Keep the black cover briefly to cover scene transition, then hide it.
        if (_rawImage != null) _rawImage.enabled = false;
        if (_skipButton != null) _skipButton.gameObject.SetActive(false);

        _isPlaying = false;
        var callback = _onFinished;
        _onFinished = null;
        callback?.Invoke();

        if (_hideRoutine != null && _overlayHost != null)
        {
            _overlayHost.StopCoroutine(_hideRoutine);
        }
        if (_overlayHost != null)
        {
            _hideRoutine = _overlayHost.StartCoroutine(HideCoverAfterDelay());
        }
    }

    private IEnumerator HideCoverAfterDelay()
    {
        yield return new WaitForSecondsRealtime(IntroCoverHideDelay);

        if (_root != null)
        {
            _root.SetActive(false);
        }

        if (_hideRoutine != null)
        {
            _hideRoutine = null;
        }
    }

    private sealed class IntroOverlayHost : MonoBehaviour
    {
    }
}