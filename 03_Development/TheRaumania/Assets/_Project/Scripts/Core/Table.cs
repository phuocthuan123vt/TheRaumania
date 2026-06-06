using UnityEngine;

public class Table : MonoBehaviour
{
    [System.Serializable]
    public class Seat
    {
        public Transform point;    // Điểm tọa độ cái ghế
        public Transform leavePoint; // Điểm khách đứng dậy trước khi rời bàn
        public float sitDirection; // 1: Nhìn sang phải, -1: Nhìn sang trái
        public bool isOccupied;
    }

    public Seat[] seats;
    public bool isNearWindow;

    private void Start()
    {
        Interactable interactable = GetComponent<Interactable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<Interactable>();
        }
        interactable.interactMessage = "Nhấn E để xếp bàn";
        interactable.interactRange = 2f;
        interactable.onInteract.RemoveAllListeners();
        interactable.onInteract.AddListener(OnTableInteracted);
    }

    private void OnTableInteracted()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnPlayerInteractWithTable(this);
        }
    }
}