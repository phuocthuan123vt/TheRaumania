using UnityEngine;
using System;

public abstract class MinigameBase : MonoBehaviour
{
    public Action<float> OnStepDone;
    [SerializeField] protected GameObject minigamePanel;

    public abstract void StartGame(float freshness);

    protected void Complete(float score)
    {
        minigamePanel.SetActive(false);
        OnStepDone?.Invoke(score);
    }
}