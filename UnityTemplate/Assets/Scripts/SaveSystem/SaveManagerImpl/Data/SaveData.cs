namespace kekchpek.GameSaves.Data
{
    public class SaveData
    {
        public string ProfileName { get; }
        public int Level { get; }
        public int Day { get; }

        public SaveData(string profileName, int level, int day)
        {
            ProfileName = profileName;
            Level = level;
            Day = day;
        }
        
    }
}