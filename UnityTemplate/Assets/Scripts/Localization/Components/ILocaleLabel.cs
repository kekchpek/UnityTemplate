using System.Collections.Generic;

namespace kekchpek.Localization.Components
{
    public interface ILocaleLabel
    {
        string LocaleKey { get; set; }
        void SetFormattingArgs(IEnumerable<string> args);
    }
}