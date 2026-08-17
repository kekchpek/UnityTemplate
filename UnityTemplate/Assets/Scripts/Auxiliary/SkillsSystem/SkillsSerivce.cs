using System;
using Cysharp.Threading.Tasks;
using GameResources.Domain;
using kekchpek.Auxiliary.Collections;
using kekchpek.Auxiliary.Configs;
using kekchpek.MVVM.Models.GameResources;
using kekchpek.MVVM.Models.GameResources.Prices;
using SkillsSystem.Data;
using UnityEngine;

namespace SkillsSystem 
{
    public class SkillsService : ISkillsService, IDisposable
    {

        public event Action<string, int> SkillUpgraded;

        private Action _disposeAction;

        private readonly HyperList<Action<ISkillSource>> _skillSourcesOperations = new();

        private readonly IConfigsProvider _configsProvider;

        private readonly IResourcesService<float> _resourcesService; 

        private readonly IPriceFactory<float> _priceFactory;

        private readonly ISkillsMutableModel _model;

        public SkillsService(
            IConfigsProvider configsProvider,
            IResourcesService<float> resourcesService,
            IPriceFactory<float> priceFactory,
            ISkillsMutableModel skillsModel)
        {
            _configsProvider = configsProvider;
            _resourcesService = resourcesService;
            _priceFactory = priceFactory;
            _model = skillsModel;
        }

        public async UniTask Initialize()
        {
            var config = await _configsProvider.GetConfigAsync<SkillsConfig>();
            _model.SetConfig(config);
        }

        public void RegisterSkill(ISkillSource skillSource)
        {
            skillSource = new MaxLevelRestrictedSkillSource(_model.GetConfigUpgradesCount(skillSource.SkillId) + 1, skillSource);
            if (!_model.AddSkillSource(skillSource.SkillId, skillSource))
            {
                Debug.LogError($"Skill {skillSource.SkillId} already registered");
            }
            void UpdatePrice()
            {
                UpdateSkillPrice(skillSource.SkillId);
            }
            skillSource.Level.Bind(UpdatePrice);
            skillSource.MaxLevel.Bind(UpdatePrice);
            _disposeAction += () => {
                skillSource.Level.Unbind(UpdatePrice);
                skillSource.MaxLevel.Unbind(UpdatePrice);
            };
            foreach (var operation in _skillSourcesOperations.GetReadOnlySpan())
            {
                operation(skillSource);
            }
        }

        private void UpdateSkillPrice(string skillId)
        {
            if (_model.GetSkillLevel(skillId).Value == _model.GetMaxLevel(skillId).Value)
            {
                _model.SetSkillPrice(skillId, null);
                return;
            }
            var skillResources = _model.GetSkillPrice(skillId, _model.GetSkillLevel(skillId).Value);
            Span<(ResourceId resId, float amount)> rawPrice = stackalloc (ResourceId, float)[skillResources.Length];
            for (int i = 0; i < skillResources.Length; i++)
            {
                rawPrice[i] = (ResourceId.FromString(skillResources[i].ResourceId), skillResources[i].Amount);
            }
            var price = _priceFactory.CreatePriceHandle(rawPrice);
            _model.SetSkillPrice(skillId, price);
        }

        public void ApplySkillSourcesOperation(Action<ISkillSource> operation)
        {
            foreach (var skillSource in _model.GetSkillSources())
            {
                operation(skillSource);
            }
            _skillSourcesOperations.Add(operation);
        }
        
        public void ClearSkill(ISkillSource skillSource)
        {
            if (!_model.RemoveSkillSource(skillSource.SkillId, skillSource))
            {
                Debug.LogError($"Skill {skillSource.SkillId} not found");
            }
        }

        public bool UpgradeSkill(string skillId)
        {
            if (!_model.TryGetSkillSource(skillId, out var skillSource))
            {
                Debug.LogError($"Skill source not registered for skill {skillId}");
                return false;
            }
            if (skillSource.Level.Value >= skillSource.MaxLevel.Value)
            {
                return false;
            }
            var skillPrice = _model.GetSkillPrice(skillId, skillSource.Level.Value);
            Span<(ResourceId resourceId, float amount)> skillPriceToSpend = stackalloc (ResourceId, float)[skillPrice.Length];
            for (int i = 0; i < skillPrice.Length; i++)
            {
                skillPriceToSpend[i] = (ResourceId.FromString(skillPrice[i].ResourceId), skillPrice[i].Amount);
            }
            if (!_resourcesService.TryToSpend(skillPriceToSpend))
            {
                return false;
            }

            var newLevel = skillSource.Level.Value + 1;
            skillSource.SetLevel(newLevel);
            
            Debug.Log($"[SkillTree] {skillId} now at level {newLevel}");
            SkillUpgraded?.Invoke(skillId, newLevel);
            return true;
        }

        public void SetSkillLevel(string skillId, int level)
        {
            if (!_model.TryGetSkillSource(skillId, out var skillSource))
            {
                Debug.LogError($"Skill source not registered for skill {skillId}");
                return;
            }
            skillSource.SetLevel(level);
        }

        public void Dispose()
        {
            foreach (var price in _model.GetSkillSources())
            {
                if (price is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _disposeAction?.Invoke();
        }
    }
}
