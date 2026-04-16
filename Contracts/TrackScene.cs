using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackScene
	{
		public string Id { get; set; }
		public string SourceId { get; set; }
		public string ExportType { get; set; }
		public string Name { get; set; }
		public string SourceName { get; set; }
		public string SourceFormat { get; set; }
		public string SourcePath { get; set; }
		public int? SourceIndex { get; set; }
		public TrackCoordinateSystem CoordinateSystem { get; set; }
		public List<TrackMesh> Meshes { get; private set; }
		public List<TrackMaterial> Materials { get; private set; }
		public List<TrackObject> Objects { get; private set; }
		public List<TrackObject> StartPositions { get; private set; }
		public List<TrackObject> Checkpoints { get; private set; }
		public List<TrackObject> Powerups { get; private set; }
		public List<TrackObject> Hazards { get; private set; }
		public List<TrackObject> Emitters { get; private set; }
		public List<TrackPath> Paths { get; private set; }
		public List<TrackPath> NpcPaths { get; private set; }
		public List<TrackMaterialAnimation> MaterialAnimations { get; private set; }
		public List<TrackGradient> Gradients { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackScene()
		{
			Id = string.Empty;
			SourceId = string.Empty;
			ExportType = TrackSceneExportTypes.Scene;
			Name = string.Empty;
			SourceName = string.Empty;
			SourceFormat = string.Empty;
			SourcePath = string.Empty;
			SourceIndex = null;
			CoordinateSystem = new TrackCoordinateSystem();
			Meshes = new List<TrackMesh>();
			Materials = new List<TrackMaterial>();
			Objects = new List<TrackObject>();
			StartPositions = new List<TrackObject>();
			Checkpoints = new List<TrackObject>();
			Powerups = new List<TrackObject>();
			Hazards = new List<TrackObject>();
			Emitters = new List<TrackObject>();
			Paths = new List<TrackPath>();
			NpcPaths = new List<TrackPath>();
			MaterialAnimations = new List<TrackMaterialAnimation>();
			Gradients = new List<TrackGradient>();
			Metadata = new Dictionary<string, string>();
		}
	}
}

