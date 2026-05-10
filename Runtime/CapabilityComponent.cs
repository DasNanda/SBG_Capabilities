using SBG.Capabilities.Runtime.Animation;
using UnityEngine;

namespace SBG.Capabilities.Runtime
{
	public class CapabilityComponent : MonoBehaviour
	{
        public Capability[] Capabilities
        {
            get
            {
                if (capabilities == null) capabilities = sheet.InstantiateSheet(this);

                return capabilities;
            }
        }

		[SerializeField] private CapabilitySheet sheet;

        protected Capability[] capabilities;
        protected CapabilityController controller;

        internal bool Claim(CapabilityController controller)
        {
            if (this.controller != null) return false;

            this.controller = controller;
            return true;
        }

        public void InterruptTags(Capability interruptor, params string[] tags) => controller.InterruptTags(interruptor, tags);
        public void InterruptTags(params string[] tags) => controller.InterruptTags(tags);

        public void BlockTags(object instigator, params string[] tags) => controller.BlockTags(instigator, tags);
        public void BlockTag(object instigator, string tag) => controller.BlockTag(instigator, tag);

        public void UnblockTags(object instigator, params string[] tags) => controller.UnblockTags(instigator, tags);
        public void UnblockTag(object instigator, string tag) => controller.UnblockTag(instigator, tag);

        public bool IsTagBlocked(string tag) => controller.IsTagBlocked(tag);

        public bool IsAnyTagBlocked(params string[] tags) => controller.IsAnyTagBlocked(tags);
    }
}