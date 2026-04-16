using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackMesh
	{
		public string Id { get; set; }
		public string SourceId { get; set; }
		public string SourceName { get; set; }
		public string SourceFormat { get; set; }
		public string SourcePath { get; set; }
		public int? SourceIndex { get; set; }
		public string Name { get; set; }
		public string MaterialName { get; set; }
		public bool IsCollisionMesh { get; set; }
		public List<TrackVertex> Vertices { get; private set; }
		public List<int> Indices { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackMesh()
		{
			Id = string.Empty;
			SourceId = string.Empty;
			SourceName = string.Empty;
			SourceFormat = string.Empty;
			SourcePath = string.Empty;
			SourceIndex = null;
			Name = string.Empty;
			MaterialName = string.Empty;
			Vertices = new List<TrackVertex>();
			Indices = new List<int>();
			Metadata = new Dictionary<string, string>();
		}
	}
}

