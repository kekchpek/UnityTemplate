using System;
using AsyncReactAwait.Bindable;
using TMPro;
using UnityEngine;

namespace AuxiliaryComponents.Timer
{
    public class ReactiveTimerLabel : MonoBehaviour
    {

        private TimeSpan _setTime;
        private string _setString;
        
        [SerializeField] private TMP_Text _text;
        private IBindable<TimeSpan?> _timer;

        private void OnValidate()
        {
            _text = GetComponent<TMP_Text>();
        }

        public void Init(IBindable<TimeSpan?> timer)
        {
            Deinit();
            _timer = timer;
            
            _timer.Bind(OnTimerChanged);
        }

        public void Deinit()
        {
            _timer?.Unbind(OnTimerChanged);
        }

        private void OnTimerChanged(TimeSpan? timer)
        {
            if (!timer.HasValue)
            {
                return;
            }
            
            var formatTime = TimeFormat(timer.Value);
            
            _text.text = formatTime;
        }
        
        private string TimeFormat(TimeSpan timeSpan)
        {
            string formatText = "";

            if (timeSpan.TotalDays >= 1d)
            {
                if (_setTime.TotalDays > 0 && _setTime.Hours == timeSpan.Hours && _setTime.Days == timeSpan.Days)
                    return _setString;
                formatText = $"{timeSpan.Days}d {timeSpan.Hours}h";
            }

            if (timeSpan is { TotalDays: < 1d, TotalHours: > 1 })
            {
                if (_setTime is { TotalDays: < 1d, TotalHours: > 1 } && _setTime.Hours == timeSpan.Hours && _setTime.Minutes == timeSpan.Minutes)
                    return _setString;
                formatText = $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            }

            if (timeSpan.TotalHours <= 1)
            {
                if (_setTime.TotalHours <= 1 && _setTime.Minutes == timeSpan.Minutes && _setTime.Seconds == timeSpan.Seconds)
                    return _setString;
                formatText = $"{timeSpan.Minutes}:{timeSpan.Seconds:00}";
            }

            _setTime = timeSpan;
            _setString = formatText;
            return formatText;
        }
    }
}