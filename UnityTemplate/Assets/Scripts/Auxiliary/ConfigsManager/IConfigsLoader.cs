namespace kekchpek.Auxiliary.Configs
{
    public interface IConfigsLoader
    {
        void LoadConfigs(string path);
        void LoadDefaultConfigs();
    }
}
