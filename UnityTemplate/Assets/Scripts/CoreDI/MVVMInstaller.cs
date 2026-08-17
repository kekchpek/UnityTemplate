using System.Linq;
using BigInteger = System.Numerics.BigInteger;
using AssetsSystem;
using GMConsole;
using kekchpek.Auxiliary.Application;
using kekchpek.Localization;
using kekchpek.MVVM.Models.GameResources;
using kekchpek.MVVM.Models.GameResources.Prices;
using kekchpek.MVVM.Models.GameResources.Views.SingleLargeResource;
using kekchpek.MVVM.Models.GameResources.Views.SingleResource;
using UnityEngine;
using UnityMVVM.DI;
using UnityMVVM.ViewModelCore.PrefabsProvider;
using Zenject;
using kekchpek.Auxiliary.Time;

namespace DI.Core
{
    public class MVVMInstaller : MonoInstaller
    {
        [SerializeField] 
        private Transform[] _viewLayers;
        
        public override void InstallBindings()
        {
            Container.UseAsMvvmContainer(_viewLayers.Select(x => (x.name, x)).ToArray());
            Container.InstallView<SingleResourceView, ISingleResourceViewModel, SingleResourceViewModel>();
            Container.InstallView<SingleLargeResourceView, ISingleLargeResourceViewModel, SingleLargeResourceViewModel>();
            Container.FastBind<IViewsPrefabsProvider, AssetsViewsPrefabsProvider>();
            Container.ProvideAccessForViewModelLayer<ITimeManager>();
            Container.ProvideAccessForViewModelLayer<IApplicationService>();
            Container.ProvideAccessForViewModelLayer<IResourcesModel>();
            Container.ProvideAccessForViewModelLayer<IFloatResourcesService>();
            Container.ProvideAccessForViewModelLayer<ILargeResourcesModel>();
            Container.ProvideAccessForViewModelLayer<ILargeResourcesService>();
            Container.ProvideAccessForViewModelLayer<IPriceFactory<float>>();
            Container.ProvideAccessForViewModelLayer<IPriceFactory<BigInteger>>();
            Container.ProvideAccessForViewLayer<ILocalizationModel>();
            Container.ProvideAccessForViewModelLayer<ILocalizationModel>();
            Container.ProvideAccessForViewModelLayer<ILocalizationService>();
            Container.ProvideAccessForViewLayer<IAssetsModel>();
            Container.ProvideAccessForViewModelLayer<IAssetsModel>();
            Container.ProvideAccessForViewModelLayer<IGameMasterCommandRegistry>();
        }
    }
}