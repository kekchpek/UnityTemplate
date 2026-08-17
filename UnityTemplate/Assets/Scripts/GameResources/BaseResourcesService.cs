using System;
using System.Collections.Generic;
using GameResources.Domain;
using GMConsole;
using kekchpek.MVVM.Models.GameResources.Container;
using UnityEngine.Pool;

namespace kekchpek.MVVM.Models.GameResources
{
    public abstract class BaseResourcesService<T> : IResourcesService<T>, IDisposable
    {
        private readonly IMutableResourcesContainer<T> _model;
        private readonly IGameMasterCommandRegistry _gameMasterCommandRegistry;
        private readonly string _setResourceCommand;

        public event Action<ResourceId, T> ResourceSpent;
        public event Action<ResourceId, T> ResourceAdded;

        protected BaseResourcesService(
            IMutableResourcesContainer<T> model,
            IGameMasterCommandRegistry gameMasterCommandRegistry,
            string setResourceCommand,
            string setResourceCommandDescription)
        {
            _model = model;
            _gameMasterCommandRegistry = gameMasterCommandRegistry;
            _setResourceCommand = setResourceCommand;
            _gameMasterCommandRegistry.RegisterCommand(
                setResourceCommand,
                setResourceCommandDescription,
                SetResourceCommand);
        }

        protected IMutableResourcesContainer<T> Model => _model;

        public virtual void Initialize()
        {
        }

        public bool TryToSpend(IEnumerable<(ResourceId resourceId, T amount)> resources)
        {
            var idsSet = HashSetPool<ResourceId>.Get();
            var resList = ListPool<(ResourceId id, T amount)>.Get();
            try
            {
                foreach (var resData in resources)
                {
                    if (idsSet.Contains(resData.resourceId))
                        throw new ArgumentException("Resources to spend should be unique. " +
                                                    $"There is an attempt to spend {resData.resourceId} twice");
                    idsSet.Add(resData.resourceId);
                    var val = _model.GetResource(resData.resourceId).Value;
                    if (Less(val, resData.amount))
                        return false;
                    resList.Add(resData);
                }

                foreach (var resData in resList)
                {
                    Reduce(resData.id, resData.amount);
                }

                return true;
            }
            finally
            {
                ListPool<(ResourceId id, T amount)>.Release(resList);
                HashSetPool<ResourceId>.Release(idsSet);
            }
        }

        public bool TryToSpend(ReadOnlySpan<(ResourceId resourceId, T amount)> resources)
        {
            var idsSet = HashSetPool<ResourceId>.Get();
            try
            {
                foreach (var resData in resources)
                {
                    if (idsSet.Contains(resData.resourceId))
                        throw new ArgumentException("Resources to spend should be unique. " +
                                                    $"There is an attempt to spend {resData.resourceId} twice");
                    idsSet.Add(resData.resourceId);
                    var val = _model.GetResource(resData.resourceId).Value;
                    if (Less(val, resData.amount))
                        return false;
                }

                // The span is already materialized, so a second pass is cheaper than buffering.
                foreach (var resData in resources)
                {
                    Reduce(resData.resourceId, resData.amount);
                }

                return true;
            }
            finally
            {
                HashSetPool<ResourceId>.Release(idsSet);
            }
        }

        public bool TryToSpend(ResourceId resourceId, T amount)
        {
            if (GreaterOrEqual(_model.GetResource(resourceId).Value, amount))
            {
                Reduce(resourceId, amount);
                return true;
            }

            return false;
        }

        public void Reduce(ResourceId resourceId, T amount)
        {
            var val = _model.GetResource(resourceId).Value;
            _model.SetResource(resourceId, ClampToNonNegative(Subtract(val, amount)));
            ResourceSpent?.Invoke(resourceId, amount);
        }

        public void Add(ResourceId resourceId, T amount)
        {
            var val = _model.GetResource(resourceId).Value;
            _model.SetResource(resourceId, Add(val, amount));
            ResourceAdded?.Invoke(resourceId, amount);
        }

        protected abstract T Zero { get; }
        protected abstract T Add(T left, T right);
        protected abstract T Subtract(T left, T right);
        protected abstract bool GreaterOrEqual(T left, T right);
        protected abstract bool Less(T left, T right);
        protected abstract void SetResourceFromCommand(GMArgs args);

        protected T ClampToNonNegative(T value)
        {
            return Less(value, Zero) ? Zero : value;
        }

        private void SetResourceCommand(GMArgs args)
        {
            SetResourceFromCommand(args);
        }

        public void Dispose()
        {
            _gameMasterCommandRegistry.UnregisterCommand(_setResourceCommand);
        }
    }
}
