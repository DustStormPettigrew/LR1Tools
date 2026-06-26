using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackAnimationChannel
	{
		public string Id { get; set; }
		public string SourceId { get; set; }
		public string SourceName { get; set; }
		public string SourceFormat { get; set; }
		public string SourcePath { get; set; }
		public int? SourceIndex { get; set; }
		public string Name { get; set; }
		public string Property { get; set; }
		public string ValueType { get; set; }
		public string Interpolation { get; set; }
		public TrackAnimationTarget Target { get; set; }
		public List<TrackAnimationKeyframe> Keyframes { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackAnimationChannel()
		{
			Id = string.Empty;
			SourceId = string.Empty;
			SourceName = string.Empty;
			SourceFormat = string.Empty;
			SourcePath = string.Empty;
			SourceIndex = null;
			Name = string.Empty;
			Property = string.Empty;
			ValueType = string.Empty;
			Interpolation = string.Empty;
			Target = new TrackAnimationTarget();
			Keyframes = new List<TrackAnimationKeyframe>();
			Metadata = new Dictionary<string, string>();
		}
	}
}
