using kekchpek.BindableValues;
using kekchpek.Localization;

namespace kekchpek.DynamicTexts
{
    public class ReactiveTextService : IReactiveTextService
    {
        private readonly ILocalizationModel _localizationModel;
        private readonly IBindableValueRegistry _valueRegistry;

        public ReactiveTextService(
            ILocalizationModel localizationModel,
            IBindableValueRegistry valueRegistry)
        {
            _localizationModel = localizationModel;
            _valueRegistry = valueRegistry;
        }

        public IReactiveText Create(string localeKey)
        {
            return new ReactiveText(localeKey, _localizationModel, _valueRegistry);
        }
    }
}
