using System;
using AsyncReactAwait.Bindable;
using kekchpek.Auxiliary.Collections;
using UnityEngine;

namespace SkillsSystem
{
    public class MaxLevelRestrictedSkillSource : ISkillSource, IDisposable
    {
        private readonly ISkillSource _decoratedSkillSource;
        private readonly int _maxLevelLimit;

        private readonly Mutable<int> _maxLevel = new(0);
        private readonly Mutable<int> _level = new(0);

        public string SkillId => _decoratedSkillSource.SkillId;

        public IBindable<string> CurrentValue => _decoratedSkillSource.CurrentValue;

        public IBindable<string> NextValue => _decoratedSkillSource.NextValue;

        public IBindable<int> Level => _level;

        public int InitialLevel => _decoratedSkillSource.InitialLevel;

        public IBindable<int> MaxLevel => _maxLevel;

        public IBindable<HyperListReadonlyToken<string>> FormattingArgs => _decoratedSkillSource.FormattingArgs;

        public MaxLevelRestrictedSkillSource(int maxLevel, ISkillSource decoratedSkillSource)
        {
            _decoratedSkillSource = decoratedSkillSource;
            _maxLevelLimit = maxLevel;
            _decoratedSkillSource.Level.Bind(UpdateLevels, false);
            _decoratedSkillSource.MaxLevel.Bind(UpdateLevels, false);
            UpdateLevels();
        }

        private void UpdateLevels()
        {
            var level = _decoratedSkillSource.Level.Value;
            var decoratedMaxLevel = _decoratedSkillSource.MaxLevel.Value;
            if (decoratedMaxLevel < _maxLevelLimit)
            {
                _maxLevel.Value = decoratedMaxLevel;
            }
            else
            {
                _maxLevel.Value = _maxLevelLimit;
            }
            _level.Value = Mathf.Clamp(level, 0, _maxLevel.Value);
        }

        public void SetLevel(int level)
        {
            _decoratedSkillSource.SetLevel(Mathf.Max(level, 0));
        }

        public void Dispose()
        {
            if (_decoratedSkillSource is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _decoratedSkillSource.Level.Unbind(UpdateLevels);
            _decoratedSkillSource.MaxLevel.Unbind(UpdateLevels);
        }
    }
}
