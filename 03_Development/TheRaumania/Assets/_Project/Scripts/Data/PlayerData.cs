using System;

using System.Collections.Generic;

public static class PlayerData
{
    private static int _rCredit = 1000000; // Tiền của người chơi

    // --- DỮ LIỆU ĐÁNH GIÁ (RATING) ---
    public static float foodQualityScore = 5f;
    public static float hygieneScore = 10f;
    public static float decorationScore = 0f;
    public static Queue<float> satisfactionHistory = new Queue<float>();

    public static int RCredit => _rCredit;

    // Action thông báo cho các UI biết mỗi khi tiền thay đổi
    public static event Action<int> OnCreditChanged;

    public static void SetCredit(int amount)
    {
        _rCredit = amount;
        OnCreditChanged?.Invoke(_rCredit);
    }

    public static void AddCredit(int amount)
    {
        _rCredit += amount;
        OnCreditChanged?.Invoke(_rCredit);
    }

    public static bool SpendCredit(int amount)
    {
        if (_rCredit >= amount)
        {
            _rCredit -= amount;
            OnCreditChanged?.Invoke(_rCredit);
            return true; // Xài tiền thành công
        }
        return false; // Không đủ tiền
    }
}