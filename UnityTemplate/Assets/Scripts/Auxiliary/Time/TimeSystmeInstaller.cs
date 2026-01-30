using Zenject;

namespace kekchpek.Auxiliary.Time
{
    public class TimeSystmeInstaller : Installer    
    {
        public override void InstallBindings()
        {
            Container.Bind<ITimeManager>().To<TimeManager>().FromNewComponentOnNewGameObject().AsSingle();
        }
    }
}