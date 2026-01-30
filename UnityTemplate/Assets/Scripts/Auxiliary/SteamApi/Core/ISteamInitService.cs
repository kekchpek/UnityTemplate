using AsyncReactAwait.Bindable;

namespace kekchpek.SteamApi.Core
{
    public interface ISteamInitService
    {
        IBindable<bool> IsInitialized { get; }
        void Initialize();
    }
}
