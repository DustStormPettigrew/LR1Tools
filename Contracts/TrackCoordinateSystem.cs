namespace LR1Tools.Contracts
{
	public class TrackCoordinateSystem
	{
		public string Handedness { get; set; }
		public string RightAxis { get; set; }
		public string UpAxis { get; set; }
		public string ForwardAxis { get; set; }
		public string Units { get; set; }

		public TrackCoordinateSystem()
		{
			Handedness = "RightHanded";
			RightAxis = "+X";
			UpAxis = "+Z";
			ForwardAxis = "-Y";
			Units = "NativeLR1";
		}
	}
}
