using System.Linq;
using AssetsSystem;
using kekchpek.Localization;
using UnityEngine;
using UnityMVVM.DI;
using UnityMVVM.ViewModelCore.PrefabsProvider;
using Zenject;

namespace DI.Core
{
    public class MVVMInstaller : MonoInstaller
    {
        [SerializeField] 
        private Transform[] _viewLayers;
        
        public override void InstallBindings()
        {
            Container.UseAsMvvmContainer(_viewLayers.Select(x => (x.name, x)).ToArray());
            Container.FastBind<IViewsPrefabsProvider, AssetsViewsPrefabsProvider>();
            Container.ProvideAccessForViewLayer<ILocalizationModel>();
            Container.ProvideAccessForViewModelLayer<ILocalizationModel>();
            Container.ProvideAccessForViewModelLayer<ILocalizationService>();
            Container.ProvideAccessForViewLayer<IAssetsModel>();
            Container.ProvideAccessForViewModelLayer<IAssetsModel>();
        }
    }
}