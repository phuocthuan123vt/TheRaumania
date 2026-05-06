using System;

public static class PlayerData
{
    private static int _rCredit = 1000; // Tiền của người chơi

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