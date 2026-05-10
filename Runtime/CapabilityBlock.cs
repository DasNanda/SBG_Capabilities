using System.Collections.Generic;

namespace SBG.Capabilities
{
	[System.Serializable]
	public struct CapabilityBlock
	{
		public string Tag;
		public List<object> Instigators;
	
		public CapabilityBlock(string tag, object initialInstigator)
		{
			Tag = tag;
			Instigators = new();
			Instigators.Add(initialInstigator);
		}
	}
}