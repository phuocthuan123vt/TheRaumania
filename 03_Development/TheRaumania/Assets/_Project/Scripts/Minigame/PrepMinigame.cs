using UnityEngine;
using UnityEngine.UI; // Đảm bảo có cái này nhen

public class PrepMinigame : MinigameBase
{
    public GameObject[] dirtPoints;
    public float spawnRadius = 250f;
    private int _remainingPoints;

    public override void StartGame(float freshness)
    {
        minigamePanel.SetActive(true);
        _remainingPoints = dirtPoints.Length;

        foreach (GameObject p in dirtPoints)
        {
            p.SetActive(true);

            // 1. Ngẫu nhiên vị trí trong hình tròn
            Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
            p.GetComponent<RectTransform>().anchoredPosition = randomPos;

            // 2. TỰ ĐỘNG GÁN SỰ KIỆN CLICK (Lead Dev Style)
            // Thay vì gán trong Inspector, ta gán bằng code ở đây
            Button btn = p.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners(); // Xóa lệnh cũ
                btn.onClick.AddListener(() => OnPointClicked(p)); // Cắm dây lệnh mới
            }
        }
    }

    public void OnPointClicked(GameObject point)
    {
        point.SetActive(false);
        _remainingPoints--;
        Debug.Log("Đã dọn 1 vết bẩn, còn lại: " + _remainingPoints);

        if (_remainingPoints <= 0)
        {
            Complete(100);
        }
    }
}