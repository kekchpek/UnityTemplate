using Zenject;

namespace kekchpek.MVVM.Models.GameResources.Container
{
    public class ResourcesContainerFactory<T> : IFactory<IMutableResourcesContainer<T>>
    {
        private readonly IInstantiator _instantiator;

        public ResourcesContainerFactory(IInstantiator instantiator)
        {
            _instantiator = instantiator;
        }
        
        public IMutableResourcesContainer<T> Create()
        {
            return _instantiator.Instantiate<ResourcesContainer<T>>();
        }
    }
}