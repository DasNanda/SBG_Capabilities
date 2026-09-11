using UnityEngine;

namespace SBG.Capabilities.Animation
{
	internal interface ICapabilityPlayable
	{
        string Id { get; }
		int InputIndex { get; }
        int Priority { get; }
        TransitionLength InTransitionLength { get; }
        TransitionLength OutTransitionLength { get; }
        bool IsPlaying { get; }
        bool Active { get; set; }

        void SetWeight(float weight);
        float GetWeight();
        void Play(float startSpeed = 0);
        void Cancel();
    }
}