using AsyncReactAwait.Bindable;

namespace kekchpek.DynamicTexts
{
    /// <summary>
    /// Localized text that resolves bindable placeholders from <see cref="kekchpek.BindableValues.IBindableValueRegistry"/>.
    /// </summary>
    public interface IReactiveText
    {
        /// <summary>
        /// Fully resolved text updated on locale or referenced value changes.
        /// </summary>
        IBindable<string> Text { get; }
    }
}
