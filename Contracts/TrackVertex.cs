using System.Collections.Generic;
using System.Numerics;

namespace LR1Tools.Contracts
{
	public class TrackVertex
	{
		public Vector3 Position { get; set; }
		public Vector3 Normal { get; set; }
		public Vector2 PrimaryTexCoord { get; set; }
		public TrackColor Color { get; set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackVertex()
		{
			Position = Vector3.Zero;
			Normal = Vector3.Zero;
			PrimaryTexCoord = Vector2.Zero;
			Color = new TrackColor(1f, 1f, 1f, 1f);
			Metadata = new Dictionary<string, string>();
		}
	}
}

