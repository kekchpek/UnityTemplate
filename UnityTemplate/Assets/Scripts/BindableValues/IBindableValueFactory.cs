using AsyncReactAwait.Bindable;

namespace kekchpek.BindableValues
{
    /// <summary>
    /// Creates and registers shared bindable values.
    /// Use <see cref="IBindableValueRegistry"/> to read values created through this factory.
    /// </summary>
    public interface IBindableValueFactory
    {
        /// <summary>
        /// Creates a new mutable value, registers it under <paramref name="id"/>,
        /// and returns the created instance.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="id">Unique identifier used for later lookups.</param>
        /// <param name="initial">Initial value stored in the mutable.</param>
        /// <returns>The created mutable value.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when a value with the same <paramref name="id"/> is already registered.
        /// </exception>
        IMutable<T> Create<T>(BindableValueId id, T initial = default);
    }
}
