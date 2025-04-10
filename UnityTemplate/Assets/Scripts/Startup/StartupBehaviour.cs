using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Startup
{
    public class StartupBehaviour : MonoBehaviour
    {
        
        private IStartupService _startupService;

        [Inject]
        public void Construct(IStartupService startupService)
        {
            _startupService = startupService;
        }

        private void Awake()
        {
            _startupService.Startup().Forget();
        }
        
    }
}