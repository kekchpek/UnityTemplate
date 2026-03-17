using kekchpek.MVVM.Models.GameResources.Container;
using kekchpek.MVVM.Models.GameResources.Prices;
using kekchpek.MVVM.Models.GameResources.Views.SingleResource;
using UnityMVVM.DI;
using Zenject;

namespace kekchpek.MVVM.Models.GameResources
{
    public class GameResourcesInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.FastBind<IResourcesMutableModel, IResourcesModel, ResourcesModel>();
            Container.FastBind<IPriceFactory, PriceFactory>();
            Container.FastBind<IResourcesService, ResourcesService>();
            Container.InstallView<SingleResourceView, ISingleResourceViewModel, SingleResourceViewModel>();
            Container.Bind<IFactory<IMutableResourcesContainer>>().To<ResourcesContainerFactory>().AsSingle();
        }
    }
}