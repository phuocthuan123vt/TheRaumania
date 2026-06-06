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
}