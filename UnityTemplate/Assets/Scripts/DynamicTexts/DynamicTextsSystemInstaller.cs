using UnityMVVM.DI;
using Zenject;

namespace kekchpek.DynamicTexts
{
    public class DynamicTextsSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.FastBind<IReactiveTextService, ReactiveTextService>();
        }
    }
}
