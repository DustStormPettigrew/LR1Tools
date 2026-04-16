using System.Collections.Generic;
using System.Numerics;

namespace LR1Tools.Contracts
{
	public class TrackMaterialAnimationFrame
	{
		public string MaterialName { get; set; }
		public int FrameIndex { get; set; }
		public Vector2 UvOffset { get; set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackMaterialAnimationFrame()
		{
			MaterialName = string.Empty;
			FrameIndex = 0;
			UvOffset = Vector2.Zero;
			Metadata = new Dictionary<string, string>();
		}
	}
}
