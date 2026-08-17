using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;
using kekchpek.BindableValues;
using kekchpek.Localization;

namespace kekchpek.DynamicTexts
{
    public class ReactiveText : IReactiveText, IDisposable
    {
        private readonly Mutable<string> _text = new();
        private readonly ILocalizationModel _localizationModel;
        private readonly IBindableValueRegistry _valueRegistry;
        private readonly string _localeKey;
        private readonly List<BindableValueId> _valueIds = new();
        // Same delegate instance must be passed to Subscribe and Unsubscribe — method group
        // conversions (RefreshText) create a new Action each time, so the registry cannot
        // match handlers for removal when using the dictionary keyed by Action reference.
        private readonly Action _refreshHandler;
        private bool _valueSubscriptionsRegistered;

        public IBindable<string> Text => _text;

        public ReactiveText(
            string localeKey,
            ILocalizationModel localizationModel,
            IBindableValueRegistry valueRegistry)
        {
            _localeKey = localeKey;
            _localizationModel = localizationModel;
            _valueRegistry = valueRegistry;
            _refreshHandler = RefreshText;
            _localizationModel.NewLocaleReadyToBeDisplayed += RefreshText;
            _localizationModel.IsInitialized.Bind(OnInitializedChanged);
            RefreshText();
        }

        private void OnInitializedChanged(bool isInitialized)
        {
            if (isInitialized)
            {
                RefreshText();
            }
        }

        private void RegisterValueSubscriptions()
        {
            if (_valueSubscriptionsRegistered
                || !_localizationModel.IsInitialized.Value
                || string.IsNullOrEmpty(_localeKey))
            {
                return;
            }

            _valueSubscriptionsRegistered = true;

            var template = _localizationModel.GetLocalizedString(_localeKey);
            BindableTemplateResolver.CollectValueIds(template, _valueIds);
            foreach (var valueId in _valueIds)
            {
                _valueRegistry.Subscribe(valueId, _refreshHandler);
            }
        }

        private void RefreshText()
        {
            RegisterValueSubscriptions();

            if (!_localizationModel.IsInitialized.Value || string.IsNullOrEmpty(_localeKey))
            {
                _text.Value = string.Empty;
                return;
            }

            var template = _localizationModel.GetLocalizedString(_localeKey);
            _text.Value = BindableTemplateResolver.Resolve(template, _valueRegistry);
        }

        public void Dispose()
        {
            foreach (var valueId in _valueIds)
            {
                _valueRegistry.Unsubscribe(valueId, _refreshHandler);
            }

            _localizationModel.NewLocaleReadyToBeDisplayed -= RefreshText;
            _localizationModel.IsInitialized.Unbind(OnInitializedChanged);
        }
    }
}
