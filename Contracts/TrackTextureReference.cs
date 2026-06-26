using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackTextureReference
	{
		public string TextureId { get; set; }
		public string Name { get; set; }
		public string SourcePath { get; set; }
		public string ExportPath { get; set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackTextureReference()
		{
			TextureId = string.Empty;
			Name = string.Empty;
			SourcePath = string.Empty;
			ExportPath = string.Empty;
			Metadata = new Dictionary<string, string>();
		}
	}
}
