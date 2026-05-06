using UnityEngine;
using System;

public abstract class MinigameBase : MonoBehaviour
{
    public event Action<float> OnStepDone;
    public abstract MinigameType GetMinigameType();

    public virtual void StartGame(float freshness)
    {
        CookingEvents.OnMinigameStarted?.Invoke(GetMinigameType());
    }

    protected void Complete(float score)
    {
        CookingEvents.OnMinigameCompleted?.Invoke(GetMinigameType());
        OnStepDone?.Invoke(score);
    }
}