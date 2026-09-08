using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SBG.Capabilities.Animation
{
	internal class ClipTransition
	{
        private CapabilityClip from;
        private CapabilityClip to;
        private float duration;
        private float startTime;
        private Action onComplete;
        private CapabilityClip[] lingerClips;
        private float[] lingerClipStartWeights;

        public ClipTransition(CapabilityClip from, CapabilityClip to, Action onComplete, CapabilityClip[] lingerClips=null)
        {
            this.from = from;
            this.to = to;
            this.onComplete = onComplete;
            this.lingerClips = lingerClips;

            InitLingerClipWeights();

            duration = GetDuration(from, to);
            startTime = Time.time;

            if (duration <= 0)
            {
                SetWeights(1);
                onComplete?.Invoke();
            }
        }

        private float GetDuration(CapabilityClip from, CapabilityClip to)
        {
            float result = float.MaxValue;
            float max = float.MaxValue;
            bool validDuration = false;

            if (from != null && from.OutTransitionLength.IsUsed)
            {
                result = from.OutTransitionLength.PreferedLength;
                max = from.OutTransitionLength.MaxLength;
                validDuration = true;
            }

            if (to != null && to.InTransitionLength.IsUsed)
            {
                max = Mathf.Min(max, to.InTransitionLength.MaxLength);
                result = Mathf.Max(result, to.InTransitionLength.PreferedLength);
                result = Mathf.Min(result, max);
                validDuration = true;
            }

            return validDuration ? result : 0;
        }

        public void Update()
        {
            float progress = (Time.time - startTime) / duration;

            if (progress >= 1)
            {
                SetWeights(1);
                onComplete?.Invoke();
            }
            else
            {
                SetWeights(progress);
            }
        }

        public void Stop() => SetWeights(1);

        public CapabilityClip[] GetLingerClips()
        {
            List<CapabilityClip> lingers = new();

            if (from != null && from.GetWeight() > 0) lingers.Add(from);

            if (lingerClips != null)
            {
                foreach (var clip in lingerClips)
                {
                    if (clip.GetWeight() > 0) lingers.Add(clip);
                }
            }

            return lingers.ToArray();
        }

        private void SetWeights(float progress)
        {
            UpdateLingerClips(1 - progress);
            from?.SetWeight(1 - progress);
            to.SetWeight(progress);
        }

        private void InitLingerClipWeights()
        {
            if (lingerClips == null) return;

            lingerClipStartWeights = new float[lingerClips.Length];

            for (int i = 0; i < lingerClips.Length; i++)
            {
                lingerClipStartWeights[i] = lingerClips[i].GetWeight();
            }
        }

        private void UpdateLingerClips(float progress)
        {
            if (lingerClips == null) return;

            for (int i = 0; i < lingerClips.Length; i++)
            {
                float weight = Mathf.Lerp(lingerClipStartWeights[i], 0, progress);
                lingerClips[i].SetWeight(weight);
            }
        }
    }
}