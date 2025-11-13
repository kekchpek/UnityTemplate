using System;
using System.Collections.Generic;
using UnityEngine;

namespace kekchpek.Auxiliary.SkinController
{
    [Serializable]
    public class SkinGroupData
    {
        [SerializeField] private string _skinSelectionGroup;
        [SerializeField] private bool _useFolderName = false;
        [SerializeField] private bool _randomSelection = false;
        [SerializeField] private List<SkinEntry> _skinEntries = new();
        

        public string SkinSelectionGroup => _skinSelectionGroup;
        public bool UseFolderName => _useFolderName;
        public List<SkinEntry> SkinEntries => _skinEntries;
        public bool RandomSelection => _randomSelection;

        public SkinGroupData()
        {
            _skinEntries = new List<SkinEntry>();
        }

        public SkinGroupData(string skinSelectionGroup, bool useFolderName = false, bool randomSelection = false)
        {
            _skinSelectionGroup = skinSelectionGroup;
            _useFolderName = useFolderName;
            _randomSelection = randomSelection;
            _skinEntries = new List<SkinEntry>();
        }

        public List<string> GetSkinsToApply()
        {
            if (_skinEntries == null || _skinEntries.Count == 0)
                return new List<string>();

            var allAvailableSkins = new List<string>();
            
            var groupFolderName = _useFolderName ? _skinSelectionGroup : "";
            
            foreach (var entry in _skinEntries)
            {
                allAvailableSkins.AddRange(entry.GetSkinNames(groupFolderName));
            }

            if (allAvailableSkins.Count == 0)
                return new List<string>();

            var skinsToUse = new List<string>();

            if (_randomSelection)
            {
                var randomIndex = UnityEngine.Random.Range(0, allAvailableSkins.Count);
                skinsToUse.Add(allAvailableSkins[randomIndex]);
            }
            else
            {
                skinsToUse.AddRange(allAvailableSkins);
            }

            return skinsToUse;
        }
    }
}
