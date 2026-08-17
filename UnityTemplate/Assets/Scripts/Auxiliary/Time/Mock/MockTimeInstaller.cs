using Zenject;

namespace kekchpek.Auxiliary.Time.Mock
{
    public class MockTimeInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<ITimeManager>().To<MockTimeManager>().AsSingle();
        }
    }
}