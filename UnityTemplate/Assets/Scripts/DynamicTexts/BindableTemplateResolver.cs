using System.Collections.Generic;
using System.Text.RegularExpressions;
using kekchpek.BindableValues;

namespace kekchpek.DynamicTexts
{
    public static class BindableTemplateResolver
    {
        private static readonly Regex PlaceholderRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

        public static string Resolve(string template, IBindableValueRegistry valueRegistry)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            return PlaceholderRegex.Replace(
                template,
                match =>
                {
                    var valueId = new BindableValueId(match.Groups[1].Value);
                    return valueRegistry.TryGetFormattedValue(valueId, out var formattedValue)
                        ? formattedValue
                        : match.Value;
                });
        }

        public static void CollectValueIds(string template, ICollection<BindableValueId> valueIds)
        {
            if (string.IsNullOrEmpty(template))
            {
                return;
            }

            foreach (Match match in PlaceholderRegex.Matches(template))
            {
                valueIds.Add(new BindableValueId(match.Groups[1].Value));
            }
        }
    }
}
