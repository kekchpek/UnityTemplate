using System;
using TMPro;
using UnityEngine;

namespace AuxiliaryComponents.Timer
{
    [RequireComponent(typeof(TMP_Text))]
    public class TimerLabel : MonoBehaviour, ITimerLabel
    {
        private TMP_Text _text;

        private long? _appStartTimeStamp;
        private long _timestamp;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        public void SetTimerTimestamp(long timestampUtc)
        {
            _timestamp = timestampUtc;
        }

        private void Update()
        {
            _appStartTimeStamp ??= DateTime.UtcNow.AddSeconds(-Time.realtimeSinceStartupAsDouble).Ticks;
            
            var currentTime = Time.realtimeSinceStartupAsDouble;

            var timeSpan = TimeSpan.FromSeconds(
                Math.Max(.0, (_timestamp - _appStartTimeStamp.Value) / (double)TimeSpan.TicksPerSecond) - currentTime
            );
            var format = TimeFormat(timeSpan);
            
            _text.text = format;
        }

        private string TimeFormat(TimeSpan timeSpan)
        {
            string formatText = "";
            
            if (timeSpan.TotalDays >= 1d)
            {
                formatText = $"{timeSpan.Days}d {timeSpan.Hours}h";
            }

            if (timeSpan.TotalDays < 1d && timeSpan.TotalHours > 1)
            {
                formatText = $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            }

            if (timeSpan.TotalHours <= 1)
            {
                formatText = $"{timeSpan.Minutes}:{timeSpan.Seconds:00}";
            }

            return formatText;
        }
    }
}