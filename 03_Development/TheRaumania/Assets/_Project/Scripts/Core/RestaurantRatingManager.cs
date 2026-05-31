using UnityEngine;
using System.Linq;
using System;
using System.Text;
using TMPro;

public class RestaurantRatingManager : MonoBehaviour
{
    public static RestaurantRatingManager Instance;

    // Event fired when the restaurant star rating changes
    public static event Action<float> OnRatingChanged;

    [Header("UI (auto-mapped at runtime)")]
    public TextMeshProUGUI txtRating;

    private void Awake()
    {
        if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.transform.root.gameObject);
        }
        else if (Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        // Try to auto-map HUD text if available
        if (txtRating == null && HUDManager.Instance != null)
        {
            txtRating = HUDManager.Instance.txtRating;
        }

        if (txtRating == null)
        {
            txtRating = RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(transform.root, "txt_Rating");
        }

        // Broadcast initial rating so HUD can initialize
        BroadcastRating();
    }

    private void Start()
    {
        if (txtRating == null)
        {
            txtRating = RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(transform.root, "txt_Rating");
        }

        BroadcastRating();
    }

    /// <summary>
    /// Tính trung bình thái độ (mức độ hài lòng của 50 khách hàng gần nhất)
    /// Trả về thang điểm max 10.
    /// </summary>
    public float GetAttitudeScore()
    {
        if (PlayerData.satisfactionHistory.Count == 0) return 5f; // Mặc định nếu chưa có khách
        return PlayerData.satisfactionHistory.Average();
    }

    /// <summary>
    /// Lấy điểm đánh giá tổng cộng của Nhà Hàng. (Thang điểm 5 Sao)
    /// </summary>
    public float GetRestaurantStars()
    {
        return Mathf.Clamp(GetRawStarScore(), 1f, 5f);
    }

    public float GetRawStarScore()
    {
        float food = PlayerData.foodQualityScore;
        float hygiene = PlayerData.hygieneScore;
        float decor = GetDecorationScore();
        float attitude = GetAttitudeScore();

        // Tổng max = 40. Công thức quy đổi ra 5 sao: Total / 8.
        return (food + hygiene + decor + attitude) / 8f;
    }

    public string GetScoreBreakdownText()
    {
        float food = PlayerData.foodQualityScore;
        float hygiene = PlayerData.hygieneScore;
        float decor = GetDecorationScore();
        float attitude = GetAttitudeScore();
        float stars = GetRestaurantStars();

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("<b>Điểm nhà hàng</b>");
        builder.AppendLine($"Món ăn: {food:0.0}/10");
        builder.AppendLine($"Vệ sinh: {hygiene:0.0}/10");
        builder.AppendLine($"Trang trí: {decor:0.0}/10");
        builder.AppendLine($"Thái độ: {attitude:0.0}/10");
        builder.AppendLine($"Tổng: {stars:0.0}/5 sao");
        return builder.ToString();
    }

    public float GetDecorationScore()
    {
        if (UpgradeManager.Instance == null)
        {
            return PlayerData.decorationScore;
        }

        switch (Mathf.Clamp(UpgradeManager.Instance.highestUnlockedLevel, 1, 3))
        {
            case 1:
                return 3f;
            case 2:
                return 6f;
            case 3:
                return 10f;
            default:
                return 3f;
        }
    }

    public void RefreshRating()
    {
        BroadcastRating();
    }

    void BroadcastRating()
    {
        float stars = GetRestaurantStars();
        OnRatingChanged?.Invoke(stars);

        if (txtRating != null)
        {
            txtRating.text = string.Format("{0:F1}", stars);
        }
    }

    /// <summary>
    /// Lắng nghe khách hàng gọi review trước khi rời đi
    /// </summary>
    public void SubmitCustomerReview(float satisfactionScore, float foodScore)
    {
        // 1. Cập nhật vào Queue Thái độ (Giới hạn 50 bill)
        PlayerData.satisfactionHistory.Enqueue(satisfactionScore);
        if (PlayerData.satisfactionHistory.Count > 50)
        {
            PlayerData.satisfactionHistory.Dequeue(); // Đuổi khách cũ nhất đi cho nhẹ RAM
        }

        // 2. Trung bình thêm vào điểm Chất Lượng Món Ăn của nhà hàng
        // Hiện tại món ăn đánh giá đang có thể để tự trung bình từ từ, hoặc thay đổi dần
        // Ví dụ: làm mượt điểm thức ăn mới
        PlayerData.foodQualityScore = Mathf.Lerp(PlayerData.foodQualityScore, foodScore, 0.1f);

        Debug.Log($"<color=orange>Khách review: Hài lòng={satisfactionScore:F1}/10, Món={foodScore:F1}/10. => Nhà hàng đang đạt {GetRestaurantStars():F1} Sao.</color>");

        // Notify listeners and update mapped UI
        BroadcastRating();
    }

    /// <summary>
    /// Giảm điểm vệ sinh nếu có vết bẩn
    /// </summary>
    public void DecreaseHygiene(float amount)
    {
        PlayerData.hygieneScore -= amount;
        PlayerData.hygieneScore = Mathf.Clamp(PlayerData.hygieneScore, 0f, 10f);
        BroadcastRating();
    }

    public void IncreaseHygiene(float amount)
    {
        PlayerData.hygieneScore += amount;
        PlayerData.hygieneScore = Mathf.Clamp(PlayerData.hygieneScore, 0f, 10f);
        BroadcastRating();
    }
}
