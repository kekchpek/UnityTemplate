using System;
using System.Collections.Generic;
using System.Linq;
using AsyncReactAwait.Bindable;
using AsyncReactAwait.Bindable.BindableExtensions;
using GameResources.Domain;
using UnityEngine;

namespace kekchpek.MVVM.Models.GameResources.Container
{
    public class ResourcesContainer<T> : IMutableResourcesContainer<T>
    {

        public event Action<ResourceId, T> ResourceChanged;

        private readonly Dictionary<ResourceId, IMutable<T>> _resources = new();

        private Dictionary<ResourceId, IMutable<T>> Resources => _resources;


        public IEnumerable<ResourceId> GetKnownResources()
        {
            return Resources.Keys;
        }

        public IEnumerable<ResourceId> GetNonZeroResources()
        {
            return Resources.Keys.Where(k => !EqualityComparer<T>.Default.Equals(Resources[k].Value, default)).ToArray();
        }

        public void RegisterResource(ResourceId resourceId)
        {
            if (_resources.ContainsKey(resourceId))
            {
                Debug.LogError($"Resource {resourceId} already registered in this container.");
                return;
            }
            _resources.Add(resourceId, CreateResourceValue(resourceId));
        }
        
        public IBindable<T> GetResource(ResourceId resourceId)
        {
            if (!_resources.ContainsKey(resourceId))
            {
                Debug.LogError($"Resource {resourceId} not found in this container.");
                return new Mutable<T>(default);
            }
            return _resources[resourceId];
        }

        public void SetResource(ResourceId resourceId, T value)
        {
            if (!_resources.ContainsKey(resourceId))
            {
                Debug.LogError($"Resource {resourceId} not found in this container.");
                return;
            }
            var bindable = _resources[resourceId];
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (EqualityComparer<T>.Default.Equals(bindable.Value, value))
                return;
            _resources[resourceId].Set(value);
            ResourceChanged?.Invoke(resourceId, value);
            
        }

        protected virtual IMutable<T> CreateResourceValue(ResourceId id)
        {
            return new Mutable<T>();
        }

        public bool HasResource(ResourceId resourceId)
        {
            return _resources.ContainsKey(resourceId);
        }
    }
}