using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("Cài Đặt Chuyển Map")]
    public string targetSceneName; // Tên Scene sẽ Load (VD: scn_Restaurant)
    public string spawnPointName;  // Tên của vị trí sẽ xuất hiện sau khi Tele tới (VD: EntryPoint_Restaurant)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Ghi nhớ tên điểm SpawnPoint trước khi load map
            PersistentGameManager.TargetSpawnPointName = spawnPointName;
            
            // Nếu bạn có dùng hiệu ứng mờ dần (Fade), bạn có thể gọi ở đây
            // Còn đây là load trực tiếp
            SceneManager.LoadScene(targetSceneName);
        }
    }
}