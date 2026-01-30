using Zenject;

namespace kekchpek.GameSaves
{
    public class NoSaveInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind(typeof(IGameSaveManager), 
                           typeof(IGameSaveController)).To<NoSaveManager>()
                           .AsSingle();
        }
    }
}

