using System;
using AudioSystem.Manager;
using AudioSystem.Service;
using UnityEngine;
using UnityMVVM.DI;
using Zenject;

namespace AudioSystem
{
    public class AudioInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<IAudioManager>().FromMethod(() =>
            {
                var audioManager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
                return audioManager;
            }).AsSingle().Lazy();
            
            Container.Bind(new [] { typeof(IAudioService)}).To<AudioService>().AsSingle();

            Container.ProvideAccessForViewLayer<IAudioService>();
            Container.ProvideAccessForViewModelLayer<IAudioService>();
        }
    }
}
