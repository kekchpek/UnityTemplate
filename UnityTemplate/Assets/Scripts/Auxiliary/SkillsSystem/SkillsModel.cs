using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;
using kekchpek.Auxiliary.Collections;
using kekchpek.MVVM.Models.GameResources.Prices;
using SkillsSystem.Data;
using UnityEngine;

namespace SkillsSystem 
{
    public class SkillsModel : ISkillsMutableModel
    {

        private readonly HyperList<ISkillSource> _skillSourcesList = new();

        private readonly Dictionary<string, ISkillSource> _skillSources = new();

        private readonly Dictionary<string, SkillData> _skillsIndex = new();

        private readonly Dictionary<string, Mutable<IPrice>> _skillPrices = new();

        public void SetConfig(SkillsConfig config)
        {
            _skillsIndex.Clear();
            foreach (var skill in config.Skills)
            {
                _skillsIndex[skill.Id] = skill;
            }
        }

        public ReadOnlySpan<SkillResourceData> GetSkillPrice(string skillId, int level)
        {
            if (!_skillsIndex.TryGetValue(skillId, out var skill))
            {
                Debug.LogError($"Skill {skillId} not found");
                return ReadOnlySpan<SkillResourceData>.Empty;
            }
            if (level > skill.MaxUpgrades)
            {
                Debug.LogError($"Skill {skillId} level is greater than the max level to upgrade");
                return ReadOnlySpan<SkillResourceData>.Empty;
            }
            int initialLevel = 0;
            if (_skillSources.TryGetValue(skillId, out var skillSource))
            {
                initialLevel = skillSource.InitialLevel;
            }
            return skill.GetUpgradeCost(level - initialLevel);
        }

        public void SetSkillPrice(string skillId, IPrice price)
        {
            if (!_skillPrices.TryGetValue(skillId, out var mutablePrice))
            {
                mutablePrice = new Mutable<IPrice>(price);
                _skillPrices[skillId] = mutablePrice;
            }
            else
            {
                mutablePrice.Value = price;
            }
        }

        public IBindable<IPrice> GetSkillPrice(string skillId)
        {
            if (_skillPrices.TryGetValue(skillId, out var price))
            {
                return price;
            }
            Debug.LogError($"Skill price not registered for skill {skillId}");
            return null;
        }

        public IBindable<int> GetSkillLevel(string skillId)
        {
            if (_skillSources.TryGetValue(skillId, out var skillSource))
            {
                return skillSource.Level;
            }

            Debug.LogError($"Skill source not registered for skill {skillId}");
            return new Mutable<int>(1);
        }

        public int GetInitialLevel(string skillId)
        {
            if (_skillSources.TryGetValue(skillId, out var skillSource))
            {
                return skillSource.InitialLevel;
            }

            Debug.LogError($"Skill source not registered for skill {skillId}");
            return 0;
        }

        public int GetConfigUpgradesCount(string skillId)
        {
            if (_skillsIndex.TryGetValue(skillId, out var skill))
            {
                return skill.MaxUpgrades;
            }
            Debug.LogError($"Skill not found for skill {skillId}");
            return 0;
        }

        public ReadOnlySpan<ISkillSource> GetSkillSources()
        {
            return _skillSourcesList.GetReadOnlySpan();
        }

        public IBindable<int> GetMaxLevel(string skillId)
        {
            if (_skillSources.TryGetValue(skillId, out var skillSource))
            {
                return skillSource.MaxLevel;
            }

            Debug.LogError($"Skill source not registered for skill {skillId}");
            return new Mutable<int>(0);
        }

        public bool AddSkillSource(string skillId, ISkillSource skillSource)
        {
            if (_skillSources.TryAdd(skillId, skillSource))
            {
                _skillSourcesList.Add(skillSource);
                return true;
            }
            return false;
        }

        public bool RemoveSkillSource(string skillId, ISkillSource skillSource)
        {
            if (!_skillSources.TryGetValue(skillId, out var existingSource) || existingSource != skillSource)
            {
                return false;
            }
            _skillSourcesList.SwapAndRemove(skillSource);
            return _skillSources.Remove(skillId);
        }

        public bool TryGetSkillSource(string skillId, out ISkillSource skillSource)
        {
            return _skillSources.TryGetValue(skillId, out skillSource);
        }

        public string GetSkillNameKey(string skillId)
        {
            if (_skillsIndex.TryGetValue(skillId, out var skill))
            {
                return skill.NameKey;
            }

            Debug.LogError($"Skill not found for skill {skillId}");
            return string.Empty;
        }

        public string GetSkillDescriptionKey(string skillId)
        {
            if (_skillsIndex.TryGetValue(skillId, out var skill))
            {
                return skill.DescriptionKey;
            }

            Debug.LogError($"Skill not found for skill {skillId}");
            return string.Empty;
        }

        public IBindable<HyperListReadonlyToken<string>> GetDescriptionFormattingArgs(string skillId)
        {
            if (_skillSources.TryGetValue(skillId, out var skillSource))
            {
                return skillSource.FormattingArgs;
            }

            Debug.LogError($"Skill source not registered for skill {skillId}");
            return new Mutable<HyperListReadonlyToken<string>>(default);
        }

        public IBindable<string> GetCurrentValue(string skillId)
        {
            if (_skillSources.TryGetValue(skillId, out var skillSource))
            {
                return skillSource.CurrentValue;
            }

            Debug.LogError($"Skill source not registered for skill {skillId}");
            return new Mutable<string>("");
        }

        public IBindable<string> GetNextValue(string skillId)
        {
            if (_skillSources.TryGetValue(skillId, out var skillSource))
            {
                return skillSource.NextValue;
            }

            Debug.LogError($"Skill source not registered for skill {skillId}");
            return new Mutable<string>("");
        }
    }
}
