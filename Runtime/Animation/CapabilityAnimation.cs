using UnityEngine;

namespace SBG.Capabilities.Runtime.Animation
{
    [System.Serializable]
    public struct CapAnimClipEntry
    {
        public string SpecifierId;
        public AnimationClip Clip;
    }

    [System.Serializable]
    public struct TransitionLength
    {
        public bool IsUsed;
        public float PreferedLength;
        public float MaxLength;

        public TransitionLength(bool isUsed, float preferedLength, float maxLength)
        {
            IsUsed = isUsed;
            PreferedLength = preferedLength;
            MaxLength = maxLength;
        }
    }

    [System.Serializable]
	public class CapabilityAnimation
	{
        public string Channel = "default";
        [Min(0)] public int Priority = 100;
        public TransitionLength InTransitionLength = new TransitionLength(false, 0, 1);
        public TransitionLength OutTransitionLength = new TransitionLength(false, 0, 1);
        public AnimationClip fallbackClip;
        public CapAnimClipEntry[] Clips;
	}
}