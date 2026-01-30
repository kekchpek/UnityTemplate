using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;
using AsyncReactAwait.Bindable.BindableExtensions;
using AsyncReactAwait.Promises;
using UnityEngine;

namespace kekchpek.Auxiliary.Time
{
    public class TimeManager : MonoBehaviour, ITimeManager
    {
        private long _startupTimestampUtc;
        private long? _localTimeOffset;

        private readonly IMutable<long> _currentTimestampUtc = new Mutable<long>();
        private readonly SortedList<long, List<Action>> _callbacks = new();
        
        private long CurrentTimestampUtcInternal
        {
            get
            {
                if (_startupTimestampUtc != 0)
                {
                    return _startupTimestampUtc + TimestampSinceStart;
                }

                return DateTime.UtcNow.Ticks;
            }
        }

        public IBindable<long> CurrentTimestampUtc
        {
            get
            {
                if (_currentTimestampUtc.Value == 0)
                {
                    _currentTimestampUtc.Value = CurrentTimestampUtcInternal;
                }
                return _currentTimestampUtc;
            }
        }

        public long CurrentTimestampLocal => CurrentTimestampUtcInternal + LocalTimeOffset;

        public DateTime NowUtc => new(CurrentTimestampUtcInternal);
        
        public DateTime NowLocal => new(CurrentTimestampLocal);

        public long TimestampSinceStart => (long)(UnityEngine.Time.unscaledTimeAsDouble * TimeSpan.TicksPerSecond);

        public long LocalTimeOffset
        {
            get
            {
                _localTimeOffset ??= (DateTime.Now - DateTime.UtcNow).Ticks;
                return _localTimeOffset.Value;
            }
        }
        
        public void AddCallback(long timestampUtc, Action callback)
        {
            if (callback == null)
                return;
            
            Debug.Log($"Delayed callback submitted: {callback.Method.Name} with time = {new DateTime(timestampUtc)}");
            
            if (timestampUtc < CurrentTimestampUtcInternal)
            {
                Debug.Log($"Invoke delayed callback: {callback.Method.Name}");
                callback.Invoke();
                return;
            }
            
            if (!_callbacks.ContainsKey(timestampUtc))
            {
                _callbacks.Add(timestampUtc, new List<Action>());
            }
            _callbacks[timestampUtc].Add(callback);
        }

        public void RemoveCallback(long timestampUtc, Action callback)
        {
            if (_callbacks.TryGetValue(timestampUtc, out var callbacks))
            {
                callbacks.Remove(callback);
            }
        }

        public void RemoveCallback(Action callback)
        {
            foreach (var kvp in _callbacks)
            {
                kvp.Value.Remove(callback);
            }
        }

        public DateTime TimestampToLocalTime(long timestampUtc)
        {
            return new DateTime(timestampUtc, DateTimeKind.Utc).ToLocalTime();
        }

        public IPromise Await(float seconds)
        {
            var promise = new ControllablePromise();
            AddCallback(
                CurrentTimestampUtcInternal + (long)(seconds * TimeSpan.TicksPerSecond),
                () => promise.Success());
            return promise;
        }

        private void Update()
        {
            if (_startupTimestampUtc == 0)
            {
                _startupTimestampUtc = DateTime.UtcNow.Ticks - TimestampSinceStart;
                Debug.Log($"Application origin time = {new DateTime(_startupTimestampUtc)}");
            }

            var currentTimeStamp = CurrentTimestampUtcInternal;
            _currentTimestampUtc.Set(currentTimeStamp);
            if (_callbacks.Count > 0) 
            {
                (long key, List<Action> value) kvp;
                while (_callbacks.Count > 0 && (kvp = (_callbacks.Keys[0], _callbacks[_callbacks.Keys[0]])).key < currentTimeStamp)
                {
                    var callbacksCollection = new Action[kvp.value.Count];
                    kvp.value.CopyTo(callbacksCollection);
                    foreach (var callback in callbacksCollection)
                    {
                        Debug.Log($"Invoke delayed callback: {callback.Method.Name}");
                        callback.Invoke();
                    }

                    _callbacks.Remove(kvp.key);
                }
            }
        }
    }
}