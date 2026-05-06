using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public string saveFileName; 
    public int rCredit;

    // Đánh giá nhà hàng
    public float foodQualityScore = 5f; // Mặc định 5/10
    public float hygieneScore = 10f;    // Mặc định sạch sẽ 10/10
    public float decorationScore = 0f;  // Mặc định chưa trang trí 0/10
    public List<float> satisfactionHistory = new List<float>(); // Lưu queue 50 khách

    public GameSaveData(string fileName, int money)
    {
        this.saveFileName = fileName;
        this.rCredit = money;
    }
}