using System.Collections.Generic;
using UnityEngine;

namespace SBG.Capabilities
{
	[CreateAssetMenu(fileName = "NewSheet", menuName = "SBG/Capabilities/CapabilitySheet")]
	public class CapabilitySheet : ScriptableObject
	{
		public Capability[] Capablities;

		public Capability[] InstantiateSheet(CapabilityComponent owner)
		{
			var instances = new List<Capability>();

			foreach (var item in this.Capablities)
			{
				var cap = Instantiate(item);
				cap.Setup(owner);
                instances.Add(cap);
			}

			return SortTickOrder(instances).ToArray();
		}

		public static List<Capability> SortTickOrder(List<Capability> list)
        {
            if (list == null) return null;

            list.Sort((a, b) => a.CompareTo(b));

			foreach (var item in list)
			{
				if (item.IsCompound) item.Children = SortTickOrder(item.Children);
			}

            return list;
        }
    }
}