using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SBG.Capabilities.Animation
{
	internal class CapabilityClip
	{
        public string Id { get; private set; }
        public int InputIndex { get; private set; }
        public int Priority { get; private set; }
        public TransitionLength InTransitionLength { get; private set; }
        public TransitionLength OutTransitionLength { get; private set; }
        public bool IsPlaying { get; private set; }

        public bool Active;

        private AnimationClipPlayable playable;
        private CapabilityChannel channel;

        private event Action onComplete;
        private event Action onCancel;

        private float duration;
        private bool looping;


        public CapabilityClip(AnimationClip clip, CapabilityChannel parentChannel, string id, int priority, TransitionLength inLength, TransitionLength outLength, Action onComplete, Action onCancel)
        {
            Priority = priority;
            InTransitionLength = inLength;
            OutTransitionLength = outLength;
            duration = clip.length;
            looping = clip.isLooping;

            this.onComplete = onComplete;
            this.onCancel = onCancel;

            channel = parentChannel;
            Id = id;
            InputIndex = channel.Mixer.GetInputCount();

            playable = AnimationClipPlayable.Create(channel.Mixer.GetGraph(), clip);
            channel.Mixer.SetInputCount(InputIndex + 1);
            channel.Mixer.ConnectInput(InputIndex, playable, 0, 0);
        }

        public void TryComplete()
        {
            if (!Active || !IsPlaying) return;
            if (looping || playable.IsDone()) return;
            if (playable.GetTime() < duration) return;

            Complete();
        }

        public void SetWeight(float weight)
        {
            channel.Mixer.SetInputWeight(InputIndex, weight);

            if (weight >= 1) playable.SetSpeed(1);
        }

        public void Play(float startSpeed=0)
        {
            channel.Mixer.SetInputWeight(InputIndex, 0);
            channel.Mixer.SetDone(false);

            playable.SetDone(false);

            bool negativeSpeed = PlayableExtensions.GetSpeed(channel.Mixer) < 0;
            float startTime = negativeSpeed ? 1 : 0;

            // For some reason we gotta set the time twice,
            // otherwise all animation events will trigger instantly
            // and then again at the proper timing.
            playable.SetTime(startTime);
            playable.SetTime(startTime);

            playable.SetSpeed(startSpeed);
            playable.Play();

            IsPlaying = true;
        }

        public void Cancel()
        {
            if (!IsPlaying) return;

            onCancel?.Invoke();
            StopClip();
        }

        private void Complete()
        {
            if (!IsPlaying) return;

            onComplete?.Invoke();
            Active = false;
            StopClip();
        }

        private void StopClip()
        {
            //channel.Mixer.SetInputWeight(InputIndex, 0);
            channel.Mixer.SetDone(true);

            playable.SetDone(true);
            playable.Pause();

            IsPlaying = false;

            //channel.Next();
        }
    }
}