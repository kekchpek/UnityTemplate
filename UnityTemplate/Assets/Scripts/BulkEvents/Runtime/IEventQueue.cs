using System;

namespace BulkEvents
{
    public interface IEventQueue<T> : IEventQueue where T : unmanaged
    {
        void EnqueueEvent(T eventData);
        ReadOnlySpan<T> FlushEvents();
    }

    public interface IEventQueue
    {
        bool Clear();
        bool Any();
    }
}