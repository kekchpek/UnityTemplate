using System;
using BigInteger = System.Numerics.BigInteger;
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
            Container.Bind(new Type[] { typeof(IResourcesMutableModel), typeof(IResourcesModel), typeof(IResourcesContainer<float>) }).To<ResourcesModel>().AsSingle();
            Container.Bind(new Type[] { typeof(IFloatResourcesService), typeof(IResourcesService<float>), typeof(IDisposable) }).To<ResourcesService>().AsSingle();
            Container.Bind(new Type[] { typeof(ILargeResourcesMutableModel), typeof(ILargeResourcesModel), typeof(IResourcesContainer<BigInteger>) }).To<LargeResourcesModel>().AsSingle();
            Container.Bind(new Type[] { typeof(ILargeResourcesService), typeof(IResourcesService<BigInteger>) }).To<LargeResourcesService>().AsSingle();
            Container.Bind<IPriceFactory<float>>().To<PriceFactory<float>>().AsSingle();
            Container.Bind<IPriceFactory<BigInteger>>().To<PriceFactory<BigInteger>>().AsSingle();
            Container.Bind<IFactory<IMutableResourcesContainer<float>>>().To<ResourcesContainerFactory<float>>().AsSingle();
            Container.Bind<IFactory<IMutableResourcesContainer<BigInteger>>>().To<ResourcesContainerFactory<BigInteger>>().AsSingle();
        }
    }
}