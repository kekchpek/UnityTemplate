using System;

namespace AuxiliaryComponents.Timer
{
    public static class TimerExtensions
    {
        public static void SetTimer(this ITimerLabel timerLabel, float timeInSec)
        {
            timerLabel.SetTimerTimestamp(DateTime.UtcNow.AddSeconds(timeInSec).Ticks);
        }
    }
}