using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Diagnostics.Time
{
    public sealed class DefaultTimeDebugger : ITimeDebugger
    {
        private struct TimeMeasurementBenchmark
        {
            private readonly string _name;
            private readonly long _startTimestamp;
            private TimeSpan _elapsedTime;

            private TimeMeasurementBenchmark(string name)
            {
                _name = name;
                _elapsedTime = TimeSpan.Zero;
                _startTimestamp = Stopwatch.GetTimestamp();
            }

            public static TimeMeasurementBenchmark Start(string name) => new TimeMeasurementBenchmark(name);

            public TimeSpan Complete()
            {
                long endTimestamp = Stopwatch.GetTimestamp();
                _elapsedTime = TimeSpan.FromTicks((endTimestamp - _startTimestamp) * TimeSpan.TicksPerSecond / Stopwatch.Frequency);
                return _elapsedTime;
            }

            public void Log()
            {
                UnityEngine.Debug.Log($"Time benchmark for {_name}: {_elapsedTime.TotalSeconds:0.000}s");
            }
        }

        private static readonly Dictionary<string, TimeMeasurementBenchmark> Benchmarks = new();
        
        public TimeMeasurementHandle StartMeasure(string blockName)
        {
            if (Benchmarks.ContainsKey(blockName))
            {
                UnityEngine.Debug.LogError($"Previous time measuring for block {blockName} wasn't completed, but new one started.");
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
            
            UnityEngine.Debug.LogError($"Time measuring block \"{blockName}\" was not started");
            return null;
        } 

        public void EndMeasure(string blockName)
        {
            EndMeasureInternal(blockName);
        }
    }
}