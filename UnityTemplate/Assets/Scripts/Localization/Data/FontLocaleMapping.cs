using System;
using TMPro;
using UnityEngine;

namespace kekchpek.Localization.Data
{
    [Serializable]
    public struct FontLocaleMapping
    {

        [SerializeField] private string _locale;
        [SerializeField] private FontMapEntry[] _fontMapEntries;
        [SerializeField] private FontMaterialMapEntry[] _fontMaterialMapEntries;

        public string Locale => _locale;
        public ReadOnlySpan<FontMapEntry> FontMapEntries => _fontMapEntries;
        public ReadOnlySpan<FontMaterialMapEntry> FontMaterialMapEntries => _fontMaterialMapEntries;
    }
}