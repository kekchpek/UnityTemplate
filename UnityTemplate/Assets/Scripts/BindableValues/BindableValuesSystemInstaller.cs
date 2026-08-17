using System;
using UnityMVVM.DI;
using Zenject;

namespace kekchpek.BindableValues
{
    public class BindableValuesSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind(new Type[]
            {
                typeof(IBindableValueFactory),
                typeof(IBindableValueRegistry),
            }).To<BindableValueFactory>().AsSingle();
        }
    }
}
