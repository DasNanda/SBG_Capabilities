using SBG.Capabilities.Animation;
using UnityEngine;

namespace SBG.Capabilities
{
	public abstract class BlendtreeCapability : Capability
	{
        public CapabilityBlendtreeAnimation Blendtree => blendtree;

        public virtual bool DrivenByAnimationEvent => false;

        protected CapabilityAnimator animator;

        [SerializeField] protected CapabilityBlendtreeAnimation blendtree;

        protected abstract CapabilityAnimator GetAnimator();

        public override void Setup(CapabilityComponent owner)
        {
            base.Setup(owner);
            animator = GetAnimator();
            AddBlendtree();
        }

        protected virtual void AddBlendtree()
        {
            animator.AddBlendtree($"{name}_blendtree", blendtree, OnAnimCancel);
        }

        protected virtual void RemoveBlendtree()
        {
            animator.RemovePlayable($"{name}_blendtree", blendtree.Channel);
        }

        protected virtual void SetTreeActive(bool active)
        {
            string id = $"{name}_blendtree";

            if (animator.IsPlayableRegistered(id, blendtree.Channel))
            {
                animator.SetActive(id, blendtree.Channel, active);
            }
        }

        protected virtual void OnAnimCancel()
        {
            
        }

        protected override void OnActivated()
        {
            SetTreeActive(true);
        }

        protected override void OnDeactivated()
        {
            SetTreeActive(false);
        }

        protected virtual void SetBlend(Vector2 direction)
        {
            string id = $"{name}_blendtree";

            animator.SetBlendtree(id, blendtree.Channel, direction);
        }

        public override void OnOwnerRemoved()
        {
            RemoveBlendtree();
            base.OnOwnerRemoved();
        }
    }
}