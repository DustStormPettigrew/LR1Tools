using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackGradient
	{
		public string Name { get; set; }
		public List<TrackGradientStop> Stops { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackGradient()
		{
			Name = string.Empty;
			Stops = new List<TrackGradientStop>();
			Metadata = new Dictionary<string, string>();
		}
	}
}

