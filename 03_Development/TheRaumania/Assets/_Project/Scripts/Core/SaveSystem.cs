using UnityEngine;
using System.IO;

public static class SaveSystem
{
    private static string GetPath(int slot) => Application.persistentDataPath + $"/slot_{slot}.json";

    public static void Save(int slot, GameSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetPath(slot), json);
        Debug.Log($"[SaveSystem] Đã lưu file: {GetPath(slot)}");
    }

    public static GameSaveData Load(int slot)
    {
        if (File.Exists(GetPath(slot)))
        {
            string json = File.ReadAllText(GetPath(slot));
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        return null;
    }
}