using System;
using AsyncReactAwait.Bindable;
using AsyncReactAwait.Promises;

namespace kekchpek.Auxiliary.Time.Mock
{
    public class MockTimeManager : ITimeManager
    {
        public IBindable<long> CurrentTimestampUtc => new Mutable<long>();

        public long CurrentTimestampLocal => CurrentTimestampUtc.Value + LocalTimeOffset;

        public DateTime NowUtc => new(CurrentTimestampUtc.Value);

        public DateTime NowLocal => new(CurrentTimestampLocal);

        public long TimestampSinceStart => (long)(UnityEngine.Time.unscaledTimeAsDouble * TimeSpan.TicksPerSecond);

        public long LocalTimeOffset => (DateTime.Now - DateTime.UtcNow).Ticks;

        public void AddCallback(long timestampUtc, Action callback)
        {
        }

        public IPromise Await(float seconds)
        {
            var promise = new ControllablePromise();
            promise.Success();
            return promise;
        }

        public void RemoveCallback(long timestampUtc, Action callback)
        {
        }

        public void RemoveCallback(Action callback)
        {
        }

        public DateTime TimestampToLocalTime(long timestampUtc)
        {
            return new DateTime(timestampUtc, DateTimeKind.Utc).ToLocalTime();
        }
    }
}