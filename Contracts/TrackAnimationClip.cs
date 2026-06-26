using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackAnimationClip
	{
		public string Id { get; set; }
		public string SourceId { get; set; }
		public string SourceName { get; set; }
		public string SourceFormat { get; set; }
		public string SourcePath { get; set; }
		public int? SourceIndex { get; set; }
		public string Name { get; set; }
		public string LoopMode { get; set; }
		public int? FrameCount { get; set; }
		public float Speed { get; set; }
		public List<TrackAnimationChannel> Channels { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackAnimationClip()
		{
			Id = string.Empty;
			SourceId = string.Empty;
			SourceName = string.Empty;
			SourceFormat = string.Empty;
			SourcePath = string.Empty;
			SourceIndex = null;
			Name = string.Empty;
			LoopMode = string.Empty;
			FrameCount = null;
			Speed = 0f;
			Channels = new List<TrackAnimationChannel>();
			Metadata = new Dictionary<string, string>();
		}
	}
}
