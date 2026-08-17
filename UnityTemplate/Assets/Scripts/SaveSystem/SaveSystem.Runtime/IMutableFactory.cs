using AsyncReactAwait.Bindable;

namespace kekchpek.SaveSystem
{
    /// <summary>
    /// Creates mutable values captured by the save system.
    /// </summary>
    public interface IMutableFactory
    {
        IMutable<T> Create<T>(string valueKey, T value);
    }
}
