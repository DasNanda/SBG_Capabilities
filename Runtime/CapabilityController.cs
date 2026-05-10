using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SBG.Capabilities
{
	public class CapabilityController : MonoBehaviour
	{
#if UNITY_EDITOR
		public Dictionary<TickGroup, List<Capability>> TickGroups => tickGroups;
        public List<CapabilityBlock> TagBlocks => blocks;
#endif

		private Dictionary<TickGroup, List<Capability>> tickGroups = new();
        private List<CapabilityBlock> blocks = new();

        private void Start()
        {
            var capComponents = GetComponentsInChildren<CapabilityComponent>();

			foreach (var component in capComponents)
			{
                AddCapabilities(component);
			}
        }

        private void Update()
        {
            IterateCapabilities(Time.deltaTime, TickGroup.Update);
        }

        private void FixedUpdate()
        {
            IterateCapabilities(Time.fixedDeltaTime, TickGroup.FixedUpdate);
        }

        public void IterateCapabilities(float deltaTime, TickGroup tickGroup = TickGroup.CustomEvent)
        {
            if (!tickGroups.ContainsKey(tickGroup)) return;

            var group = tickGroups[tickGroup];

            foreach (var capability in group)
            {
                if (capability.IsActive)
                {
                    if (capability.ShouldDeactivate())
                    {
                        capability.SetActive(false);
                        continue;
                    }

                    capability.Tick(deltaTime);
                }
                else if (!IsAnyTagBlocked(capability.Tags) && capability.ShouldActivate())
                {
                    capability.SetActive(true);
                }
            }
        }

        public void InterruptTags(params string[] tags) => InterruptTags(null, tags);

        public void InterruptTags(Capability interruptor, params string[] tags)
        {
            foreach (var group in tickGroups.Values)
            {
                foreach (var c in group)
                {
                    if (c == interruptor) continue;

                    c.OnTagInterrupt(interruptor, tags);
                }
            }
        }

        public void BlockTag(object instigator, string tag)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].Tag != tag) continue;

                if (!blocks[i].Instigators.Contains(instigator))
                    blocks[i].Instigators.Add(instigator);

                return;
            }

            blocks.Add(new CapabilityBlock(tag, instigator));
        }

        public void UnblockTag(object instigator, string tag)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].Tag == tag && blocks[i].Instigators.Contains(instigator))
                {
                    blocks[i].Instigators.Remove(instigator);
                    if (blocks[i].Instigators.Count <= 0) blocks.RemoveAt(i);
                    break;
                }
            }
        }

        public void BlockTags(object instigator, params string[] tags)
        {
            foreach (var t in tags) BlockTag(instigator, t);
        }

        public void UnblockTags(object instigator, params string[] tags)
        {
            foreach (var t in tags) UnblockTag(instigator, t);
        }

        public bool IsTagBlocked(string tag) => blocks.Exists(b => b.Tag == tag);

        public bool IsAnyTagBlocked(params string[] tags) => tags.Any(t => IsTagBlocked(t));

        public void AddCapabilities(CapabilityComponent component)
        {
            component.Claim(this);

            foreach (var c in component.Capabilities)
            {
                if (tickGroups.ContainsKey(c.TickGroup))
                {
                    tickGroups[c.TickGroup].Add(c);
                }
                else
                {
                    tickGroups.Add(c.TickGroup, new() { c });
                }
            }

            foreach (var group in tickGroups.Values)
            {
                group.Sort((a, b) => a.CompareTo(b));
            }
        }

        public void RemoveCapabilities(CapabilityComponent component)
        {
            foreach (var group in tickGroups.Values)
            {
                for (int i = group.Count - 1; i >= 0; i--)
                {
                    if (group[i].Owner == component)
                    {
                        group[i].OnOwnerRemoved();
                        group.RemoveAt(i);
                    }
                }
            }
        }
    }
}