using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class RecipeBookUI : MonoBehaviour
{
    public GameObject recipeSlotPrefab; 
    public Transform contentArea;      
    public List<RecipeSO> allRecipes;   

    [Header("Recipe Details")]
    public GameObject img_RecipeDetail;
    public TextMeshProUGUI txtRecipeDetail;

    private RecipeSO _currentRecipe;
    private Transform ingredientsContent;

    private void OnEnable()
    {
        AutoMapDetailsUI();
        CookingEvents.OnRecipeSelected += HandleRecipeSelected;

        if (contentArea == null || recipeSlotPrefab == null)
        {
            ShowEmptyDetails();
            return;
        }

        if (allRecipes == null)
        {
            ShowEmptyDetails();
            return;
        }

        foreach (Transform child in contentArea) Destroy(child.gameObject);
        foreach (var recipe in allRecipes)
        {
            GameObject newSlot = Instantiate(recipeSlotPrefab, contentArea);
            newSlot.GetComponent<RecipeSlotUI>().Setup(recipe);
        }

        if (allRecipes != null && allRecipes.Count > 0)
        {
            ShowRecipeDetails(allRecipes[0]);
        }
        else
        {
            ShowEmptyDetails();
        }
    }

    private void OnDisable()
    {
        CookingEvents.OnRecipeSelected -= HandleRecipeSelected;
    }

    private void AutoMapDetailsUI()
    {
        Debug.Log("RecipeBookUI: AutoMapDetailsUI running");
        if (img_RecipeDetail == null)
        {
            img_RecipeDetail = RuntimeReferenceFinder.FindDeepGameObject(transform, "img_RecipeDetail", "pnl_RecipeDetail", "pnl_RecipeInfo", "pnl_RecipeHint");
        }

        if (txtRecipeDetail == null && img_RecipeDetail != null)
        {
            txtRecipeDetail = RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(img_RecipeDetail.transform, "txt_RecipeDetail", "txt_RecipeInfo", "txt_Ingredients", "txt_Hint");
        }

        if (img_RecipeDetail != null)
        {
            RectTransform rt = img_RecipeDetail.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(300f, 800f);
            }

            Image bg = img_RecipeDetail.GetComponent<Image>();
            if (bg != null) bg.raycastTarget = false;

            CanvasGroup cg = img_RecipeDetail.GetComponent<CanvasGroup>();
            if (cg == null) cg = img_RecipeDetail.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            if (txtRecipeDetail != null) txtRecipeDetail.raycastTarget = false;
        }

        if (img_RecipeDetail == null)
        {
            BuildFallbackDetailUI();
        }

        if (img_RecipeDetail != null)
        {
            img_RecipeDetail.SetActive(true);
        }

        // try get existing ingredients content container
        if (img_RecipeDetail != null && ingredientsContent == null)
        {
            GameObject found = RuntimeReferenceFinder.FindDeepGameObject(img_RecipeDetail.transform, "contentIngredients", "content_ingredients", "content");
            if (found != null) ingredientsContent = found.transform;
        }
    }

    private void BuildFallbackDetailUI()
    {
        if (img_RecipeDetail == null)
        {
            GameObject panel = new GameObject("img_RecipeDetail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0.5f);
            panelRect.anchorMax = new Vector2(0f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.sizeDelta = new Vector2(300f, 800f);
            panelRect.anchoredPosition = new Vector2(0f, 0f);
            panelRect.localScale = Vector3.one;

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.12f, 0.10f, 0.08f, 0.92f);
            image.raycastTarget = false;

            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            img_RecipeDetail = panel;
        }

        if (txtRecipeDetail == null)
        {
            GameObject textObject = new GameObject("txt_RecipeDetail", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(img_RecipeDetail.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.04f, 0.04f);
            textRect.anchorMax = new Vector2(0.96f, 0.96f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = GetFallbackFontAsset();
            text.fontSize = 28;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.text = "Chọn một món để xem nguyên liệu.";
            text.raycastTarget = false;

            txtRecipeDetail = text;
        }
        
        // create an ingredients content container under detail panel if not present
        if (ingredientsContent == null)
        {
            GameObject content = new GameObject("contentIngredients", typeof(RectTransform), typeof(CanvasRenderer), typeof(VerticalLayoutGroup));
            content.transform.SetParent(img_RecipeDetail.transform, false);
            RectTransform cr = content.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0.04f, 0.06f);
            cr.anchorMax = new Vector2(0.96f, 0.90f);
            cr.offsetMin = Vector2.zero;
            cr.offsetMax = Vector2.zero;
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 8f;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ingredientsContent = content.transform;
        }
    }

    private void HandleRecipeSelected(RecipeSO recipe)
    {
        ShowRecipeDetails(recipe);
    }

    private void ShowEmptyDetails()
    {
        _currentRecipe = null;
        if (txtRecipeDetail != null)
        {
            txtRecipeDetail.text = "Chưa có công thức nào trong danh sách.";
        }
    }

    private void ShowRecipeDetails(RecipeSO recipe)
    {
        Debug.Log($"RecipeBookUI: ShowRecipeDetails -> {recipe?.dishName}");
        _currentRecipe = recipe;

        if (recipe == null)
        {
            return;
        }

        // If we have an ingredients content container, populate it with icon + name
        if (ingredientsContent != null)
        {
            // clear existing
            foreach (Transform t in ingredientsContent) Destroy(t.gameObject);

            // title
            GameObject titleObj = new GameObject("txtTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(ingredientsContent, false);
            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 40f;
            titleLayout.minHeight = 40f;
            TextMeshProUGUI title = titleObj.GetComponent<TextMeshProUGUI>();
            title.font = GetFallbackFontAsset();
            title.fontSize = 30;
            title.color = Color.white;
            title.alignment = TextAlignmentOptions.Left;
            title.enableWordWrapping = true;
            title.text = recipe.dishName;

            if (recipe.ingredientsRequired == null || recipe.ingredientsRequired.Count == 0)
            {
                GameObject emptyObj = new GameObject("txtEmpty", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                emptyObj.transform.SetParent(ingredientsContent, false);
                LayoutElement emptyLayout = emptyObj.AddComponent<LayoutElement>();
                emptyLayout.preferredHeight = 32f;
                emptyLayout.minHeight = 32f;
                TextMeshProUGUI et = emptyObj.GetComponent<TextMeshProUGUI>();
                et.font = GetFallbackFontAsset();
                et.fontSize = 22;
                et.color = Color.white;
                et.enableWordWrapping = true;
                et.text = "Chưa khai báo nguyên liệu.";
            }
            else
            {
                Dictionary<string, int> counts = new Dictionary<string, int>();
                Dictionary<string, BaseItemSO> itemsById = new Dictionary<string, BaseItemSO>();

                foreach (BaseItemSO ingredient in recipe.ingredientsRequired)
                {
                    if (ingredient == null) continue;

                    if (counts.ContainsKey(ingredient.id)) counts[ingredient.id]++;
                    else { counts[ingredient.id] = 1; itemsById[ingredient.id] = ingredient; }
                }

                foreach (var pair in counts)
                {
                    BaseItemSO ing = itemsById[pair.Key];
                    GameObject line = new GameObject("ingLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(HorizontalLayoutGroup));
                    line.transform.SetParent(ingredientsContent, false);
                    LayoutElement lineLayout = line.AddComponent<LayoutElement>();
                    lineLayout.preferredHeight = 48f;
                    lineLayout.minHeight = 48f;
                    HorizontalLayoutGroup h = line.GetComponent<HorizontalLayoutGroup>();
                    h.padding = new RectOffset(0, 0, 0, 0);
                    h.childForceExpandHeight = false;
                    h.childForceExpandWidth = false;
                    h.childControlHeight = true;
                    h.childControlWidth = true;
                    h.spacing = 8f;
                    h.childAlignment = TextAnchor.MiddleLeft;

                    // icon
                    GameObject iconObj = new GameObject("icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    iconObj.transform.SetParent(line.transform, false);
                    Image img = iconObj.GetComponent<Image>();
                    img.sprite = ing.icon;
                    img.preserveAspect = true;
                    RectTransform ir = iconObj.GetComponent<RectTransform>();
                    ir.sizeDelta = new Vector2(40f, 40f);
                    LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
                    iconLayout.preferredWidth = 40f;
                    iconLayout.preferredHeight = 40f;
                    iconLayout.minWidth = 40f;
                    iconLayout.minHeight = 40f;

                    // text
                    GameObject txtObj = new GameObject("txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    txtObj.transform.SetParent(line.transform, false);
                    LayoutElement textLayout = txtObj.AddComponent<LayoutElement>();
                    textLayout.flexibleWidth = 1f;
                    TextMeshProUGUI ti = txtObj.GetComponent<TextMeshProUGUI>();
                    ti.font = GetFallbackFontAsset();
                    ti.fontSize = 22;
                    ti.color = Color.white;
                    ti.alignment = TextAlignmentOptions.Left;
                    ti.enableWordWrapping = true;
                    ti.text = $"{ing.itemName} x{pair.Value}";
                }
            }

            // base value
            GameObject valObj = new GameObject("txtValue", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            valObj.transform.SetParent(ingredientsContent, false);
            LayoutElement valueLayout = valObj.AddComponent<LayoutElement>();
            valueLayout.preferredHeight = 30f;
            valueLayout.minHeight = 30f;
            TextMeshProUGUI vt = valObj.GetComponent<TextMeshProUGUI>();
            vt.font = GetFallbackFontAsset();
            vt.fontSize = 20;
            vt.color = Color.white;
            vt.enableWordWrapping = true;
            vt.text = $"Giá trị cơ bản: {recipe.baseValue}";

            // hide legacy text area
            if (txtRecipeDetail != null) txtRecipeDetail.gameObject.SetActive(false);
            return;
        }

        // Fallback: plain text block
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"<b>{recipe.dishName}</b>");
        builder.AppendLine();
        builder.AppendLine("<b>Nguyên liệu cần:</b>");

        if (recipe.ingredientsRequired == null || recipe.ingredientsRequired.Count == 0)
        {
            builder.AppendLine("- Chưa khai báo nguyên liệu.");
        }
        else
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            Dictionary<string, BaseItemSO> itemsById = new Dictionary<string, BaseItemSO>();

            foreach (BaseItemSO ingredient in recipe.ingredientsRequired)
            {
                if (ingredient == null) continue;

                if (counts.ContainsKey(ingredient.id))
                {
                    counts[ingredient.id]++;
                }
                else
                {
                    counts[ingredient.id] = 1;
                    itemsById[ingredient.id] = ingredient;
                }
            }

            foreach (var pair in counts)
            {
                BaseItemSO ingredient = itemsById[pair.Key];
                builder.AppendLine($"- {ingredient.itemName} x{pair.Value}");
            }
        }

        builder.AppendLine();
        builder.AppendLine($"<b>Giá trị cơ bản:</b> {recipe.baseValue}");
        txtRecipeDetail.text = builder.ToString();
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