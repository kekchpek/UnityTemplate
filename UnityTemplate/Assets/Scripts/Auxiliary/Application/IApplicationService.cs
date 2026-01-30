using System;
using AsyncReactAwait.Bindable;

namespace kekchpek.Auxiliary.Application
{
    public interface IApplicationService
    {
        IBindable<(int width, int height)> ScreenSize { get; }
        event Action ApplicationQuit;
        void Quit();
    }
}