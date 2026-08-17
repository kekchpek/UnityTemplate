using System;
using Zenject;

namespace kekchpek.Auxiliary.Configs
{
    public class ConfigsManagerInstaller : Installer
    {
        public override void InstallBindings()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Container.Bind(new Type[] { typeof(IConfigsProvider), typeof(IConfigsLoader) }).To<WebConfigsManager>().AsSingle();
#else
            Container.Bind(new Type[] { typeof(IConfigsProvider), typeof(IConfigsLoader) }).To<FileConfigsManager>().AsSingle();
#endif
        }
    }
}
