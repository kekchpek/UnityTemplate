using System;

namespace Diagnostics.Time
{
    public readonly struct TimeMeasurementHandle : IDisposable
    {

        private readonly string _timeBlock;

        public TimeMeasurementHandle(string timeBlock)
        {
            _timeBlock = timeBlock;
        }
        
        public void Dispose()
        {
            if (_timeBlock != null)
            {
                TimeDebug.EndMeasure(_timeBlock);
            }
        }
    }
}