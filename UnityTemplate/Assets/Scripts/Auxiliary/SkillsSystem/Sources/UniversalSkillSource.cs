using System;
using AsyncReactAwait.Bindable;
using kekchpek.Auxiliary.Collections;
using kekchpek.Localization;
using kekchpek.SaveSystem;

namespace SkillsSystem.Impl
{
    public class UniversalSkillSource : ISkillSource
    {

        private const string SkillsLevelPrefix = "Skills/Level/";
        private const string LegacySkillTreeLevelPrefix = "SkillTree/Level/";

        private readonly int _initialLevel;
        private readonly IMutable<int> _level;
        private readonly IMutable<int> _maxLevel = new Mutable<int>(0);

        private readonly Mutable<string> _currentValue = new();
        private readonly Mutable<string> _nextValue = new();
        private readonly IBindable<HyperListReadonlyToken<string>> _formattingArgs;

        private readonly Action<int> _setLevel;

        private readonly string _skillId;

        private readonly Func<int, ILocalizationModel, string> _getCurrentValue;
        private readonly Func<int, ILocalizationModel, string> _getNextValue;

        private readonly ILocalizationModel _localizationModel;

        public string SkillId => _skillId;

        public IBindable<string> CurrentValue => _currentValue;

        public IBindable<string> NextValue => _nextValue;

        public IBindable<int> Level => _level;

        public int InitialLevel => _initialLevel;

        public IBindable<int> MaxLevel => _maxLevel;

        public IBindable<HyperListReadonlyToken<string>> FormattingArgs => _formattingArgs;

        public UniversalSkillSource(
            string skillId,
            int initialLevel,
            int maxLevel,
            Action<int> setLevel,
            ISaveDataProvider saveDataProvider,
            Func<int, ILocalizationModel, string> getCurrentValue,
            Func<int, ILocalizationModel, string> getNextValue,
            IBindable<HyperListReadonlyToken<string>> formattingArgs,
            ILocalizationModel localizationModel)
        {
            _skillId = skillId;
            _initialLevel = initialLevel;
            _localizationModel = localizationModel;
            _getCurrentValue = getCurrentValue;
            _getNextValue = getNextValue;
            _maxLevel.Value = maxLevel;
            _setLevel = setLevel;
            _formattingArgs = formattingArgs;
            var legacyLevel = saveDataProvider
                .DeserializeAndCaptureStructValue($"{LegacySkillTreeLevelPrefix}{skillId}", -1)
                .Value;
            var levelToSet = Math.Clamp(legacyLevel + 1, initialLevel, maxLevel);
            _level = saveDataProvider.DeserializeAndCaptureStructValue(
                $"{SkillsLevelPrefix}{skillId}",
                levelToSet);
            SetLevel(_level.Value);
        }

        private Func<ILocalizationModel, string> CreateValueStringBuilder(Func<int, ILocalizationModel, string> valueGetter)
        {
            return (lm) => {
                return valueGetter(_level.Value, lm);
            };
        }

        public void UpdateStrings()
        {
            _currentValue.Proxy(_localizationModel.ConstructLocalizedString(CreateValueStringBuilder(_getCurrentValue))); 
            _nextValue.Proxy(_localizationModel.ConstructLocalizedString(CreateValueStringBuilder(_getNextValue)));
        }

        public void SetLevel(int level)
        {
            level = Math.Clamp(level, 0, _maxLevel.Value);
            _level.Value = level;
            _setLevel?.Invoke(level);
            UpdateStrings();
        }
    }
}