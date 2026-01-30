using System;
using AuxiliaryComponents;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Serialization;

namespace kekchpek.Auxiliary.AnimationControllerTool
{
    [Serializable]
    public class AnimationData
    {
        public AnimationType Type;
        public bool ExecuteInParallel;
        
        [HideInInspector]
        public Animator UnityAnimator;

        [HideInInspector]
        [FormerlySerializedAs("TriggerName")]
        public string AnimationStateName;
        
        [HideInInspector]
        public SkeletonGraphic SpineSkeleton;
        [HideInInspector]
        public SkeletonAnimation SpineSkeletonAnimation;
        [HideInInspector]
        public string AnimationName;
        [HideInInspector]
        public int SpineAnimationLayer;
        
        [HideInInspector]
        public AnimationController TargetAnimationController;
        [HideInInspector]
        public string TargetSequenceName;

        public void OnValidate()
        {
            bool isUnityType = Type == AnimationType.Unity;
            bool isSpineType = Type == AnimationType.Spine;
            bool isAnimationControllerType = Type == AnimationType.AnimationController;

            // Clear Unity fields if not Unity type
            if (!isUnityType)
            {
                UnityAnimator = null;
                AnimationStateName = string.Empty;
            }

            // Clear Spine fields if not Spine type
            if (!isSpineType)
            {
                SpineSkeleton = null;
                SpineSkeletonAnimation = null;
                AnimationName = string.Empty;
                SpineAnimationLayer = 0;
            }
            
            // Clear AnimationController fields if not AnimationController type
            if (!isAnimationControllerType)
            {
                TargetAnimationController = null;
                TargetSequenceName = string.Empty;
            }
        }
    }
} 