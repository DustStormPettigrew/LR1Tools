using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackScene
	{
		public string Name { get; set; }
		public string SourceName { get; set; }
		public List<TrackMesh> Meshes { get; private set; }
		public List<TrackMaterial> Materials { get; private set; }
		public List<TrackObject> Objects { get; private set; }
		public List<TrackPath> Paths { get; private set; }
		public List<TrackGradient> Gradients { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackScene()
		{
			Name = string.Empty;
			SourceName = string.Empty;
			Meshes = new List<TrackMesh>();
			Materials = new List<TrackMaterial>();
			Objects = new List<TrackObject>();
			Paths = new List<TrackPath>();
			Gradients = new List<TrackGradient>();
			Metadata = new Dictionary<string, string>();
		}
	}
}

