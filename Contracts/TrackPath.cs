using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackPath
	{
		public string Name { get; set; }
		public bool Closed { get; set; }
		public List<TrackPathNode> Nodes { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackPath()
		{
			Name = string.Empty;
			Nodes = new List<TrackPathNode>();
			Metadata = new Dictionary<string, string>();
		}
	}
}

