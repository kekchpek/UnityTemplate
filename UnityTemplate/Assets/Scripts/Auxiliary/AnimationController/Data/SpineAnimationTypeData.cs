using System;
using Spine.Unity;

namespace kekchpek.Auxiliary.AnimationControllerTool
{
    [Serializable]
    public class SpineAnimationTypeData : IAnimationTypeData
    {
        public SkeletonGraphic SpineSkeleton;
        public SkeletonAnimation SpineSkeletonAnimation;
        public string AnimationName;
        public int SpineAnimationLayer;
    }
}
