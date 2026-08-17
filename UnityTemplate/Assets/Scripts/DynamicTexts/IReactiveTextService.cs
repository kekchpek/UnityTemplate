namespace kekchpek.DynamicTexts
{
    /// <summary>
    /// Creates reactive localized texts backed by the bindable value registry.
    /// </summary>
    public interface IReactiveTextService
    {
        /// <summary>
        /// Creates a reactive text for the provided locale key.
        /// </summary>
        /// <param name="localeKey">Localization key containing optional <c>{ValueId}</c> placeholders.</param>
        /// <returns>Reactive text instance cached by the caller when needed.</returns>
        IReactiveText Create(string localeKey);
    }
}
