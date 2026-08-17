using System;
using UnityEngine;

namespace kekchpek.Localization.Data
{
    [Serializable]
    public struct FontMaterialMapEntry
    {
        [SerializeField] private Material _material;
        [SerializeField] private string _localeMaterialPath;

        public Material Material => _material;
        public string LocaleMaterialPath => _localeMaterialPath;
    }
}