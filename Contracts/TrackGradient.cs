using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackGradient
	{
		public string Id { get; set; }
		public string SourceId { get; set; }
		public string SourceName { get; set; }
		public string SourceFormat { get; set; }
		public string SourcePath { get; set; }
		public int? SourceIndex { get; set; }
		public string Name { get; set; }
		public List<TrackGradientStop> Stops { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackGradient()
		{
			Id = string.Empty;
			SourceId = string.Empty;
			SourceName = string.Empty;
			SourceFormat = string.Empty;
			SourcePath = string.Empty;
			SourceIndex = null;
			Name = string.Empty;
			Stops = new List<TrackGradientStop>();
			Metadata = new Dictionary<string, string>();
		}
	}
}

