using Zenject;

namespace kekchpek.GameSaves
{
    public class GameSavesInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind(typeof(IGameSaveManager), 
                           typeof(IGameSaveController)).To<GameSaveManager>()
                           .AsSingle();
        }
    }
}