using System;
using UnityEngine;

namespace kekchpek.Auxiliary.AnimationControllerTool
{
    [Serializable]
    public class ObjectCreationAnimationTypeData : IAnimationTypeData
    {
        public GameObject ObjectToSpawn;
        public Transform Container;
    }
}
