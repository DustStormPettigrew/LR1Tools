using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackMaterial
	{
		public string Name { get; set; }
		public string TextureName { get; set; }
		public string AlphaTextureName { get; set; }
		public TrackColor DiffuseColor { get; set; }
		public float Opacity { get; set; }
		public bool DoubleSided { get; set; }
		public List<TrackGradient> Gradients { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackMaterial()
		{
			Name = string.Empty;
			TextureName = string.Empty;
			AlphaTextureName = string.Empty;
			DiffuseColor = new TrackColor(1f, 1f, 1f, 1f);
			Opacity = 1f;
			Gradients = new List<TrackGradient>();
			Metadata = new Dictionary<string, string>();
		}
	}
}

