using System;
using UnityEngine;

namespace SBG.Capabilities.Animation
{
	internal class ClipTransition
	{
        private ICapabilityPlayable from;
        private ICapabilityPlayable to;
        private float duration;
        private float startTime;
        private Action onComplete;

        private float fromStartWeight;
        private float toStartWeight;

        public ClipTransition(ICapabilityPlayable from, ICapabilityPlayable to, Action onComplete)
        {
            this.from = from;
            this.to = to;
            this.onComplete = onComplete;

            if (from != null) fromStartWeight = from.GetWeight();
            toStartWeight = to.GetWeight();

            duration = GetDuration(from, to);
            startTime = Time.time;

            if (duration <= 0)
            {
                SetWeights(1);
                onComplete?.Invoke();
            }
        }

        private float GetDuration(ICapabilityPlayable from, ICapabilityPlayable to)
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

        public void SnapToEndState() => SetWeights(1);

        private void SetWeights(float progress)
        {
            if (from != null)
            {
                float fromWeight = Mathf.Lerp(fromStartWeight, 0, progress);
                from?.SetWeight(fromWeight);
            }

            float toWeight = Mathf.Lerp(toStartWeight, 1, progress);
            to.SetWeight(toWeight);
        }
    }
}