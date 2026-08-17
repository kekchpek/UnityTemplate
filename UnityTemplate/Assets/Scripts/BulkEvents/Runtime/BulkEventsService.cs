using System;
using System.Collections.Generic;
using UnityEngine;

namespace BulkEvents
{
    public class BulkEventsService : IBulkEventsService
    {

        private readonly Dictionary<Type, IEventQueue> _eventQueues = new();

        public IEventQueue<T> GetEventQueue<T>() where T : unmanaged
        {
            if (!_eventQueues.TryGetValue(typeof(T), out var eventQueue))
            {
                eventQueue = new EventQueue<T>();
                _eventQueues.Add(typeof(T), eventQueue);
            }
            return (IEventQueue<T>)eventQueue;
        }

        public bool ClearAllEvents()
        {
            bool anyCleared = false;
            foreach (var queue in _eventQueues)
            {
                anyCleared |= queue.Value.Clear();
            }
            return anyCleared;
        }

        public bool AnyEvents()
        {
            foreach (var queue in _eventQueues)
            {
                if (queue.Value.Any())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
