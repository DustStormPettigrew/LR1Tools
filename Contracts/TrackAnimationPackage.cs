using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackAnimationPackage
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
		public List<TrackAnimationClip> Clips { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackAnimationPackage()
		{
			Id = string.Empty;
			SourceId = string.Empty;
			ExportType = TrackSceneExportTypes.AnimationSet;
			Name = string.Empty;
			SourceName = string.Empty;
			SourceFormat = string.Empty;
			SourcePath = string.Empty;
			SourceIndex = null;
			CoordinateSystem = new TrackCoordinateSystem();
			Clips = new List<TrackAnimationClip>();
			Metadata = new Dictionary<string, string>();
		}
	}
}
