using System.Numerics;

namespace LR1Tools.Contracts
{
	public class TrackTransform
	{
		public Vector3 Position { get; set; }
		public Quaternion Rotation { get; set; }
		public Vector3 Scale { get; set; }

		public TrackTransform()
		{
			Position = Vector3.Zero;
			Rotation = Quaternion.Identity;
			Scale = Vector3.One;
		}
	}
}

