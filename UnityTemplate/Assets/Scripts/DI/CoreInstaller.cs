using System.Linq;
using Startup;
using UnityEngine;
using UnityMVVM.DI;
using UnityMVVM.ViewModelCore.PrefabsProvider;
using Zenject;

namespace DI
{
    public class CoreInstaller : MonoInstaller
    {

        [SerializeField] 
        private Transform[] _viewLayers;
        
        public override void InstallBindings()
        {
            Container.UseAsMvvmContainer(_viewLayers.Select(x => (x.name, x)).ToArray());
            Container.Bind<IStartupService>().To<StartupService>().AsSingle().WhenInjectedInto<StartupBehaviour>();
            Container.FastBind<IViewsPrefabsProvider, AssetsViewsPrefabsProvider>();
            
            // Game Installers
            
        }
        
    }
}