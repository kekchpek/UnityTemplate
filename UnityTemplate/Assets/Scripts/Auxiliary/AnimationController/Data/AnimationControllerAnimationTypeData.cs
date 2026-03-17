using System;

namespace kekchpek.Auxiliary.AnimationControllerTool
{
    [Serializable]
    public class AnimationControllerAnimationTypeData : IAnimationTypeData
    {
        public AnimationController TargetAnimationController;
        public string TargetSequenceName;
    }
}
