using AudioSystem.Service;
using Zenject;

namespace AudioSystem.NoAudio
{
    public class NoAudioInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<IAudioService>().To<NoAudioService>().AsSingle();
        }
    }
}