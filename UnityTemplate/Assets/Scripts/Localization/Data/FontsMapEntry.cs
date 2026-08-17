using System;
using TMPro;
using UnityEngine;

namespace kekchpek.Localization.Data 
{
    [Serializable]
    public struct FontMapEntry
    {

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private string _localeFontPath;

        public TMP_FontAsset Font => _font;
        public string LocaleFontPath => _localeFontPath;
    }
}