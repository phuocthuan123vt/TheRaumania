using UnityEngine;
using System.Linq;

public class RestaurantRatingManager : MonoBehaviour
{
    public static RestaurantRatingManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
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
        float food = PlayerData.foodQualityScore;
        float hygiene = PlayerData.hygieneScore;
        float decor = PlayerData.decorationScore;
        float attitude = GetAttitudeScore();

        // Tổng max = 40. Công thức quy đổi ra 5 sao: Total / 8.
        float rawStars = (food + hygiene + decor + attitude) / 8f;

        // Ép giới hạn từ 1 đến 5 sao
        return Mathf.Clamp(rawStars, 1f, 5f);
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
    }

    /// <summary>
    /// Giảm điểm vệ sinh nếu có vết bẩn
    /// </summary>
    public void DecreaseHygiene(float amount)
    {
        PlayerData.hygieneScore -= amount;
        PlayerData.hygieneScore = Mathf.Clamp(PlayerData.hygieneScore, 0f, 10f);
    }

    public void IncreaseHygiene(float amount)
    {
        PlayerData.hygieneScore += amount;
        PlayerData.hygieneScore = Mathf.Clamp(PlayerData.hygieneScore, 0f, 10f);
    }
}
