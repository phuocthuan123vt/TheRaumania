using UnityEngine;
using UnityEngine.UI;
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
            Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
            p.GetComponent<RectTransform>().anchoredPosition = randomPos;
            Button btn = p.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnPointClicked(p));
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