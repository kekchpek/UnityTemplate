using System;
using AsyncReactAwait.Bindable;
using AsyncReactAwait.Promises;

namespace kekchpek.Auxiliary.Time
{
    public interface ITimeManager
    {
        IBindable<long> CurrentTimestampUtc { get; }
        long CurrentTimestampLocal { get; }
        DateTime NowUtc { get; }
        DateTime NowLocal { get; }
        long TimestampSinceStart { get; }
        long LocalTimeOffset { get; }
        void AddCallback(long timestampUtc, Action callback);
        void RemoveCallback(long timestampUtc, Action callback);
        void RemoveCallback(Action callback);
        DateTime TimestampToLocalTime(long timestampUtc);
        IPromise Await(float seconds);
    }
}