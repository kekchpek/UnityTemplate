using System;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Serialization;

namespace kekchpek.Auxiliary.AnimationControllerTool
{
    [Serializable]
    public class AnimationData : ISerializationCallbackReceiver
    {
        public AnimationType Type;
        public bool ExecuteInParallel;

        [SerializeReference]
        private IAnimationTypeData _animationTypeData;

        public UnityAnimationTypeData UnityData => _animationTypeData as UnityAnimationTypeData;
        public SpineAnimationTypeData SpineData => _animationTypeData as SpineAnimationTypeData;
        public SpineClearTrackAnimationTypeData SpineClearTrackData => _animationTypeData as SpineClearTrackAnimationTypeData;
        public AnimationControllerAnimationTypeData AnimationControllerData => _animationTypeData as AnimationControllerAnimationTypeData;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            AssignDataByType();
        }

        private void AssignDataByType()
        {
            if (Type == AnimationType.Unity && _animationTypeData is UnityAnimationTypeData)
            {
                return;
            }

            if (Type == AnimationType.Spine && _animationTypeData is SpineAnimationTypeData)
            {
                return;
            }

            if (Type == AnimationType.AnimationController && _animationTypeData is AnimationControllerAnimationTypeData)
            {
                return;
            }

            if (Type == AnimationType.SpineClearTrack && _animationTypeData is SpineClearTrackAnimationTypeData)
            {
                return;
            }

            switch (Type)
            {
                case AnimationType.Unity:
                    _animationTypeData = new UnityAnimationTypeData();
                    break;
                case AnimationType.Spine:
                    _animationTypeData = new SpineAnimationTypeData();
                    break;
                case AnimationType.SpineClearTrack:
                    _animationTypeData = new SpineClearTrackAnimationTypeData();
                    break;
                case AnimationType.AnimationController:
                    _animationTypeData = new AnimationControllerAnimationTypeData();
                    break;
            }
        }
    }
} 