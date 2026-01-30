using System;
using kekchpek.SteamApi.Achievements;
using kekchpek.SteamApi.Core;
using Zenject;

namespace kekchpek.SteamApi.DI
{
    public class SteamInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind(new[] { typeof(ISteamInitService), typeof(IDisposable) })
                .To<SteamInitService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<ISteamAchivementsInitializer>()
                .To<SteamAchievementsService>()
                .AsSingle();
        }
    }
}
