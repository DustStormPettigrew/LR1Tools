using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackAnimationTarget
	{
		public string Id { get; set; }
		public string SourceId { get; set; }
		public string SourceName { get; set; }
		public string SourceFormat { get; set; }
		public string SourcePath { get; set; }
		public int? SourceIndex { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public string Path { get; set; }
		public string Slot { get; set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackAnimationTarget()
		{
			Id = string.Empty;
			SourceId = string.Empty;
			SourceName = string.Empty;
			SourceFormat = string.Empty;
			SourcePath = string.Empty;
			SourceIndex = null;
			Name = string.Empty;
			Type = string.Empty;
			Path = string.Empty;
			Slot = string.Empty;
			Metadata = new Dictionary<string, string>();
		}
	}
}
