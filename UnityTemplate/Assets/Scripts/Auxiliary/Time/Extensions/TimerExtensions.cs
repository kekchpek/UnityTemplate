using System;

namespace kekchpek.Auxiliary.Time.Extensions
{
    public static class TimerExtensions
    {
        
        /// <summary>
        /// Correct the time to fit the start-end hour span.
        /// If time doesn't fit start-end span, time will be translated to nearest future time inside the span
        /// </summary>
        /// <param name="timestampUtc">Utc timestamp to correct in local time.</param>
        /// <param name="startNightHour">Min available time span border.</param>
        /// <param name="endNightHour">Max available time span border.</param>
        /// <returns>Returns corrected local date-time.</returns>
        public static DateTime CheckNightTimeGetCorrectTime(this long timestampUtc, int startNightHour, int endNightHour)
        {
            var dateTime = new DateTime(timestampUtc, DateTimeKind.Utc).ToLocalTime();

            bool isNight = IsNight(dateTime, startNightHour, endNightHour);

            if (isNight)
            {
                var nightEndTimeSpan = new TimeSpan(endNightHour, 0, 0);
                var timeDifference = nightEndTimeSpan - dateTime.TimeOfDay;
                
                if (timeDifference.Ticks < 0)
                {
                    dateTime = dateTime.AddDays(1);
                }

                dateTime = dateTime.Add(timeDifference);
                return dateTime;
            }

            return dateTime;
        }

        private static bool IsNight(DateTime localTime, int startHour, int endHour)
        {
            return localTime.Hour >= startHour || localTime.Hour < endHour;
        }

        public static void AddCallbackIn(this ITimeManager timeManager, long timestampDelta, Action callback)
        {
            timeManager.AddCallback(timeManager.CurrentTimestampUtc.Value + timestampDelta, callback);
        }
    }
}