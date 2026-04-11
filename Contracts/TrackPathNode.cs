using System.Collections.Generic;
using System.Numerics;

namespace LR1Tools.Contracts
{
	public class TrackPathNode
	{
		public Vector3 Position { get; set; }
		public Vector3 Forward { get; set; }
		public Vector3 Up { get; set; }
		public float Width { get; set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackPathNode()
		{
			Position = Vector3.Zero;
			Forward = Vector3.UnitZ;
			Up = Vector3.UnitY;
			Width = 0f;
			Metadata = new Dictionary<string, string>();
		}
	}
}

