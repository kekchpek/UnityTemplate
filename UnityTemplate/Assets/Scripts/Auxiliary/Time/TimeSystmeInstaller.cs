using UnityMVVM.DI;
using Zenject;

namespace kekchpek.Auxiliary.Time
{
    public class TimeSystmeInstaller : Installer    
    {
        public override void InstallBindings()
        {
            Container.Bind<ITimeManager>().To<TimeManager>().FromNewComponentOnNewGameObject().AsSingle();
            Container.ProvideAccessForViewModelLayer<ITimeManager>();
            Container.ProvideAccessForViewLayer<ITimeManager>();
        }
    }
}