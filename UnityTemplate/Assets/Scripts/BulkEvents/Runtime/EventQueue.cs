using System;
using kekchpek.Auxiliary.Collections;

namespace BulkEvents
{
    public class EventQueue<T> : IEventQueue<T> where T : unmanaged
    {

        private HyperList<T> _events;
        private HyperList<T> _flushBuffer;

        public EventQueue()
        {
            _events = new HyperList<T>();
            _flushBuffer = new HyperList<T>();
        }

        public void EnqueueEvent(T eventData)
        {
            _events.Add(eventData);
        }

        public ReadOnlySpan<T> FlushEvents()
        {
            var newFlush = _events;
            _events = _flushBuffer;
            _flushBuffer = newFlush;
            _events.ClearWithoutReleasingGcReferences();
            return _flushBuffer.GetReadOnlySpan();
        }

        public bool Clear()
        {
            var anyCleared = _events.Count > 0;
            _events.ClearWithoutReleasingGcReferences();
            return anyCleared;
        }

        public bool Any()
        {
            return _events.Count > 0;
        }
    }
}