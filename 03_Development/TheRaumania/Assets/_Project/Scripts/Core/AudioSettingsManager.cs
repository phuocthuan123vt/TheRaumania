using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance;

    private const string MusicVolumeKey = "TheRaumania_MusicVolume";
    private const string GameVolumeKey = "TheRaumania_GameVolume";

    private GameObject _canvasRoot;
    private GameObject _panel;
    private GameObject _scenePanel;
    private Slider _musicSlider;
    private Slider _gameSlider;
    private TextMeshProUGUI _musicValueText;
    private TextMeshProUGUI _gameValueText;
    private Coroutine _applyRoutine;
    private readonly Dictionary<int, float> _musicBaseVolumes = new Dictionary<int, float>();

    public float MusicVolume { get; private set; } = 1f;
    public float GameVolume { get; private set; } = 1f;

    public static AudioSettingsManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameObject go = new GameObject("AudioSettingsManager");
        Instance = go.AddComponent<AudioSettingsManager>();
        DontDestroyOnLoad(go);
        return Instance;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPrefs();
            EnsureUI();
            ApplyVolumes();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindScenePanel();

        if (_applyRoutine != null)
        {
            StopCoroutine(_applyRoutine);
        }

        _applyRoutine = StartCoroutine(ApplyAfterSceneLoad());
    }

    public void OpenSettings()
    {
        EnsureUI();
        SyncSlidersToValues();
        GetActivePanel().SetActive(true);
    }

    public void CloseSettings()
    {
        var panel = GetActivePanel();
        if (panel != null) panel.SetActive(false);
    }

    public void ToggleSettings()
    {
        EnsureUI();
        var panel = GetActivePanel();
        if (panel != null && panel.activeSelf) CloseSettings();
        else OpenSettings();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        SavePrefs();
        ApplyVolumes();
        RefreshValueTexts();
    }

    public void SetGameVolume(float value)
    {
        GameVolume = Mathf.Clamp01(value);
        SavePrefs();
        ApplyVolumes();
        RefreshValueTexts();
    }

    private void LoadPrefs()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        GameVolume = PlayerPrefs.GetFloat(GameVolumeKey, 1f);
    }

    private void SavePrefs()
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.SetFloat(GameVolumeKey, GameVolume);
        PlayerPrefs.Save();
    }

    private void EnsureUI()
    {
        if (TryBindScenePanel()) return;

        if (_panel != null) return;

        _canvasRoot = new GameObject("AudioSettingsCanvas");
        DontDestroyOnLoad(_canvasRoot);
        var canvas = _canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 6000;
        _canvasRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasRoot.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("pnl_AudioSettings");
        _panel.transform.SetParent(_canvasRoot.transform, false);
        _panel.SetActive(false);

        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = new Color(0.18f, 0.11f, 0.07f, 0.96f);

        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 420f);

        CreateTitle("Settings", new Vector2(0f, 150f));
        _musicSlider = CreateSliderRow("Music Volume", new Vector2(0f, 55f), SetMusicVolume);
        _gameSlider = CreateSliderRow("Game Volume", new Vector2(0f, -35f), SetGameVolume);
        CreateCloseButton(new Vector2(0f, -145f));

        SyncSlidersToValues();
        RefreshValueTexts();
    }

    private void CreateTitle(string text, Vector2 anchoredPosition)
    {
        GameObject titleGo = new GameObject("txt_Title");
        titleGo.transform.SetParent(_panel.transform, false);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = text;
        title.alignment = TextAlignmentOptions.Center;
        title.fontSize = 40f;
        title.color = Color.white;
        title.raycastTarget = false;

        var rect = title.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(640f, 60f);
        rect.anchoredPosition = anchoredPosition;
    }

    private bool TryBindScenePanel()
    {
        GameObject scenePanel = FindSceneSettingsPanel();
        if (scenePanel == null)
        {
            return false;
        }

        if (_scenePanel == scenePanel && _musicSlider != null && _gameSlider != null)
        {
            return true;
        }

        _scenePanel = scenePanel;
        BindExistingPanel(_scenePanel);
        return true;
    }

    private GameObject FindSceneSettingsPanel()
    {
        var roots = Object.FindObjectsOfType<GameObject>(true);
        foreach (var go in roots)
        {
            if (go == null) continue;
            if (go.name == "Pnl_Settings" || go.name == "pnl_Settings" || go.name == "Pnl_Settings(Clone)" || go.name == "pnl_Settings(Clone)")
            {
                return go;
            }
        }

        return null;
    }

    private void BindExistingPanel(GameObject panel)
    {
        if (panel == null) return;

        _panel = panel;

        _musicSlider = FindSlider(panel.transform, "slider_BGMVolume", "slider_BgmVolume", "BGM_Volume", "BGM", "Slider");
        _gameSlider = FindSlider(panel.transform, "slider_VFXVolume", "slider_VfxVolume", "VFX_Volume", "VFX", "Slider");

        _musicValueText = FindValueText(panel.transform, "txt_BGMVolume", "txt_BgmVolume", "BGM_Value", "Value_BGM", "Text", "Value");
        _gameValueText = FindValueText(panel.transform, "txt_VFXVolume", "txt_VfxVolume", "VFX_Value", "Value_VFX", "Text", "Value");

        WireSlider(_musicSlider, SetMusicVolume);
        WireSlider(_gameSlider, SetGameVolume);

        var closeButton = FindButton(panel.transform, "btn_close", "btn_Close", "X", "Close");
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseSettings);
            var label = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.raycastTarget = false;
        }

        SyncSlidersToValues();
        RefreshValueTexts();
    }

    private Slider FindSlider(Transform root, params string[] names)
    {
        return RuntimeReferenceFinder.FindDeepComponent<Slider>(root, names);
    }

    private TextMeshProUGUI FindValueText(Transform root, params string[] names)
    {
        return RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(root, names);
    }

    private Button FindButton(Transform root, params string[] names)
    {
        return RuntimeReferenceFinder.FindDeepComponent<Button>(root, names);
    }

    private void WireSlider(Slider slider, System.Action<float> handler)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(value => handler(value));
    }

    private Slider CreateSliderRow(string labelText, Vector2 rowPosition, System.Action<float> onValueChanged)
    {
        GameObject row = new GameObject(labelText.Replace(" ", "_") + "_Row");
        row.transform.SetParent(_panel.transform, false);

        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(640f, 90f);
        rowRect.anchoredPosition = rowPosition;

        GameObject labelGo = new GameObject(labelText + "_Label");
        labelGo.transform.SetParent(row.transform, false);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.alignment = TextAlignmentOptions.Left;
        label.fontSize = 26f;
        label.color = Color.white;
        label.raycastTarget = false;

        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(240f, 40f);
        labelRect.anchoredPosition = new Vector2(0f, 20f);

        GameObject valueGo = new GameObject(labelText + "_Value");
        valueGo.transform.SetParent(row.transform, false);
        var valueText = valueGo.AddComponent<TextMeshProUGUI>();
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.fontSize = 24f;
        valueText.color = new Color(1f, 0.92f, 0.72f, 1f);
        valueText.raycastTarget = false;

        var valueRect = valueText.rectTransform;
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.sizeDelta = new Vector2(100f, 30f);
        valueRect.anchoredPosition = new Vector2(-5f, 20f);

        GameObject sliderGo = new GameObject(labelText + "_Slider");
        sliderGo.transform.SetParent(row.transform, false);
        var slider = sliderGo.AddComponent<Slider>();
        var sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(0f, 24f);
        sliderRect.anchoredPosition = new Vector2(0f, -10f);

        var background = sliderGo.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.18f);
        background.raycastTarget = true;
        slider.targetGraphic = background;
        slider.fillRect = CreateSliderFill(sliderGo.transform);
        slider.handleRect = CreateSliderHandle(sliderGo.transform);
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(value => onValueChanged(value));

        if (labelText.Contains("Music"))
        {
            _musicValueText = valueText;
        }
        else
        {
            _gameValueText = valueText;
        }

        return slider;
    }

    private RectTransform CreateSliderFill(Transform parent)
    {
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(parent, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(10f, 7f);
        fillAreaRect.offsetMax = new Vector2(-10f, -7f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.95f, 0.76f, 0.42f, 1f);
        fillImage.raycastTarget = false;

        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        return fillRect;
    }

    private RectTransform CreateSliderHandle(Transform parent)
    {
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(parent, false);
        var handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(1f, 0.97f, 0.9f, 1f);
        handleImage.raycastTarget = false;

        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 30f);
        return handleRect;
    }

    private void CreateCloseButton(Vector2 anchoredPosition)
    {
        GameObject buttonGo = new GameObject("btn_CloseSettings");
        buttonGo.transform.SetParent(_panel.transform, false);
        var button = buttonGo.AddComponent<Button>();
        var image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.75f, 0.24f, 0.18f, 1f);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(170f, 54f);
        rect.anchoredPosition = anchoredPosition;

        GameObject labelGo = new GameObject("Text");
        labelGo.transform.SetParent(buttonGo.transform, false);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = "Close";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 28f;
        label.color = Color.white;
        label.raycastTarget = false;
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        button.onClick.AddListener(CloseSettings);
    }

    private IEnumerator ApplyAfterSceneLoad()
    {
        yield return null;
        TryBindScenePanel();
        ApplyVolumes();
        _applyRoutine = null;
    }

    public void ApplyVolumes()
    {
        AudioListener.volume = GameVolume;

        var sources = Object.FindObjectsOfType<AudioSource>(true);
        foreach (var source in sources)
        {
            if (source == null) continue;

            if (IsMusicSource(source))
            {
                int id = source.GetInstanceID();
                if (!_musicBaseVolumes.ContainsKey(id))
                {
                    _musicBaseVolumes[id] = source.volume <= 0f ? 1f : source.volume;
                }

                source.volume = _musicBaseVolumes[id] * MusicVolume;
            }
        }

        RefreshValueTexts();
    }

    private bool IsMusicSource(AudioSource source)
    {
        string objectName = source.gameObject != null ? source.gameObject.name : string.Empty;
        return objectName.IndexOf("AudioManager", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void SyncSlidersToValues()
    {
        if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(MusicVolume);
        if (_gameSlider != null) _gameSlider.SetValueWithoutNotify(GameVolume);
        RefreshValueTexts();
    }

    private GameObject GetActivePanel()
    {
        if (_scenePanel != null) return _scenePanel;
        if (_panel != null) return _panel;
        EnsureUI();
        return _scenePanel != null ? _scenePanel : _panel;
    }

    private void RefreshValueTexts()
    {
        if (_musicValueText != null) _musicValueText.text = $"{Mathf.RoundToInt(MusicVolume * 100f)}%";
        if (_gameValueText != null) _gameValueText.text = $"{Mathf.RoundToInt(GameVolume * 100f)}%";
    }
}