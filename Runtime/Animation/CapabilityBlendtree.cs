using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SBG.Capabilities.Animation
{
	internal class CapabilityBlendtree : ICapabilityPlayable
	{
        public AnimationMixerPlayable Mixer { get; private set; }

        public string Id { get; private set; }
        public int InputIndex { get; private set; }
        public int Priority { get; private set; }
        public TransitionLength InTransitionLength { get; private set; }
        public TransitionLength OutTransitionLength { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool Active { get; set; }

        private CapabilityChannel channel;
        private event Action onCancel;

        private Vector2 blend;

        private AnimationClipPlayable playableCenter;
        private AnimationClipPlayable playableForward;
        private AnimationClipPlayable playableBackward;
        private AnimationClipPlayable playableLeft;
        private AnimationClipPlayable playableRight;


        public CapabilityBlendtree(CapabilityChannel parentChannel, string id, CapabilityBlendtreeAnimation blendtree, Action onCancel)
        {
            channel = parentChannel;
            InputIndex = channel.Mixer.GetInputCount();

            this.Mixer = AnimationMixerPlayable.Create(channel.Mixer.GetGraph());
            channel.Mixer.SetInputCount(InputIndex + 1);
            channel.Mixer.ConnectInput(InputIndex, this.Mixer, 0, 0);

            Id = id;
            Priority = blendtree.Priority;
            InTransitionLength = blendtree.InTransitionLength;
            OutTransitionLength = blendtree.OutTransitionLength;
            
            this.onCancel = onCancel;
            blend = Vector2.zero;

            // Add Directional Clips
            this.Mixer.SetInputCount(5);
            var graph = this.Mixer.GetGraph();

            playableCenter = AnimationClipPlayable.Create(graph, blendtree.ClipCenter);
            playableForward = AnimationClipPlayable.Create(graph, blendtree.ClipForward);
            playableBackward = AnimationClipPlayable.Create(graph, blendtree.ClipBackward);
            playableLeft = AnimationClipPlayable.Create(graph, blendtree.ClipLeft);
            playableRight = AnimationClipPlayable.Create(graph, blendtree.ClipRight);

            this.Mixer.ConnectInput(0, playableCenter, 0, 1);
            this.Mixer.ConnectInput(1, playableForward, 0, 0);
            this.Mixer.ConnectInput(2, playableBackward, 0, 0);
            this.Mixer.ConnectInput(3, playableLeft, 0, 0);
            this.Mixer.ConnectInput(4, playableRight, 0, 0);
        }

        public void SetWeight(float weight)
        {
            channel.Mixer.SetInputWeight(InputIndex, weight);

            if (weight >= 1) this.Mixer.SetSpeed(1);
        }

        public float GetWeight()
        {
            return channel.Mixer.GetInputWeight(InputIndex);
        }

        public void SetBlend(Vector2 direction)
        {
            blend = direction;

            float weightForward = Mathf.Clamp01(blend.y);
            float weightBackward = Mathf.Clamp01(-blend.y);
            float weightLeft = Mathf.Clamp01(-blend.x);
            float weightRight = Mathf.Clamp01(blend.x);

            float maxX = Mathf.Max(weightLeft, weightRight);
            float maxY = Mathf.Max(weightForward, weightBackward);

            float weightX = maxX * Mathf.Lerp(1f, 0.5f, maxY);
            float weightY = maxY * Mathf.Lerp(1f, 0.5f, maxX);

            float weightCenter = Mathf.Clamp01(1 - (weightX + weightY));

            // Directions
            this.Mixer.SetInputWeight(1, weightForward * weightY);
            this.Mixer.SetInputWeight(2, weightBackward * weightY);
            this.Mixer.SetInputWeight(3, weightLeft * weightX);
            this.Mixer.SetInputWeight(4, weightRight * weightX);

            // Center
            this.Mixer.SetInputWeight(0, weightCenter);
        }

        public void Play(float startSpeed = 0)
        {
            channel.Mixer.SetInputWeight(InputIndex, 0);
            channel.Mixer.SetDone(false);

            this.Mixer.SetDone(false);

            bool negativeSpeed = PlayableExtensions.GetSpeed(channel.Mixer) < 0;
            float startTime = negativeSpeed ? 1 : 0;

            // For some reason we gotta set the time twice,
            // otherwise all animation events will trigger instantly
            // and then again at the proper timing.
            this.Mixer.SetTime(startTime);
            this.Mixer.SetTime(startTime);

            this.Mixer.SetSpeed(startSpeed);
            this.Mixer.Play();

            IsPlaying = true;
        }

        public void Cancel()
        {
            if (!IsPlaying) return;

            onCancel?.Invoke();
            StopClips();
        }

        private void StopClips()
        {
            channel.Mixer.SetDone(true);

            this.Mixer.SetSpeed(0);
            this.Mixer.SetDone(true);
            this.Mixer.Pause();

            IsPlaying = false;
        }
    }
}