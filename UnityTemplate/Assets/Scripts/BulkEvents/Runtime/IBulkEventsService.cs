namespace BulkEvents
{
    public interface IBulkEventsService
    {
        IEventQueue<T> GetEventQueue<T>() where T : unmanaged;

        /// <summary>
        /// Return true if there are any events to clear, false otherwise.
        /// </summary>
        /// <returns></returns>
        bool ClearAllEvents();

        /// <summary>
        /// Return true if there are any events, false otherwise.
        /// </summary>
        /// <returns></returns>
        bool AnyEvents();
    }
}