using System.Collections.Generic;

namespace LR1Tools.Contracts
{
	public class TrackTexture
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string SourcePath { get; set; }
		public string ExportPath { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public string Format { get; set; }
		public bool? HasAlpha { get; set; }
		public int? PaletteColorCount { get; set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackTexture()
		{
			Id = string.Empty;
			Name = string.Empty;
			SourcePath = string.Empty;
			ExportPath = string.Empty;
			Width = 0;
			Height = 0;
			Format = string.Empty;
			HasAlpha = null;
			PaletteColorCount = null;
			Metadata = new Dictionary<string, string>();
		}
	}
}
