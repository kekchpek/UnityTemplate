using System;
using System.Collections.Generic;
using GMConsole;
using UnityEngine;
using UnityEngine.Pool;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources
{
    public class ResourcesService : IResourcesService, IDisposable
    {

        private const string SetResCommand = "SetRes";

        private readonly IResourcesMutableModel _model;
        private readonly IGameMasterCommandRegistry _gameMasterCommandRegistry;
        
        public ResourcesService(
            IResourcesMutableModel model,
            IGameMasterCommandRegistry gameMasterCommandRegistry)
        {
            _model = model;
            _gameMasterCommandRegistry = gameMasterCommandRegistry;
            _gameMasterCommandRegistry.RegisterCommand(
                SetResCommand, "Устанавливает значение ресурса. Пример: \"SetRes Gold 99999\"",
                SetResourceCommand);
        }

        public bool TryToSpend(IEnumerable<(ResourceId resourceId, float amount)> resources)
        {
            var idsSet = HashSetPool<ResourceId>.Get();
            var resList = ListPool<(ResourceId id, float amount)>.Get(); // to not enumerate twice.
            try
            {
                foreach (var resData in resources)
                {
                    if (idsSet.Contains(resData.resourceId))
                        throw new ArgumentException("Resources to spend should be unique. " +
                                                    $"There is an attempt to spend {resData.resourceId} twice");
                    idsSet.Add(resData.resourceId);
                    var val = _model.GetResource(resData.resourceId).Value;
                    if (val < resData.amount)
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
                ListPool<(ResourceId id, float amount)>.Release(resList);
                HashSetPool<ResourceId>.Release(idsSet);
            }
        }

        public bool TryToSpend(ResourceId resourceId, float amount)
        {
            if (_model.GetResource(resourceId).Value >= amount)
            {
                Reduce(resourceId, amount);
                return true;
            }

            return false;
        }

        public void Reduce(ResourceId resourceId, float amount)
        {
            var val = _model.GetResource(resourceId).Value;
            _model.SetResource(resourceId, Mathf.Max(val - amount, 0f));
        }

        public void Add(ResourceId resourceId, float amount)
        {
            var val = _model.GetResource(resourceId).Value;
            _model.SetResource(resourceId, val + amount);
        }

        private void SetResourceCommand(GMArgs args) {
            _model.SetResource(ResourceId.FromString(args.GetString()), args.GetFloat());
        }

        public void Dispose()
        {
            _gameMasterCommandRegistry.UnregisterCommand(SetResCommand);
        }
    }
}
