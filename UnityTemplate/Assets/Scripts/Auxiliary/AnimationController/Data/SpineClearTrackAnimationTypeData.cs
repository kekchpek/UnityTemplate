using System;
using Spine.Unity;

namespace kekchpek.Auxiliary.AnimationControllerTool
{
    [Serializable]
    public class SpineClearTrackAnimationTypeData : IAnimationTypeData
    {
        public SkeletonGraphic SpineSkeleton;
        public SkeletonAnimation SpineSkeletonAnimation;
        public int TrackIndex;
        public float Duration = 0.2f;
    }
}
