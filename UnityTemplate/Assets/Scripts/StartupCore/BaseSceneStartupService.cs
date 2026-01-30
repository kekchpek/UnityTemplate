using Cysharp.Threading.Tasks;

namespace Startup.Core
{
    public abstract class BaseSceneStartupService : IStartupService
    {

        private readonly IProjectStartupService _projectStartupService;

        public BaseSceneStartupService(
            IProjectStartupService projectStartupService
        )
        {
            _projectStartupService = projectStartupService;
        }

        public async UniTask Startup()
        {
            if (!_projectStartupService.IsCompleted)
            {
                await _projectStartupService.Startup();
            }
            await SceneStartup();
        }

        protected abstract UniTask SceneStartup();
    }
}