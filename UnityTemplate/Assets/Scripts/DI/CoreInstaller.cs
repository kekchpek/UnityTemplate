using System.Linq;
using UnityEngine;
using UnityMVVM.DI;
using Zenject;

namespace DI
{
    public class CoreInstaller : MonoInstaller
    {

        [SerializeField] 
        private Transform[] _viewLayers;
        
        public override void InstallBindings()
        {
            Container.UseAsMvvmContainer(_viewLayers.Select(x => (x.name, x)).ToArray());
        }
    }
}