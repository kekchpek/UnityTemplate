namespace kekchpek.Auxiliary.Configs
{
    public interface IConfigsProvider
    {
        T GetConfig<T>(string configName);
    }
}
 