using UnityEngine;

namespace SBG.Capabilities.Animation
{
    [System.Serializable]
    public class CapabilityBlendtreeAnimation
	{
        public string Channel = "default";
        [Min(0)] public int Priority = 100;
        public TransitionLength InTransitionLength = new TransitionLength(false, false, 0, 1);
        public TransitionLength OutTransitionLength = new TransitionLength(false, false, 0, 1);
        [Space]
        public AnimationClip ClipCenter;
        public AnimationClip ClipForward;
        public AnimationClip ClipBackward;
        public AnimationClip ClipLeft;
        public AnimationClip ClipRight;
    }
}