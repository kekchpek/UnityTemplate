using Cysharp.Threading.Tasks;

namespace kekchpek.Auxiliary.Configs
{
    public interface IConfigsProvider
    {
        T GetConfig<T>();
        UniTask<T> GetConfigAsync<T>();
    }
}
 