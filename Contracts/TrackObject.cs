using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackObject
	{
		public string Name { get; set; }
		public string MeshName { get; set; }
		public string MaterialName { get; set; }
		public string PathName { get; set; }
		public bool Visible { get; set; }
		public TrackTransform Transform { get; set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackObject()
		{
			Name = string.Empty;
			MeshName = string.Empty;
			MaterialName = string.Empty;
			PathName = string.Empty;
			Visible = true;
			Transform = new TrackTransform();
			Metadata = new Dictionary<string, string>();
		}
	}
}

