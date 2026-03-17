namespace kekchpek.MVVM.Models.GameResources.Static
{
    public static class ResourceFormatting
    {
        public static string FormatNumber(double value)
        {
            double absValue = System.Math.Abs(value);
            
            if (absValue < 10000)
            {
                return ((int)value).ToString();
            }
            else if (absValue < 1_000_000)
            {
                double divided = value / 1000.0;
                return divided % 1 == 0 ? $"{(int)divided}K" : $"{divided:F1}K";
            }
            else if (absValue < 1_000_000_000)
            {
                double divided = value / 1_000_000.0;
                return divided % 1 == 0 ? $"{(int)divided}M" : $"{divided:F1}M";
            }
            else
            {
                double divided = value / 1000000000.0;
                return divided % 1 == 0 ? $"{(int)divided}B" : $"{divided:F1}B";
            }
        }
    }
}