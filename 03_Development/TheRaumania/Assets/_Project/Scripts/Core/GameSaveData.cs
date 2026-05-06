using System;

[Serializable]
public class GameSaveData
{
    public string saveFileName; 
    public int rCredit;

    public GameSaveData(string fileName, int money)
    {
        this.saveFileName = fileName;
        this.rCredit = money;
    }
}