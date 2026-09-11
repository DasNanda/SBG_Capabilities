using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SBG.Capabilities.Animation
{
	internal class CapabilityChannel
	{
        public AnimationMixerPlayable Mixer { get; private set; }

        private Dictionary<string, ICapabilityPlayable> playables = new();
        private ICapabilityPlayable currentPlayable;
        private ClipTransition currentTransition;
        private bool currentIsBlendtree;

        private ICapabilityPlayable lastClip;

        public CapabilityChannel(AnimationMixerPlayable playable)
        {
            this.Mixer = playable;
        }

        public void AddClip(string id, int priority, TransitionLength inLength, TransitionLength outLength, AnimationClip clip, Action onComplete, Action onCancel)
        {
            if (playables.ContainsKey(id))
            {
                Debug.LogError($"Clip '{id}' already exists!");
                return;
            }

            playables.Add(id, new CapabilityClip(clip, this, id, priority, inLength, outLength, onComplete, onCancel));
        }

        public void AddBlendtree(string id, CapabilityBlendtreeAnimation blendtree, Action onCancel)
        {
            if (playables.ContainsKey(id))
            {
                Debug.LogError($"Clip '{id}' already exists!");
                return;
            }

            playables.Add(id, new CapabilityBlendtree(this, id, blendtree, onCancel));
        }

        public void RemovePlayable(string id)
        {
            if (!playables.ContainsKey(id)) return;

            int inputIndex = playables[id].InputIndex;
            var playable = Mixer.GetInput(inputIndex);

            Mixer.DisconnectInput(inputIndex);
            playable.Destroy();

            playables.Remove(id);
        }

        public bool IsPlayableRegistered(string id)
        {
            return playables.ContainsKey(id);
        }

        public void Update()
        {
            if (currentTransition != null)
            {
                currentTransition.Update();
            }
            else if (currentPlayable != null && !currentIsBlendtree)
            {
                if (currentPlayable.IsPlaying) (currentPlayable as CapabilityClip).TryComplete();
                else Next();
            }
        }

        public void SetActive(string id, bool active)
        {
            if (!playables.TryGetValue(id, out var clip))
            {
                Debug.LogError($"Clip {id} not found!");
                return;
            }

            clip.Active = active;

            // If active set to false, cancel the clip and/or just return
            if (!clip.Active)
            {
                if (clip.IsPlaying) clip.Cancel();
                return;
            }

            // Already Playing Target Clip
            if (currentPlayable == clip) return;

            // Play first
            if (currentPlayable == null)
            {
                Transition(lastClip, clip);
                return;
            }

            // Interrupt previous
            if (clip.Priority < currentPlayable.Priority)
            {
                currentPlayable.Cancel();
            }
        }

        public void SetBlendtree(string id, Vector2 direction)
        {
            if (!playables.TryGetValue(id, out var clip))
            {
                Debug.LogError($"Clip {id} not found!");
                return;
            }

            (clip as CapabilityBlendtree).SetBlend(direction);
        }

        public void Next()
        {
            int highestPrio = int.MaxValue;
            ICapabilityPlayable newClip = null;

            foreach (var clip in playables.Values)
            {
                if (!clip.Active) continue;

                if (clip.Priority < highestPrio)
                {
                    highestPrio = clip.Priority;
                    newClip = clip;
                }
            }

            if (newClip == currentPlayable)
            {
                currentPlayable.Play(1);
                currentPlayable.SetWeight(1);
                return;
            }

            // When transitioning back and forth quickly, set up the transition so it doesnt snap to the end
            if (newClip != null && lastClip == newClip) currentTransition = null;
            else ClearTransition();

            lastClip = currentPlayable;
            currentPlayable = newClip;
            currentIsBlendtree = currentPlayable is CapabilityBlendtree;

            if (currentPlayable != null) Transition(lastClip, currentPlayable);
        }

        private void Transition(ICapabilityPlayable from, ICapabilityPlayable to)
        {
            bool crossfade = false;
            if (from != null && from.OutTransitionLength.IsUsed && from.OutTransitionLength.ForceCrossfade) crossfade = true;
            else if (to != null && to.InTransitionLength.IsUsed && to.InTransitionLength.ForceCrossfade) crossfade = true;

            currentPlayable = to;
            currentIsBlendtree = currentPlayable is CapabilityBlendtree;

            currentPlayable?.Play(crossfade ? 1 : 0);
            currentTransition = new ClipTransition(from, to, ClearTransition);
        }

        private void ClearTransition()
        {
            if (currentTransition != null)
            {
                currentTransition.SnapToEndState();
                currentTransition = null;
                lastClip = null;
            }
        }
    }
}