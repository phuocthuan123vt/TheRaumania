using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("Cài Đặt Chuyển Map")]
    public string targetSceneName; // Tên Scene sẽ Load (VD: scn_Restaurant)
    public string spawnPointName;  // Tên của vị trí sẽ xuất hiện sau khi Tele tới (VD: EntryPoint_Restaurant)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // Ghi nhớ tên điểm SpawnPoint trước khi load map
        PersistentGameManager.TargetSpawnPointName = spawnPointName;

        // Nếu scene đã được load trước đó, chỉ set active và di chuyển Player
        var existing = SceneManager.GetSceneByName(targetSceneName);
        if (existing.IsValid() && existing.isLoaded)
        {
            // Set scene active so lighting/cameras in that scene become active
            SceneManager.SetActiveScene(existing);
            // Yêu cầu PersistentGameManager di chuyển Player tới spawn trong scene đó
            if (PersistentGameManager.Instance != null)
            {
                PersistentGameManager.Instance.MovePlayerToSpawnPoint(spawnPointName);
                PersistentGameManager.Instance.ActivateSceneForPlayer(existing);
            }
            return;
        }

        // Nếu chưa load, load additive để giữ scene hiện tại chạy song song
        StartCoroutine(LoadTargetSceneAdditive());
    }

    private System.Collections.IEnumerator LoadTargetSceneAdditive()
    {
        var op = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        // Set newly loaded scene active
        var loaded = SceneManager.GetSceneByName(targetSceneName);
        if (loaded.IsValid())
        {
            SceneManager.SetActiveScene(loaded);
            if (PersistentGameManager.Instance != null)
            {
                // Move player (OnSceneLoaded already handles MovePlayerToSpawnPoint if TargetSpawnPointName set)
                PersistentGameManager.Instance.ActivateSceneForPlayer(loaded);
            }
        }
    }
}