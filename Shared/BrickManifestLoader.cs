using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LR1Tools.Shared
{
	public sealed class BrickManifest { public List<BrickManifestColor> ColorPalette { get; set; } = new List<BrickManifestColor>(); public List<BrickManifestSet> Sets { get; set; } = new List<BrickManifestSet>(); public List<BrickManifestPiece> Pieces { get; set; } = new List<BrickManifestPiece>(); }
	public sealed class BrickManifestColor { public int Index { get; set; } public string Name { get; set; } public string Display { get; set; } public string Hex { get; set; } }
	public sealed class BrickManifestSet { public string SetTag { get; set; } public string Chassis { get; set; } public string DisplayName { get; set; } public bool PlayerAccessible { get; set; } }
	public sealed class BrickManifestPiece { public string PieceName { get; set; } public string Category { get; set; } public bool SingleColorLock { get; set; } public List<BrickManifestPieceSet> Sets { get; set; } = new List<BrickManifestPieceSet>(); }
	public sealed class BrickManifestPieceSet { public string SetTag { get; set; } public List<string> ValidColors { get; set; } = new List<string>(); }
	public static class BrickManifestLoader
	{
		public static bool TryLoad(out BrickManifest p_manifest, out string p_path)
		{
			foreach (string path in CandidatePaths()) if (File.Exists(path)) { p_manifest = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).IgnoreUnmatchedProperties().Build().Deserialize<BrickManifest>(File.ReadAllText(path)); p_path = path; return p_manifest != null; }
			p_manifest = null; p_path = null; return false;
		}
		private static IEnumerable<string> CandidatePaths()
		{
			yield return Path.Combine(AppContext.BaseDirectory, "brick_manifest.yaml");
			yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LR1RacerEditor", "brick_manifest.yaml");
			yield return Path.Combine(Directory.GetCurrentDirectory(), "LR1Tools", "BrickManifest", "brick_manifest.yaml");
			yield return Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LR1Tools", "BrickManifest", "brick_manifest.yaml");
		}
		public static BrickManifestPiece FindPiece(BrickManifest p_manifest, string p_name) { return p_manifest?.Pieces?.FirstOrDefault(piece => string.Equals(piece.PieceName, p_name, StringComparison.OrdinalIgnoreCase)); }
	}
}
