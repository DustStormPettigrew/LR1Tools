using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackMesh
	{
		public string Name { get; set; }
		public string MaterialName { get; set; }
		public bool IsCollisionMesh { get; set; }
		public List<TrackVertex> Vertices { get; private set; }
		public List<int> Indices { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackMesh()
		{
			Name = string.Empty;
			MaterialName = string.Empty;
			Vertices = new List<TrackVertex>();
			Indices = new List<int>();
			Metadata = new Dictionary<string, string>();
		}
	}
}

