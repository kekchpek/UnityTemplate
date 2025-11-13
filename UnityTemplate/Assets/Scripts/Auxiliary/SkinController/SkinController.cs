using System;
using System.Collections.Generic;
using System.Linq;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace kekchpek.Auxiliary.SkinController
{
    public class SpineSkinApplier : MonoBehaviour
    {
        [SerializeField] private SkeletonRenderer _skeletonRenderer;
        [SerializeField] private List<SkinGroupData> _skinGroups = new();
        private bool _validateSkinNames = true;

        private List<string> _lastAppliedSkins = new();

        public event Action<List<string>> OnSkinsChanged;
        public IReadOnlyList<string> LastAppliedSkins => _lastAppliedSkins.AsReadOnly();

        private void Awake()
        {
            if (_skeletonRenderer == null)
                _skeletonRenderer = GetComponent<SkeletonRenderer>();
        }

        private void Start()
        {
            ApplySkins();
        }

        public bool HasSkinGroup(string skinSelectionGroup)
        {
            return _skinGroups.Any(g => g.SkinSelectionGroup == skinSelectionGroup);
        }


        public void ApplySkins()
        {
            if (_skeletonRenderer?.skeleton?.Data == null)
            {
                Debug.LogError($"SkinController: No SkeletonRenderer or skeleton data found on {gameObject.name}");
                return;
            }

            var skinsToApply = GetSkinsToApply();
            
            if (_validateSkinNames)
                skinsToApply = ValidateSkinNames(skinsToApply);

            ApplySkinsToSkeleton(skinsToApply);
            
            _lastAppliedSkins = skinsToApply;
            OnSkinsChanged?.Invoke(skinsToApply);
        }

        public void RandomizeGroup(string skinSelectionGroup)
        {
            var skinGroup = _skinGroups.FirstOrDefault(g => g.SkinSelectionGroup == skinSelectionGroup);
            if (skinGroup != null && skinGroup.SkinEntries.Count > 0)
            {
                ApplySkins();
            }
        }

        public void RandomizeAllGroups()
        {
            ApplySkins();
        }

        private List<string> GetSkinsToApply()
        {
            var allSkins = new List<string>();

            foreach (var skinGroup in _skinGroups)
            {
                var groupSkins = skinGroup.GetSkinsToApply();
                allSkins.AddRange(groupSkins);
            }

            return allSkins;
        }

        private List<string> ValidateSkinNames(List<string> skinNames)
        {
            var validSkins = new List<string>();

            foreach (var skinName in skinNames)
            {
                var skin = _skeletonRenderer.skeleton.Data.FindSkin(skinName);
                if (skin != null)
                {
                    validSkins.Add(skinName);
                }
                else
                {
                    Debug.LogWarning($"SkinController: Skin '{skinName}' not found in skeleton data on {gameObject.name}");
                }
            }

            return validSkins;
        }

        private void ApplySkinsToSkeleton(List<string> skinNames)
        {
            var combineSkin = new Skin("combineSkin");

            var currentSkin = _skeletonRenderer.skeleton.Skin;
            if (currentSkin != null)
            {
                combineSkin.AddSkin(currentSkin);
            }

            foreach (var skinName in skinNames)
            {
                var skin = _skeletonRenderer.skeleton.Data.FindSkin(skinName);
                if (skin != null)
                {
                    combineSkin.AddSkin(skin);
                }
            }

            _skeletonRenderer.skeleton.SetSkin(combineSkin);
            _skeletonRenderer.skeleton.SetSlotsToSetupPose();
        }

        public void AddSkinGroup(SkinGroupData skinData)
        {
            if (skinData != null && !HasSkinGroup(skinData.SkinSelectionGroup))
            {
                _skinGroups.Add(skinData);
            }
        }

        public void RemoveSkinGroup(string skinSelectionGroup)
        {
            _skinGroups.RemoveAll(g => g.SkinSelectionGroup == skinSelectionGroup);
        }

        public SkinGroupData GetSkinGroup(string skinSelectionGroup)
        {
            return _skinGroups.FirstOrDefault(g => g.SkinSelectionGroup == skinSelectionGroup);
        }

        public List<string> GetAllSkinGroupNames()
        {
            return _skinGroups.Select(g => g.SkinSelectionGroup).ToList();
        }

        public void ClearAllSkins()
        {
            if (_skeletonRenderer?.skeleton != null)
            {
                _skeletonRenderer.skeleton.SetSkin((Skin)null);
                _skeletonRenderer.skeleton.SetSlotsToSetupPose();
                
                _lastAppliedSkins.Clear();
                OnSkinsChanged?.Invoke(_lastAppliedSkins);
            }
        }
    }
}
