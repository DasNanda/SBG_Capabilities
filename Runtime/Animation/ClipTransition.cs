using System;
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

        public ClipTransition(CapabilityClip from, CapabilityClip to, Action onComplete)
        {
            this.from = from;
            this.to = to;
            this.onComplete = onComplete;

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

        private void SetWeights(float progress)
        {
            from?.SetWeight(1 - progress);
            to.SetWeight(progress);
        }
    }
}