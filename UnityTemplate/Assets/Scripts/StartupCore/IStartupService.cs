using Cysharp.Threading.Tasks;

namespace Startup.Core
{
    public interface IStartupService
    {
        UniTask Startup();
    }
}