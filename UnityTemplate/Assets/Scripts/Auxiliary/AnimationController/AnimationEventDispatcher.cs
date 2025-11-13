using System;
using UnityEngine;

namespace AuxiliaryComponents
{
    public class AnimationEventDispatcher : MonoBehaviour
    {
        public event Action<string> EventTriggered;
        
        private void Animation_Trigger(string trigger)
        {
            EventTriggered?.Invoke(trigger);
        }
    }
}