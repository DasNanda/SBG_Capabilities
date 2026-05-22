using SBG.Capabilities.Animation;
using UnityEngine;

namespace SBG.Capabilities
{
	public abstract class AnimatedCapability : Capability
	{
        public CapabilityAnimation Animation => animation;

        public virtual bool DrivenByAnimationEvent => false;

        protected CapabilityAnimator animator;

        [SerializeField] protected CapabilityAnimation animation;

        private string lastAnimSelector;
        protected bool animationFinished;

        protected abstract string GetAnimSelector();
        protected abstract CapabilityAnimator GetAnimator();

        public override void Setup(CapabilityComponent owner)
        {
            base.Setup(owner);
            animator = GetAnimator();
            AddClips();
        }

        protected virtual void AddClips()
        {
            foreach (var entry in animation.Clips)
            {
                if (string.IsNullOrEmpty(entry.SpecifierId) || entry.Clip == null) continue;

                string clipId = $"{name}_{entry.SpecifierId}";
                animator.AddClip(clipId, animation.Channel, animation.Priority, animation.InTransitionLength, animation.OutTransitionLength, entry.Clip, OnAnimComplete, OnAnimCancel);
            }

            if (animation.fallbackClip != null)
            {
                string clipId = $"{name}_fallback";
                animator.AddClip(clipId, animation.Channel, animation.Priority, animation.InTransitionLength, animation.OutTransitionLength, animation.fallbackClip, OnAnimComplete, OnAnimCancel);
            }
        }

        protected virtual void RemoveClips()
        {
            foreach (var entry in animation.Clips)
            {
                if (string.IsNullOrEmpty(entry.SpecifierId) || entry.Clip == null) continue;

                string clipId = $"{name}_{entry.SpecifierId}";
                animator.RemoveClip(clipId, animation.Channel);
            }

            if (animation.fallbackClip != null)
            {
                string clipId = $"{name}_fallback";
                animator.RemoveClip(clipId, animation.Channel);
            }
        }

        protected virtual void SetClipActive(string selectorId, bool active)
        {
            string id = $"{name}_{selectorId}";

            if (animator.IsClipRegistered(id, animation.Channel))
            {
                animator.SetActive(id, animation.Channel, active);
            }
            else if (animation.fallbackClip != null)
            {
                id = $"{name}_fallback";
                animator.SetActive(id, animation.Channel, active);
            }

            lastAnimSelector = selectorId;
        }

        protected virtual void OnAnimComplete()
        {
            animationFinished = true;
        }

        protected virtual void OnAnimCancel()
        {
            animationFinished = true;
        }

        protected override void OnActivated()
        {
            animationFinished = false;
            SetClipActive(GetAnimSelector(), true);
        }

        protected override void OnDeactivated()
        {
            SetClipActive(lastAnimSelector, false);
        }

        protected virtual void ChangeClip(string selectorId)
        {
            SetClipActive(lastAnimSelector, false);
            SetClipActive(selectorId, true);
        }

        public override void OnOwnerRemoved()
        {
            RemoveClips();
            base.OnOwnerRemoved();
        }
    }
}