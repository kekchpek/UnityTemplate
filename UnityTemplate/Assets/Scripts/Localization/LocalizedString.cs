using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;

namespace kekchpek.Localization
{
    public class LocalizedString : IBindable<string>
    {

        private readonly List<Action<string>> _valueHandlers = new();
        private readonly List<Action<string, string>> _changeHandlers = new();
        private readonly List<Action> _noValueHandlers = new();
        
        private readonly ILocalizationModel _localizationModel;
        private readonly Func<ILocalizationModel, string> _stringBuilder;
        private readonly string _key;

        private string _value;
        private bool _isBoundToModel;

        public string Value => _value;

        public LocalizedString(
            Func<ILocalizationModel, string> stringBuilder,
            ILocalizationModel localizationModel) 
        {
            _localizationModel = localizationModel;
            _stringBuilder = stringBuilder;
            _value = _stringBuilder(_localizationModel);
        }

        public LocalizedString(string key, ILocalizationModel localizationModel)
        {
            _localizationModel = localizationModel;
            _key = key;
            _value = _localizationModel.GetLocalizedString(key);
        }

        public void Bind(Action<string> handler, bool callImmediately = true)
        {
            _valueHandlers.Add(handler);
            if (callImmediately) {
                handler(_value);
            }
            UpdateModelBinding();
        }

        public void Bind(Action<string, string> handler)
        {
            _changeHandlers.Add(handler);
            UpdateModelBinding();
        }

        public void Bind(Action handler, bool callImmediately = true)
        {
            _noValueHandlers.Add(handler);
            if (callImmediately)
                handler();
            UpdateModelBinding();
        }

        public void Unbind(Action<string> handler)
        {
            _valueHandlers.Remove(handler);
            UpdateModelBinding();
        }

        public void Unbind(Action<string, string> handler)
        {
            _changeHandlers.Remove(handler);
            UpdateModelBinding();
        }

        public void Unbind(Action handler)
        {
            _noValueHandlers.Remove(handler);
            UpdateModelBinding();
        }

        private void UpdateModelBinding() 
        {
            if (HasAnyBinding() && !_isBoundToModel) 
            {
                BindToModel();
            }
            if (!HasAnyBinding() && _isBoundToModel) 
            {
                UnbindFromModel();
            }
        }

        private void BindToModel() 
        {
            if (_isBoundToModel)
                return;
            _localizationModel.NewLocaleReadyToBeDisplayed += UpdateValue;
            _isBoundToModel = true;
        }

        private void UnbindFromModel()
        {
            if (!_isBoundToModel)
                return;
            _localizationModel.NewLocaleReadyToBeDisplayed -= UpdateValue;
            _isBoundToModel = false;
        }

        private void UpdateValue() 
        {
            var prevValue = _value;
            if (_key != null)
            {
                _value = _localizationModel.GetLocalizedString(_key);
            }
            else
            {
                _value = _stringBuilder(_localizationModel);
            }
            foreach (var handler in _valueHandlers) {
                handler(_value);
            }
            foreach (var handler in _changeHandlers) {
                handler(prevValue, _value);
            }
            foreach (var handler in _noValueHandlers) {
                handler();
            }
        }

        private bool HasAnyBinding() 
        {
            return _changeHandlers.Count > 0 ||
                   _valueHandlers.Count > 0 ||
                   _noValueHandlers.Count > 0;
        }
    }
}