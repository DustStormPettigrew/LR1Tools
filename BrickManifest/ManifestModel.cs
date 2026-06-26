using System.Collections.Generic;

namespace LR1Tools.BrickManifest
{
	public sealed class BrickManifestDocument
	{
		public int Version { get; set; } = 1;
		public string GeneratedAt { get; set; }
		public List<ManifestColor> ColorPalette { get; set; } = new List<ManifestColor>();
		public List<ManifestTokenCategory> TokenCategories { get; set; } = new List<ManifestTokenCategory>();
		public List<ManifestSet> Sets { get; set; } = new List<ManifestSet>();
		public List<ManifestPiece> Pieces { get; set; } = new List<ManifestPiece>();
		public List<ManifestOverride> Overrides { get; set; } = new List<ManifestOverride>();
	}

	public sealed class ManifestColor { public int Index { get; set; } public string Name { get; set; } public string Display { get; set; } public string Hex { get; set; } }
	public sealed class ManifestTokenCategory { public string Token { get; set; } public string Name { get; set; } public string Description { get; set; } }
	public sealed class ManifestSet { public string SetTag { get; set; } public string CsetFile { get; set; } public string Chassis { get; set; } public string DisplayName { get; set; } public bool PlayerAccessible { get; set; } }
	public sealed class ManifestPiece { public string PieceName { get; set; } public string DisplayName { get; set; } public string Token { get; set; } public string BrickId { get; set; } public uint VertexCount { get; set; } public uint FaceCount { get; set; } public List<ManifestPieceSet> Sets { get; set; } = new List<ManifestPieceSet>(); public bool SingleColorLock { get; set; } public string Category { get; set; } public string Notes { get; set; } }
	public sealed class ManifestPieceSet { public string SetTag { get; set; } public List<string> ValidColors { get; set; } = new List<string>(); }
	public sealed class ManifestOverride { public string PieceName { get; set; } public string Color { get; set; } public string Warning { get; set; } }
}
