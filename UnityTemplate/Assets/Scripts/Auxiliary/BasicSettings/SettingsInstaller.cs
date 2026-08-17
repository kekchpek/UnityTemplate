using System;
using UnityMVVM.DI;
using Zenject;

namespace BasicSettings
{
    public class SettingsInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<ISettingsModel>().To<SettingsModel>().AsSingle();
            Container.Bind(new[] { typeof(ISettingsService), typeof(IDisposable) })
                .To<SettingsService>().AsSingle();
            Container.ProvideAccessForViewModelLayer<ISettingsService>();
            Container.ProvideAccessForViewModelLayer<ISettingsModel>();
        }
    }
}
