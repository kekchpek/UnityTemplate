namespace Diagnostics.Time
{
    public interface ITimeDebugger
    {
        TimeMeasurementHandle StartMeasure(string blockName);
        void EndMeasure(string blockName);
    }
}