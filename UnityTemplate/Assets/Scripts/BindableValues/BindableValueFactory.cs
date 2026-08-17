using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;

namespace kekchpek.BindableValues
{
    public sealed class BindableValueFactory : IBindableValueFactory, IBindableValueRegistry
    {
        private readonly Dictionary<string, IRegisteredBindableValue> _values = new();
        private readonly Dictionary<Type, object> _formatterCache = new();

        public IMutable<T> Create<T>(BindableValueId id, T initial = default)
        {
            if (_values.ContainsKey(id.Id))
            {
                throw new InvalidOperationException($"Bindable value '{id}' is already registered.");
            }

            var entry = CreateEntry<T>(initial);
            _values.Add(id.Id, entry);
            return entry.Mutable;
        }

        public IMutable<T> GetMutable<T>(BindableValueId id)
        {
            if (TryGetEntry<T>(id, out var entry))
            {
                return entry.Mutable;
            }

            throw CreateNotFoundException<T>(id);
        }

        public IBindable<T> Get<T>(BindableValueId id)
        {
            if (TryGetEntry<T>(id, out var entry))
            {
                return entry.Mutable;
            }

            throw CreateNotFoundException<T>(id);
        }

        public bool TryGet<T>(BindableValueId id, out IBindable<T> bindable)
        {
            if (TryGetEntry<T>(id, out var entry))
            {
                bindable = entry.Mutable;
                return true;
            }

            bindable = null;
            return false;
        }

        public bool TryGetFormattedValue(BindableValueId id, out string formattedValue)
        {
            if (_values.TryGetValue(id.Id, out var entry))
            {
                formattedValue = entry.GetFormattedValue();
                return true;
            }

            formattedValue = null;
            return false;
        }

        public void Subscribe(BindableValueId id, Action onChanged)
        {
            GetEntry(id).Subscribe(onChanged);
        }

        public void Unsubscribe(BindableValueId id, Action onChanged)
        {
            if (_values.TryGetValue(id.Id, out var entry))
            {
                entry.Unsubscribe(onChanged);
            }
        }

        private RegisteredBindableValue<T> CreateEntry<T>(T initial)
        {
            return new RegisteredBindableValue<T>(
                new Mutable<T>(initial),
                BindableValueFormatters.GetOrCreate<T>(_formatterCache));
        }

        private bool TryGetEntry<T>(BindableValueId id, out RegisteredBindableValue<T> entry)
        {
            entry = null;
            return _values.TryGetValue(id.Id, out var registered) &&
                   (entry = registered as RegisteredBindableValue<T>) != null;
        }

        private IRegisteredBindableValue GetEntry(BindableValueId id)
        {
            if (_values.TryGetValue(id.Id, out var entry))
            {
                return entry;
            }

            throw new KeyNotFoundException($"Bindable value '{id}' is not registered.");
        }

        private static KeyNotFoundException CreateNotFoundException<T>(BindableValueId id)
        {
            return new KeyNotFoundException($"Bindable value '{id}' of type {typeof(T).Name} is not registered.");
        }
    }
}
