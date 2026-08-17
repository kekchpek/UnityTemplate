using System;
using kekchpek.Localization.Data;
using UnityEngine;

namespace kekchpek.Localization
{
    [CreateAssetMenu(fileName = "FontsConfig", menuName = "Localization/FontsConfig")]
    public class FontsConfig : ScriptableObject
    {
        [SerializeField] private FontLocaleMapping[] _fontLocaleMappings = new FontLocaleMapping[0];

        public ReadOnlySpan<FontLocaleMapping> FontLocaleMappings => _fontLocaleMappings;
    }
}