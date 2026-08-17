using AsyncReactAwait.Bindable;

namespace kekchpek.SaveSystem
{
    public sealed class DefaultMutableFactory : IMutableFactory
    {
        public static readonly DefaultMutableFactory Instance = new();

        public IMutable<T> Create<T>(string valueKey, T value) => new Mutable<T>(value);
    }
}
