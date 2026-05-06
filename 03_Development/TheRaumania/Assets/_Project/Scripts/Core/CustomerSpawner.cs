using System.Collections;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Cấu hình Spawn")]
    public Transform spawnPoint; // Điểm khách đứng khi xấp hiện (Cửa ra vào)
    public bool isSpawning = true;

    [Header("Thời gian chờ (Cooldown)")]
    public float maxSpawnDelay = 15f; // Thời gian chờ khi nhà hàng 1 sao
    public float minSpawnDelay = 4f;  // Thời gian chờ nhanh nhất khi nhà hàng 5 sao

    [Header("Prefabs Khách Hàng")]
    public GameObject normalCustomerPrefab;
    public GameObject vipCustomerPrefab;
    public GameObject criticCustomerPrefab;

    private void Start()
    {
        if (spawnPoint == null)
        {
            spawnPoint = this.transform; // Lấy chính nó làm điểm sinh nếu chưa gán
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        // Chờ 2 giây đầu game cho các hệ thống khởi tạo xong
        yield return new WaitForSeconds(2f);

        while (isSpawning)
        {
            // 1. Lấy số sao hiện tại
            float currentStars = 1f;
            if (RestaurantRatingManager.Instance != null)
            {
                currentStars = RestaurantRatingManager.Instance.GetRestaurantStars();
            }

            // 2. Tính toán thời gian chờ dựa trên số sao (Sao càng cao, chờ càng ít)
            // Tỷ lệ (currentStars - 1) / 4f sẽ trả về từ 0 (nếu 1 sao) đến 1 (nếu 5 sao)
            float t = Mathf.Clamp01((currentStars - 1f) / 4f); 
            float currentDelay = Mathf.Lerp(maxSpawnDelay, minSpawnDelay, t);

            yield return new WaitForSeconds(currentDelay);

            // 3. Sinh khách
            SpawnCustomer(currentStars);
        }
    }

    private void SpawnCustomer(float stars)
    {
        GameObject prefabToSpawn = normalCustomerPrefab; // Mặc định là Normal
        float roll = Random.value; // Quay lô tô từ 0.00 đến 1.00 (0% đến 100%)

        if (stars >= 4.8f) // ~5 SAO (Cho phép sai số một chút)
        {
            // Nhà hàng đỉnh cao: 5% Phê bình, 15% VIP, 80% Thường
            if (roll <= 0.05f) 
            {
                prefabToSpawn = criticCustomerPrefab;
                Debug.Log("<color=magenta>[Spawner] Chà! Một nhà phê bình ẩm thực vừa tới!</color>");
            }
            else if (roll <= 0.20f) 
            {
                prefabToSpawn = vipCustomerPrefab;
                Debug.Log("<color=yellow>[Spawner] Một khách VIP vừa bước vào cửa!</color>");
            }
        }
        else if (stars >= 3f) // TỪ 3 SAO ĐẾN DƯỚI 5 SAO
        {
            // Cửa hàng khá: 10% VIP, 90% Thường (Không có Phê bình)
            if (roll <= 0.10f) 
            {
                prefabToSpawn = vipCustomerPrefab;
                Debug.Log("<color=yellow>[Spawner] Có khách VIP tới ủng hộ!</color>");
            }
        }
        else // DƯỚI 3 SAO
        {
            // Cửa hàng bình dân: 100% Khách thường
            prefabToSpawn = normalCustomerPrefab;
        }

        // Đẻ khách ra đời
        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("[CustomerSpawner] Bạn chưa gán Prefab cho khách hàng nào cả!");
        }
    }
}
