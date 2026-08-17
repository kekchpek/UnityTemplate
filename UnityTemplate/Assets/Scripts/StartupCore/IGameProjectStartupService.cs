using Cysharp.Threading.Tasks;

namespace Startup.Core
{
    public interface IGameProjectStartupService
    {
        UniTask Startup();
    }
}