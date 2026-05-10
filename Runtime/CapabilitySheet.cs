using UnityEngine;

namespace SBG.Capabilities.Runtime
{
	[CreateAssetMenu(fileName = "NewSheet", menuName = "SBG/Capabilities/CapabilitySheet")]
	public class CapabilitySheet : ScriptableObject
	{
		public Capability[] Capablities;

		public Capability[] InstantiateSheet(CapabilityComponent owner)
		{
			Capability[] instances = new Capability[Capablities.Length];

			for (int i = 0; i < Capablities.Length; i++)
			{
				instances[i] = Instantiate(Capablities[i]);
				instances[i].Setup(owner);
			}

			return instances;
		}
	}
}