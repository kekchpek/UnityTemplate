namespace kekchpek.SaveSystem
{
    public interface IMultifileSaveDataProvider : ISaveDataProvider
    {

        void ReleaseFile(string fileName, bool saveBeforeRelease = true);

    }
}