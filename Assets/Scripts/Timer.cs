#nullable enable

using System;
using System.Threading.Tasks;
using UnityEngine;

public class Timer
{
    public event Action? TimerElapsed;

    public bool IsTimerElapsed { get; private set; } = true;

    public void StartTimerSeconds(float seconds)
    {
        IsTimerElapsed = false;
        Task.Run(async () =>
        {
            Debug.Log($"Started waiting for {seconds} Second(s).");
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            Debug.Log("Waiting over.");
            IsTimerElapsed = true;
            TimerElapsed?.Invoke();
        });
    }
}
