using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PrepMinigame : MinigameBase
{
    public enum ArrowDirection
    {
        Left,
        Down,
        Right,
        Up
    }

    public enum ArrowColorVariant
    {
        Blue,
        Green,
        Red
    }

    [System.Serializable]
    public class ArrowSpriteSet
    {
        public Sprite left;
        public Sprite down;
        public Sprite right;
        public Sprite up;
    }

    [Header("Legacy Scene Objects")]
    [HideInInspector] public GameObject[] dirtPoints;
    [HideInInspector] public float spawnRadius = 250f;

    [Header("Arrow Sprites")]
    public ArrowSpriteSet blueArrows;
    public ArrowSpriteSet greenArrows;
    public ArrowSpriteSet redArrows;

    [Header("Manual UI References")]
    public RectTransform panelRoot;
    public Image frameBackground;
    public RectTransform sequenceRoot;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI scoreText;

    [Header("Layout")]
    public int sequenceLength = 10;
    public Vector2 sequenceRootSize = new Vector2(760f, 96f);
    public Vector2 slotSize = new Vector2(68f, 68f);
    public float slotSpacing = 6f;
    public Vector2 sequenceAnchoredPosition = new Vector2(0f, -130f);

    [Header("Colors")]
    public Color slotNeutralColor = new Color(1f, 1f, 1f, 0.18f);
    public Color slotCurrentColor = new Color(1f, 0.92f, 0.4f, 0.95f);
    public Color slotCorrectColor = new Color(0.35f, 0.85f, 0.35f, 0.95f);
    public Color slotWrongColor = new Color(0.9f, 0.28f, 0.28f, 0.95f);

    private struct ArrowStep
    {
        public ArrowDirection direction;
    }

    private class SlotView
    {
        public RectTransform root;
        public Image background;
        public Image icon;
        public TextMeshProUGUI fallbackText;
    }

    private readonly List<ArrowStep> _steps = new List<ArrowStep>();
    private readonly List<SlotView> _slots = new List<SlotView>();
    private int _currentIndex;
    private int _correctCount;
    private bool _isPlaying;

    public override MinigameType GetMinigameType() => MinigameType.Prep;

    public override void StartGame(float freshness)
    {
        base.StartGame(freshness);

        _isPlaying = true;
        _currentIndex = 0;
        _correctCount = 0;

        DisableLegacyDirtPoints();
        EnsureUI();
        BuildSequence();
        ApplySequenceVisuals();
        UpdateInstructionLabel();
        UpdateScoreUI();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (sequenceRoot != null)
        {
            sequenceRoot.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (!_isPlaying)
        {
            return;
        }

        ArrowDirection? direction = ReadInput();
        if (!direction.HasValue)
        {
            return;
        }

        HandleInput(direction.Value);
    }

    private void DisableLegacyDirtPoints()
    {
        if (dirtPoints == null)
        {
            return;
        }

        foreach (GameObject dirtPoint in dirtPoints)
        {
            if (dirtPoint != null)
            {
                dirtPoint.SetActive(false);
            }
        }
    }

    private void EnsureUI()
    {
        if (sequenceRoot == null)
        {
            GameObject sequenceObject = RuntimeReferenceFinder.FindDeepGameObject(transform, "contentArrowSequence", "pnl_ArrowSequence", "img_ArrowSequence");
            if (sequenceObject == null)
            {
                sequenceObject = new GameObject("contentArrowSequence", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
                sequenceObject.transform.SetParent(transform, false);

                Image bg = sequenceObject.GetComponent<Image>();
                bg.color = new Color(0.08f, 0.08f, 0.10f, 0.78f);
                bg.raycastTarget = false;

                HorizontalLayoutGroup hlg = sequenceObject.GetComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(10, 10, 10, 10);
                hlg.spacing = slotSpacing;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;

                ContentSizeFitter fitter = sequenceObject.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            sequenceRoot = sequenceObject.GetComponent<RectTransform>();
        }

        if (sequenceRoot != null)
        {
            sequenceRoot.anchorMin = new Vector2(0.5f, 0.5f);
            sequenceRoot.anchorMax = new Vector2(0.5f, 0.5f);
            sequenceRoot.pivot = new Vector2(0.5f, 0.5f);
            sequenceRoot.sizeDelta = sequenceRootSize;
            sequenceRoot.anchoredPosition = sequenceAnchoredPosition;
            sequenceRoot.localScale = Vector3.one;
        }

        if (instructionText == null)
        {
            instructionText = CreateLabel("txt_MinigamePrepInstruction", new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), 28f, Color.white);
        }

        if (scoreText == null)
        {
            scoreText = CreateLabel("txt_MinigamePrepScore", new Vector2(0.5f, 0f), new Vector2(0f, 60f), 36f, new Color(1f, 0.95f, 0.4f, 1f));
        }

        if (_slots.Count == 0)
        {
            BuildSlots();
        }
    }

    private TextMeshProUGUI CreateLabel(string objectName, Vector2 anchor, Vector2 anchoredPosition, float fontSize, Color color)
    {
        GameObject labelObject = RuntimeReferenceFinder.FindDeepGameObject(transform, objectName);
        if (labelObject == null)
        {
            labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);
        }

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = anchor;
        labelRect.anchorMax = anchor;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = new Vector2(900f, 60f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = GetFallbackFontAsset();
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        return label;
    }

    private void BuildSlots()
    {
        if (sequenceRoot == null)
        {
            return;
        }

        for (int i = 0; i < sequenceLength; i++)
        {
            GameObject slotObject = new GameObject($"slot_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            slotObject.transform.SetParent(sequenceRoot, false);

            RectTransform slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.sizeDelta = slotSize;

            Image background = slotObject.GetComponent<Image>();
            background.sprite = null;
            background.type = Image.Type.Sliced;
            background.color = slotNeutralColor;
            background.raycastTarget = false;

            LayoutElement layout = slotObject.GetComponent<LayoutElement>();
            layout.preferredWidth = slotSize.x;
            layout.preferredHeight = slotSize.y;
            layout.minWidth = slotSize.x;
            layout.minHeight = slotSize.y;

            GameObject iconObject = new GameObject("icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(slotObject.transform, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(6f, 6f);
            iconRect.offsetMax = new Vector2(-6f, -6f);

            Image icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI fallbackText = null;
            if (GetAnySpriteForPreview() == null)
            {
                GameObject fallbackObject = new GameObject("txtFallback", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                fallbackObject.transform.SetParent(slotObject.transform, false);

                RectTransform fallbackRect = fallbackObject.GetComponent<RectTransform>();
                fallbackRect.anchorMin = Vector2.zero;
                fallbackRect.anchorMax = Vector2.one;
                fallbackRect.offsetMin = Vector2.zero;
                fallbackRect.offsetMax = Vector2.zero;

                fallbackText = fallbackObject.GetComponent<TextMeshProUGUI>();
                fallbackText.font = GetFallbackFontAsset();
                fallbackText.fontSize = 30f;
                fallbackText.alignment = TextAlignmentOptions.Center;
                fallbackText.color = Color.white;
                fallbackText.raycastTarget = false;
            }

            _slots.Add(new SlotView
            {
                root = slotRect,
                background = background,
                icon = icon,
                fallbackText = fallbackText
            });
        }
    }

    private void BuildSequence()
    {
        _steps.Clear();

        for (int i = 0; i < sequenceLength; i++)
        {
            _steps.Add(new ArrowStep
            {
                direction = (ArrowDirection)Random.Range(0, 4)
            });
        }
    }

    private void ApplySequenceVisuals()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            SlotView slot = _slots[i];
            if (slot == null)
            {
                continue;
            }

            slot.background.color = (i == 0) ? slotCurrentColor : slotNeutralColor;

            if (i >= _steps.Count)
            {
                continue;
            }

            ArrowStep step = _steps[i];
            Sprite sprite = GetSprite(blueArrows, step.direction);

            if (slot.icon != null)
            {
                slot.icon.sprite = sprite;
                slot.icon.enabled = sprite != null;
                slot.icon.color = Color.white;
            }

            if (slot.fallbackText != null)
            {
                slot.fallbackText.text = GetArrowGlyph(step.direction);
                slot.fallbackText.gameObject.SetActive(sprite == null);
            }
        }
    }

    private void HandleInput(ArrowDirection pressed)
    {
        if (_currentIndex >= _steps.Count || _currentIndex >= _slots.Count)
        {
            return;
        }

        ArrowStep step = _steps[_currentIndex];
        bool correct = step.direction == pressed;

        SlotView slot = _slots[_currentIndex];
        slot.background.color = correct ? slotCorrectColor : slotWrongColor;

        if (slot.icon != null)
        {
            slot.icon.sprite = correct ? GetSprite(greenArrows, step.direction) : GetSprite(redArrows, step.direction);
            slot.icon.enabled = slot.icon.sprite != null;
            slot.icon.color = Color.white;
        }

        if (slot.fallbackText != null)
        {
            slot.fallbackText.color = correct ? Color.white : new Color(1f, 0.6f, 0.6f, 1f);
        }

        if (correct)
        {
            _correctCount++;
        }

        _currentIndex++;

        if (_currentIndex < _slots.Count)
        {
            _slots[_currentIndex].background.color = slotCurrentColor;
        }

        UpdateScoreUI();

        if (_currentIndex >= _steps.Count)
        {
            FinishGame();
        }
    }

    private void FinishGame()
    {
        _isPlaying = false;
        UpdateScoreUI(true);
        Complete(_correctCount * 10f);
    }

    private void UpdateInstructionLabel()
    {
        if (instructionText == null)
        {
            return;
        }

        instructionText.text = "Bấm mũi tên hoặc WASD từ trái sang phải";
    }

    private void UpdateScoreUI(bool final = false)
    {
        if (scoreText != null)
        {
            scoreText.text = final ? $"Kết quả: {_correctCount}/10" : $"Điểm: {_correctCount}/10";
        }
    }

    private ArrowDirection? ReadInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return ArrowDirection.Left;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return ArrowDirection.Down;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return ArrowDirection.Right;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return ArrowDirection.Up;
        return null;
    }

    private Sprite GetSprite(ArrowSpriteSet set, ArrowDirection direction)
    {
        if (set == null)
        {
            return null;
        }

        switch (direction)
        {
            case ArrowDirection.Left:
                return set.left;
            case ArrowDirection.Down:
                return set.down;
            case ArrowDirection.Right:
                return set.right;
            case ArrowDirection.Up:
                return set.up;
            default:
                return null;
        }
    }

    private Sprite GetAnySpriteForPreview()
    {
        return blueArrows?.left ?? blueArrows?.down ?? blueArrows?.right ?? blueArrows?.up ??
               greenArrows?.left ?? greenArrows?.down ?? greenArrows?.right ?? greenArrows?.up ??
               redArrows?.left ?? redArrows?.down ?? redArrows?.right ?? redArrows?.up;
    }

    private string GetArrowGlyph(ArrowDirection direction)
    {
        switch (direction)
        {
            case ArrowDirection.Left: return "←";
            case ArrowDirection.Down: return "↓";
            case ArrowDirection.Right: return "→";
            case ArrowDirection.Up: return "↑";
            default: return "?";
        }
    }

    private TMP_FontAsset GetFallbackFontAsset()
    {
        if (TMP_Settings.defaultFontAsset != null)
        {
            return TMP_Settings.defaultFontAsset;
        }

        TextMeshProUGUI anyText = FindObjectOfType<TextMeshProUGUI>(true);
        if (anyText != null && anyText.font != null)
        {
            return anyText.font;
        }

        return null;
    }
}
