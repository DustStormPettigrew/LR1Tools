using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackObject
	{
		public string Id { get; set; }
		public string SourceId { get; set; }
		public string SourceName { get; set; }
		public string SourceFormat { get; set; }
		public string SourcePath { get; set; }
		public int? SourceIndex { get; set; }
		public string Name { get; set; }
		public string MeshName { get; set; }
		public string MaterialName { get; set; }
		public string PathName { get; set; }
		public string AnimationRef { get; set; }
		public string MaterialAnimationRef { get; set; }
		public string AnimationSourceName { get; set; }
		public string AnimationSourcePath { get; set; }
		public bool Visible { get; set; }
		public TrackTransform Transform { get; set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackObject()
		{
			Id = string.Empty;
			SourceId = string.Empty;
			SourceName = string.Empty;
			SourceFormat = string.Empty;
			SourcePath = string.Empty;
			SourceIndex = null;
			Name = string.Empty;
			MeshName = string.Empty;
			MaterialName = string.Empty;
			PathName = string.Empty;
			AnimationRef = string.Empty;
			MaterialAnimationRef = string.Empty;
			AnimationSourceName = string.Empty;
			AnimationSourcePath = string.Empty;
			Visible = true;
			Transform = new TrackTransform();
			Metadata = new Dictionary<string, string>();
		}
	}
}

