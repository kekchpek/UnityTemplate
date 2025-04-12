using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diagnostics.Time
{
    public sealed class DefaultTimeDebugger : ITimeDebugger
    {
        private struct TimeMeasurementBenchmark
        {
            private readonly string _name;
            private readonly DateTime _startTime;
            private DateTime? _endTime;

            private TimeMeasurementBenchmark(string name, DateTime startTime)
            {
                _name = name;
                _startTime = startTime;
                _endTime = null;
            }

            public static TimeMeasurementBenchmark Start(string name)
            {
                return new TimeMeasurementBenchmark(name, DateTime.UtcNow);
            }

            public TimeSpan Complete()
            {
                _endTime = DateTime.UtcNow;
                return _endTime.Value - _startTime;
            }

            public void Log()
            {
                Debug.Log(_endTime.HasValue
                    ? $"Time benchmark for {_name}: {(_endTime.Value - _startTime).TotalSeconds:0.000}s ({_startTime} - start; {_endTime.Value} - end)"
                    : $"Time measurement for {_name}: incompleted ({_startTime} - start)");
            }

        }

        private static readonly Dictionary<string, TimeMeasurementBenchmark> Benchmarks = new();
        
        public TimeMeasurementHandle StartMeasure(string blockName)
        {
            if (Benchmarks.ContainsKey(blockName))
            {
                Debug.LogError($"Previous time measuring for block {blockName} wasn't completed, but new one started.");
                return default;
            }
            Benchmarks.Add(blockName, TimeMeasurementBenchmark.Start(blockName));
            return new TimeMeasurementHandle(blockName);
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        // ReSharper disable once VirtualMemberNeverOverridden.Global
        private TimeSpan? EndMeasureInternal(string blockName)
        {
            if (Benchmarks.Remove(blockName, out var benchmark))
            {
                var time = benchmark.Complete();
                benchmark.Log();
                return time;
            }
            
            Debug.LogError($"Time measuring block \"{blockName}\" was not started");
            return null;
        } 

        public void EndMeasure(string blockName)
        {
            EndMeasureInternal(blockName);
        }
    }
}