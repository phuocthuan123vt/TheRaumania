using UnityEngine;

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(SpriteRenderer))]
public class Dirt : MonoBehaviour
{
    [Header("Cài đặt")]
    public float hygieneRestoreAmount = 0.5f;
    
    [Header("Hình ảnh Ngẫu nhiên (Kéo 4 sprites rác vào đây)")]
    public Sprite[] randomDirtSprites;

    private void Awake()
    {
        // Random hình ảnh nếu có mảng sprites
        if (randomDirtSprites != null && randomDirtSprites.Length > 0)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.sprite = randomDirtSprites[Random.Range(0, randomDirtSprites.Length)];
        }

        // Tự động gán sự kiện CleanUp vào Interactable
        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.interactMessage = "Nhấn E để dọn dẹp";
            interactable.onInteract.AddListener(CleanUp);
        }
    }

    /// <summary>
    /// Hàm này được gọi khi người chơi bấm E (kích hoạt từ script Interactable)
    /// </summary>
    public void CleanUp()
    {
        // 1. Kiểm tra xem người chơi có đang cầm chổi (Broom) trên tay không
        bool hasBroom = false;
        if (HotbarManager.Instance != null)
        {
            int slotIdx = HotbarManager.Instance.SelectedSlotIndex;
            if (slotIdx >= 0 && slotIdx < HotbarManager.Instance.items.Count)
            {
                var heldItem = HotbarManager.Instance.items[slotIdx];
                if (heldItem != null && heldItem.itemData != null)
                {
                    string itemName = heldItem.itemData.itemName.ToLower();
                    if (itemName.Contains("chổi") || itemName.Contains("broom"))
                    {
                        hasBroom = true;
                    }
                }
            }
        }

        if (!hasBroom)
        {
            Debug.Log("<color=yellow>Bạn cần cầm 'Chổi' trên tay (chọn đúng ô trên Hotbar) để dọn dẹp!</color>");
            return; // Dừng lại, không cho dọn
        }

        // 2. Khôi phục điểm vệ sinh của nhà hàng
        if (RestaurantRatingManager.Instance != null)
        {
            RestaurantRatingManager.Instance.IncreaseHygiene(hygieneRestoreAmount);
            Debug.Log($"<color=cyan>Đã quét dọn xong! Tăng {hygieneRestoreAmount} điểm Vệ sinh.</color>");
        }

        // TODO: Chèn hiệu ứng Particle quét rác hoặc âm thanh "Xoẹt xoẹt" tại đây

        // 3. Hủy bãi rác/vết bẩn
        Destroy(gameObject);
    }
}
