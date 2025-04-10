using Cysharp.Threading.Tasks;

namespace Startup
{
    public interface IStartupService
    {
        UniTask Startup();
    }
}