using System;

namespace kekchpek.BindableValues
{
    internal interface IRegisteredBindableValue
    {
        string GetFormattedValue();
        void Subscribe(Action onChanged);
        void Unsubscribe(Action onChanged);
    }
}
