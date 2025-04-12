namespace Diagnostics.Time
{
    public static class TimeDebug
    {

        private static ITimeDebugger TimeDebugger = new DefaultTimeDebugger();

        public static void SetDebugger(ITimeDebugger timeDebugger)
        {
            TimeDebugger = timeDebugger;
        }
        
        /// <summary>
        /// Starting time measurement for a process with name <paramref name="blockName"/>
        /// Keep in mind, that you can't start a time measurement the for same process several times without ends it.
        /// </summary>
        /// <param name="blockName">The name of a process to measure its execution time.</param>
        /// <returns>
        /// Returns a disposable handle for measurement process.
        /// Dispose it, when you want to end measurement.
        /// </returns>
        public static TimeMeasurementHandle StartMeasure(string blockName)
        {
            return TimeDebugger.StartMeasure(blockName);
        }

        /// <summary>
        /// Ends time measurement for specified process and logs the results.
        /// </summary>
        /// <param name="blockName">The name of process to end measurement.</param>
        public static void EndMeasure(string blockName)
        {
            TimeDebugger.EndMeasure(blockName);
        }
        
    }
}