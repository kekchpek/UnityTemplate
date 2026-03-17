using System;
using UnityEngine;

namespace kekchpek.Auxiliary.AnimationControllerTool
{
    [Serializable]
    public class UnityAnimationTypeData : IAnimationTypeData
    {
        public Animator UnityAnimator;
        public string AnimationStateName;
    }
}
