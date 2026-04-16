using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackMaterial
	{
		public string Id { get; set; }
		public string SourceId { get; set; }
		public string SourceName { get; set; }
		public string SourceFormat { get; set; }
		public string SourcePath { get; set; }
		public int? SourceIndex { get; set; }
		public string Name { get; set; }
		public string TextureName { get; set; }
		public string AlphaTextureName { get; set; }
		public TrackColor DiffuseColor { get; set; }
		public float Opacity { get; set; }
		public bool DoubleSided { get; set; }
		public List<string> MaterialAnimationIds { get; private set; }
		public List<TrackGradient> Gradients { get; private set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackMaterial()
		{
			Id = string.Empty;
			SourceId = string.Empty;
			SourceName = string.Empty;
			SourceFormat = string.Empty;
			SourcePath = string.Empty;
			SourceIndex = null;
			Name = string.Empty;
			TextureName = string.Empty;
			AlphaTextureName = string.Empty;
			DiffuseColor = new TrackColor(1f, 1f, 1f, 1f);
			Opacity = 1f;
			DoubleSided = false;
			MaterialAnimationIds = new List<string>();
			Gradients = new List<TrackGradient>();
			Metadata = new Dictionary<string, string>();
		}
	}
}

