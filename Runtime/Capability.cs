using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SBG.Capabilities.Runtime
{
	public abstract class Capability : ScriptableObject, IComparable
	{
		public abstract string DisplayName { get; }

		public CapabilityComponent Owner { get; private set; }
		public bool IsActive => isActive;
        public abstract string[] Tags { get; }
		public abstract TickGroup TickGroup { get; }
		public abstract int TickOrder { get; }
		public virtual bool IsCompound => false;

		[HideInInspector] public Capability Parent;
        [HideInInspector] public List<Capability> Children;

        protected bool isActive;

#if UNITY_EDITOR
        [HideInInspector] public bool IsExpanded = false;
		public float LastStateChangeTime { get; private set; }
#endif

		public virtual void Setup(CapabilityComponent owner)
		{
			Owner = owner;

            // Instantiate Children
            if (IsCompound)
			{
				if (Children == null) Children = new();
                for (int i = 0; i < Children.Count; i++)
                {
                    Children[i] = Instantiate(Children[i]);
                    Children[i].Setup(owner);

#if UNITY_EDITOR
                    if (TickGroup != Children[i].TickGroup)
                    {
                        Debug.LogWarning($"Tick Group Mismatch on Child Capability: {Children[i].DisplayName}!");
                    }
#endif
                }
            }
        }

		public void SetActive(bool isActive)
		{
			if (this.isActive == isActive) return;

			this.isActive = isActive;

#if UNITY_EDITOR
			LastStateChangeTime = Time.realtimeSinceStartup;
#endif

			if (this.isActive)
            {
                OnActivated();
            }
            else
            {
                if (IsCompound) DisableChildren();
                OnDeactivated();
            }
		}

		public abstract bool ShouldActivate();
        public abstract bool ShouldDeactivate();
		public virtual void Tick(float deltaTime)
        {
            if (IsCompound) TickChildren(deltaTime);
        }

        protected abstract void OnActivated();
        protected abstract void OnDeactivated();

        private void TickChildren(float deltaTime)
        {
            foreach (var child in Children)
            {
                if (child.IsActive)
                {
                    if (child.ShouldDeactivate())
                    {
                        child.SetActive(false);
                        continue;
                    }

                    child.Tick(deltaTime);
                    continue;
                }
                else if (!Owner.IsAnyTagBlocked(child.Tags) && child.ShouldActivate())
                {
                    child.SetActive(true);
                }
            }
        }

        private void DisableChildren()
        {
            if (Children == null) return;

            foreach (var capability in Children)
            {
                if (capability.IsActive) capability.SetActive(false);
            }
        }

        public void OnTagInterrupt(Capability interruptor, string[] tags)
        {
            if (!isActive) return;

            if (this != interruptor && Tags.All(t => tags.Contains(t)))
            {
                SetActive(false);
                return;
            }

            if (!IsCompound) return;

            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].OnTagInterrupt(interruptor, tags);
            }
        }

        public virtual void OnOwnerRemoved()
		{
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.OnOwnerRemoved();
                }
            }

            Destroy(this);
		}

        public int CompareTo(object obj)
        {
			Capability other = obj as Capability;
            if (this.TickGroup == other.TickGroup) return this.TickOrder.CompareTo(other.TickOrder);
            else return this.TickGroup.CompareTo(other.TickGroup);
        }
    }
}