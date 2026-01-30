using UnityMVVM.DI;
using Zenject;

namespace kekchpek.Localization
{
    public class LocalizationSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.FastBind<ILocalizationService, LocalizationService>();
            Container.FastBind<ILocalizationMutableModel, ILocalizationModel, LocalizationModel>();
            Container.ProvideAccessForViewLayer<ILocalizationModel>();
        }
    }
}