using System.Collections.Generic;

namespace LR1Tools.RacerAssets
{
	/// <summary>
	/// Portable inventory of the game assets used by the Racer Editor.
	/// Model entries reference lr1tools.track-scene.v2 files; image entries reference PNG files.
	/// </summary>
	public sealed class RacerAssetManifest
	{
		public string Schema { get; set; } = "lr1tools.racer-assets.v1";
		public string GameInstallPath { get; set; } = string.Empty;
		public List<RacerAssetEntry> Assets { get; set; } = new List<RacerAssetEntry>();
		public List<RacerLogicalBrick> LogicalBricks { get; set; } = new List<RacerLogicalBrick>();
		public List<string> PaletteNames { get; set; } = new List<string>();
		public List<string> Warnings { get; set; } = new List<string>();
	}

	public sealed class RacerAssetEntry
	{
		public string Id { get; set; } = string.Empty;
		public string Category { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string SourcePath { get; set; } = string.Empty;
		public string ModelPath { get; set; } = string.Empty;
		public string ImagePath { get; set; } = string.Empty;
		public List<string> TexturePaths { get; set; } = new List<string>();
	}

	/// <summary>
	/// A CSET roster item. The LEB geometry index data is retained separately until its
	/// per-piece mesh encoding is fully decoded; no guessed LRS id-to-piece mapping is emitted.
	/// </summary>
	public sealed class RacerLogicalBrick
	{
		public string ChassisTag { get; set; } = string.Empty;
		public string PieceName { get; set; } = string.Empty;
		public string ColorName { get; set; } = string.Empty;
	}
}
