using Cysharp.Threading.Tasks;

namespace Startup
{
    public class StartupService : IStartupService
    {
        public UniTask Startup()
        {
            return UniTask.CompletedTask;
        }
    }
}
