using System.Collections.Generic;
using System.Numerics;

namespace LR1Tools.Contracts
{
	public class TrackMaterialAnimation
	{
		public string Id { get; set; }
		public string SourceId { get; set; }
		public string SourceName { get; set; }
		public string SourceFormat { get; set; }
		public string SourcePath { get; set; }
		public int? SourceIndex { get; set; }
		public string Name { get; set; }
		public string MaterialName { get; set; }
		public string Behavior { get; set; }
		public string LoopMode { get; set; }
		public int? FrameCount { get; set; }
		public float Speed { get; set; }
		public Vector2 UvOffset { get; set; }
		public Vector2 UvVelocity { get; set; }
		public List<TrackMaterialAnimationFrame> Frames { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackMaterialAnimation()
		{
			Id = string.Empty;
			SourceId = string.Empty;
			SourceName = string.Empty;
			SourceFormat = string.Empty;
			SourcePath = string.Empty;
			SourceIndex = null;
			Name = string.Empty;
			MaterialName = string.Empty;
			Behavior = string.Empty;
			LoopMode = string.Empty;
			FrameCount = null;
			Speed = 0f;
			UvOffset = Vector2.Zero;
			UvVelocity = Vector2.Zero;
			Frames = new List<TrackMaterialAnimationFrame>();
			Metadata = new Dictionary<string, string>();
		}
	}
}
