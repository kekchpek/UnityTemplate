using System;
using System.Collections.Generic;

namespace kekchpek.Auxiliary.AnimationControllerTool
{
    [Serializable]
    public class AnimationSequence
    {
        public string SequenceName;
        public List<AnimationData> Animations = new();
    }
} 