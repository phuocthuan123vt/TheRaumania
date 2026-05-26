using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentGameManager : MonoBehaviour
{
    public static PersistentGameManager Instance;

    // Lưu lại điểm sẽ xuất hiện sau khi chuyển Scene
    public static string TargetSpawnPointName = "";

    // Gom Player, Main Camera, HUD Canvas, Managers vào làm con (Child) của Script này
    // Để giữ nguyên trạng thái bay từ Scene sang Scene khác
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Bất tử qua mọi bản map
            
            // Đăng ký rà soát sự kiện khi 1 Map mới được load xong
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            // Tránh bị nhân đôi Player / HUD nếu mội map đều có con Player có sẵn
            Destroy(gameObject); 
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Khi Scene được load lên (scn_Restaurant, scn_Store)
        // Mình sẽ dời Player vào vị trí điểm Spawn đã định trước
        if (!string.IsNullOrEmpty(TargetSpawnPointName))
        {
            GameObject spawnPoint = GameObject.Find(TargetSpawnPointName);
            if (spawnPoint != null)
            {
                // Dời vị trí của khối Persistent (hoặc tìm con Player bên trong để dời)
                // Lưu ý: PersistentGameManager phải gom cả Player vào làm con của nó.
                PlayerMovement player = GetComponentInChildren<PlayerMovement>(true);
                if (player != null)
                {
                    player.transform.position = spawnPoint.transform.position;
                    // Reset animation đi lui nếu có
                }
            }
            else
            {
                Debug.LogWarning($"<color=yellow>Chuyển map thành công nhưng không tìm thấy mốc: '{TargetSpawnPointName}' để xếp Player đứng lên!</color>");
            }
            
            TargetSpawnPointName = ""; // Làm trống cho lần sau
        }
    }
}