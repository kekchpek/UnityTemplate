using System;
using System.Collections.Generic;
using UnityEngine;

namespace kekchpek.Auxiliary.SkinController
{
    [Serializable]
    public class SkinEntry
    {
        [SerializeField] private string _skinName;
        [SerializeField] private bool _useFolderName = false;
        [SerializeField] private List<string> _subSkins = new();

        public string SkinName => _skinName;
        public bool UseFolderName => _useFolderName;
        public List<string> SubSkins => _subSkins;
        public bool IsFolder => _subSkins != null && _subSkins.Count > 0;

        public SkinEntry()
        {
            _subSkins = new List<string>();
        }

        public SkinEntry(string skinName, bool useFolderName = false)
        {
            _skinName = skinName;
            _useFolderName = useFolderName;
            _subSkins = new List<string>();
        }

        public List<string> GetSkinNames(string groupFolderName = "")
        {
            var result = new List<string>();
            
            if (IsFolder)
            {
                foreach (var subSkin in _subSkins)
                {
                    var skinPath = _skinName + "/" + subSkin;
                    if (_useFolderName && !string.IsNullOrEmpty(groupFolderName))
                    {
                        skinPath = groupFolderName + "/" + skinPath;
                    }
                    result.Add(skinPath);
                }
            }
            else
            {
                var skinPath = _skinName;
                if (_useFolderName && !string.IsNullOrEmpty(groupFolderName))
                {
                    skinPath = groupFolderName + "/" + skinPath;
                }
                result.Add(skinPath);
            }
            
            return result;
        }
    }
}
