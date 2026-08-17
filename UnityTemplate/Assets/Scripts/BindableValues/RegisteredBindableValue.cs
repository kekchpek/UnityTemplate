using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;

namespace kekchpek.BindableValues
{
    internal sealed class RegisteredBindableValue<T> : IRegisteredBindableValue
    {
        private readonly Func<T, string> _formatter;
        private readonly Dictionary<Action, Action<T>> _handlers = new();

        public IMutable<T> Mutable { get; }

        public RegisteredBindableValue(IMutable<T> mutable, Func<T, string> formatter)
        {
            Mutable = mutable;
            _formatter = formatter;
        }

        public string GetFormattedValue() => _formatter(Mutable.Value);

        public void Subscribe(Action onChanged)
        {
            if (_handlers.ContainsKey(onChanged))
            {
                return;
            }

            Action<T> handler = _ => onChanged();
            _handlers[onChanged] = handler;
            Mutable.Bind(handler, false);
            onChanged();
        }

        public void Unsubscribe(Action onChanged)
        {
            if (_handlers.Remove(onChanged, out var handler))
            {
                Mutable.Unbind(handler);
            }
        }
    }
}
