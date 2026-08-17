using System;
using AsyncReactAwait.Bindable;

namespace kekchpek.BindableValues
{
    /// <summary>
    /// Read-only access to bindable values registered in the global value store.
    /// </summary>
    public interface IBindableValueRegistry
    {
        /// <summary>
        /// Returns a registered bindable value.
        /// </summary>
        /// <typeparam name="T">Expected value type.</typeparam>
        /// <param name="id">Registered value identifier.</param>
        /// <returns>The registered bindable value.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown when the value is missing or has a different type.
        /// </exception>
        IBindable<T> Get<T>(BindableValueId id);

        /// <summary>
        /// Returns a registered mutable value.
        /// </summary>
        /// <typeparam name="T">Expected value type.</typeparam>
        /// <param name="id">Registered value identifier.</param>
        /// <returns>The registered mutable value.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown when the value is missing or has a different type.
        /// </exception>
        IMutable<T> GetMutable<T>(BindableValueId id);

        /// <summary>
        /// Attempts to return a registered bindable value.
        /// </summary>
        /// <typeparam name="T">Expected value type.</typeparam>
        /// <param name="id">Registered value identifier.</param>
        /// <param name="bindable">Resolved bindable when the lookup succeeds.</param>
        /// <returns><c>true</c> when the value exists and matches <typeparamref name="T"/>.</returns>
        bool TryGet<T>(BindableValueId id, out IBindable<T> bindable);

        /// <summary>
        /// Returns the registered value formatted as display text.
        /// </summary>
        /// <param name="id">Registered value identifier.</param>
        /// <param name="formattedValue">Formatted text when the lookup succeeds.</param>
        /// <returns><c>true</c> when the value exists.</returns>
        bool TryGetFormattedValue(BindableValueId id, out string formattedValue);

        /// <summary>
        /// Subscribes to changes of the registered value.
        /// The handler is invoked immediately with the current value.
        /// </summary>
        /// <param name="id">Registered value identifier.</param>
        /// <param name="onChanged">Handler invoked when the value changes.</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown when the value is not registered.
        /// </exception>
        void Subscribe(BindableValueId id, Action onChanged);

        /// <summary>
        /// Unsubscribes a previously registered change handler.
        /// </summary>
        /// <param name="id">Registered value identifier.</param>
        /// <param name="onChanged">Handler to remove.</param>
        void Unsubscribe(BindableValueId id, Action onChanged);
    }
}
