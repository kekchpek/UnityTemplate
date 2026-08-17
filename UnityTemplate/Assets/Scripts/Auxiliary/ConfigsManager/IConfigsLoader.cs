using Cysharp.Threading.Tasks;

namespace kekchpek.Auxiliary.Configs
{
    public interface IConfigsLoader
    {
        void LoadConfigs(string path);

        UniTask LoadDefaultConfigsAsync();
    }
}
