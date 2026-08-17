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
        /// <summary>Track mix alpha (0–1). Applied to the Spine <see cref="Spine.TrackEntry.Alpha"/> when this animation is set on the track.</summary>
        public float AnimationAlpha = 1f;
    }
}
