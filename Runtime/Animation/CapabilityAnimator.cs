using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SBG.Capabilities.Animation
{
	[RequireComponent(typeof(Animator))]
	public class CapabilityAnimator : MonoBehaviour
    {
		private Animator animator;
		private PlayableGraph graph;
		private AnimationLayerMixerPlayable sourceChannelMixer;

        private Dictionary<string, CapabilityChannel> channels = new();

		private void Awake()
		{
			animator = GetComponent<Animator>();
			animator.runtimeAnimatorController = null;

			graph = PlayableGraph.Create(gameObject.name);

            DirectorUpdateMode updateMode = DirectorUpdateMode.GameTime;
            if (animator.updateMode == AnimatorUpdateMode.UnscaledTime) updateMode = DirectorUpdateMode.UnscaledGameTime;
            graph.SetTimeUpdateMode(updateMode);

            sourceChannelMixer = AnimationLayerMixerPlayable.Create(graph);

            var output = AnimationPlayableOutput.Create(graph, "Output", animator);
			output.SetSourcePlayable(sourceChannelMixer);

			graph.Play();
		}

        private void Update()
        {
            foreach (var channel in channels.Values)
            {
                channel.Update();
            }
        }

        public void SetSpeed(float speed, string channelId)
        {
            if (!channels.TryGetValue(channelId, out var channel)) return;

            PlayableExtensions.SetSpeed(channel.Mixer, speed);
        }

        public void SetBlendtree(string id, string channelId, Vector2 direction)
        {
            if (!channels.TryGetValue(channelId, out var channel)) return;

            channel.SetBlendtree(id, direction);
        }

        public void AddClip(string clipId, string channelId, int priority, TransitionLength inLength, TransitionLength outLength, AnimationClip clip, Action onComplete = null, Action onCancel = null)
		{
			if (!channels.ContainsKey(channelId)) AddChannel(channelId);

			var channel = channels[channelId];
			channel.AddClip(clipId, priority, inLength, outLength, clip, onComplete, onCancel);
        }

        public void AddBlendtree(string id, CapabilityBlendtreeAnimation blendtree, Action onCancel)
        {
            string channelId = blendtree.Channel;

            if (!channels.ContainsKey(channelId)) AddChannel(channelId);
            var channel = channels[channelId];
            channel.AddBlendtree(id, blendtree, onCancel);
        }

        public void RemovePlayable(string clipId, string channelId)
        {
            if (!channels.ContainsKey(channelId)) return;

            channels[channelId].RemovePlayable(clipId);
        }

        public void SetActive(string clipId, string channelId, bool active)
        {
            channels[channelId].SetActive(clipId, active);
        }

        public bool IsPlayableRegistered(string clipId, string channelId)
        {
            return channels[channelId].IsPlayableRegistered(clipId);
        }

        private void AddChannel(string id)
        {
            var mixer = AnimationMixerPlayable.Create(graph);
            sourceChannelMixer.AddInput(mixer, 0, 1);

            channels.Add(id, new CapabilityChannel(mixer));
        }

        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}