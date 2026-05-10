using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SBG.Capabilities.Runtime.Animation
{
	internal class CapabilityChannel
	{
        public AnimationMixerPlayable Mixer { get; private set; }

        private Dictionary<string, CapabilityClip> clips = new();
        private CapabilityClip currentClip;
        private ClipTransition currentTransition;

        private CapabilityClip lastClip;

        public CapabilityChannel(AnimationMixerPlayable playable)
        {
            this.Mixer = playable;
        }

        public void AddClip(string id, int priority, TransitionLength inLength, TransitionLength outLength, AnimationClip clip, Action onComplete, Action onCancel)
        {
            if (clips.ContainsKey(id))
            {
                Debug.LogError($"Clip '{id}' already exists!");
                return;
            }

            clips.Add(id, new CapabilityClip(clip, this, id, priority, inLength, outLength, onComplete, onCancel));
        }

        public void RemoveClip(string id)
        {
            if (!clips.ContainsKey(id)) return;

            int inputIndex = clips[id].InputIndex;
            var playable = Mixer.GetInput(inputIndex);

            Mixer.DisconnectInput(inputIndex);
            playable.Destroy();

            clips.Remove(id);

            Debug.Log($"Removed Clip {id}");
        }

        public bool IsClipRegistered(string id)
        {
            return clips.ContainsKey(id);
        }

        public void Update()
        {
            if (currentTransition != null) currentTransition.Update();
            else if (currentClip != null) currentClip.TryComplete();
        }

        public void SetActive(string id, bool active)
        {
            if (!clips.TryGetValue(id, out var clip))
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
            if (currentClip == clip) return;

            // Play first
            if (currentClip == null)
            {
                Transition(lastClip, clip);
                return;
            }

            // Interrupt previous (will trigger the clip to play when Next() is called in Cancel
            if (clip.Priority < currentClip.Priority)
            {
                currentClip.Cancel();
            }
        }

        public void Next()
        {
            ClearTransition();

            int highestPrio = int.MaxValue;
            lastClip = currentClip;
            currentClip = null;

            foreach (var clip in clips.Values)
            {
                if (!clip.Active) continue;

                if (clip.Priority < highestPrio)
                {
                    highestPrio = clip.Priority;
                    currentClip = clip;
                }
            }

            if (currentClip != null) Transition(lastClip, currentClip);
        }

        private void Transition(CapabilityClip from, CapabilityClip to)
        {
            currentClip = to;
            currentClip?.Play();
            currentTransition = new ClipTransition(from, to, ClearTransition);
        }

        private void ClearTransition()
        {
            if (currentTransition != null)
            {
                currentTransition.Stop();
                currentTransition = null;
                lastClip = null;
            }
        }
    }
}