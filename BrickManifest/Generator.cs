using LibLR1;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LR1Tools.BrickManifest
{
	public sealed class GeneratorOptions { public string GamePath { get; set; } public string ExistingManifestPath { get; set; } public string OutputPath { get; set; } }

	public static class Generator
	{
		private static readonly string[] s_paletteHex = { "#1B2A34", "#F4F4F4", "#D6BB80", "#B6B8BA", "#555A5C", "#C62828", "#795548", "#FDD835", "#2E7D32", "#1565C0" };
		private static readonly ManifestTokenCategory[] s_tokenCategories =
		{
			new ManifestTokenCategory { Token = "0x00", Name = "chassis", Description = "Chassis component, not a placeable brick" },
			new ManifestTokenCategory { Token = "0x08", Name = "primitive", Description = "Built-in primitive (Cylinder)" },
			new ManifestTokenCategory { Token = "0x13", Name = "regular", Description = "Standard regular brick" },
			new ManifestTokenCategory { Token = "0x14", Name = "compound", Description = "Compound or decorated piece (often paired)" }
		};

		public static BrickManifestDocument Generate(GeneratorOptions p_options)
		{
			if (p_options == null || string.IsNullOrWhiteSpace(p_options.GamePath) || string.IsNullOrWhiteSpace(p_options.OutputPath)) throw new ArgumentException("--game-path and --output are required.");
			string piecedb = Path.Combine(p_options.GamePath, "MENUDATA", "PIECEDB");
			if (!Directory.Exists(piecedb)) throw new DirectoryNotFoundException("PIECEDB was not found: " + piecedb);
			BrickManifestDocument existing = LoadExisting(p_options.ExistingManifestPath);
			LColors palette = new LColors(Path.Combine(piecedb, "L_COLORS.LEB"));
			CrstMgr registry = new CrstMgr(Path.Combine(piecedb, "CRSTMGR.LEB"));
			LPiece pieces = new LPiece(Path.Combine(piecedb, "LPIECEHI.LEB"));
			Champs champs = new Champs(Path.Combine(piecedb, "CHAMPS.CCB"));
			Dictionary<string, ManifestPiece> curated = existing.Pieces.ToDictionary(piece => piece.PieceName, StringComparer.OrdinalIgnoreCase);
			Dictionary<string, ManifestPiece> manifestPieces = new Dictionary<string, ManifestPiece>(StringComparer.OrdinalIgnoreCase);
			BrickManifestDocument result = new BrickManifestDocument { GeneratedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), Overrides = existing.Overrides ?? new List<ManifestOverride>() };
			for (int i = 0; i < palette.Names.Count; i++) result.ColorPalette.Add(new ManifestColor { Index = i, Name = palette.Names[i], Display = DisplayName(palette.Names[i]), Hex = s_paletteHex[i] });
			result.TokenCategories.AddRange(s_tokenCategories);

			HashSet<string> playerChassis = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int index = 0; index < registry.CSetFilenames.Count; index++)
			{
				byte setTag = checked((byte)(0x0B + index));
				string csetFile = registry.CSetFilenames[index];
				string csetPath = ResolveCsetPath(piecedb, csetFile);
				CSet cset = new CSet(csetPath);
				playerChassis.Add(cset.ChassisTag);
				result.Sets.Add(new ManifestSet { SetTag = Hex(setTag), CsetFile = csetFile, Chassis = cset.ChassisTag, DisplayName = SetDisplayName(setTag), PlayerAccessible = true });
				foreach (KeyValuePair<string, HashSet<string>> pair in cset.ValidColorsByPiece)
				{
					if (!manifestPieces.TryGetValue(pair.Key, out ManifestPiece manifestPiece))
					{
						LPieceEntry? entry = pieces.FindByName(pair.Key);
						curated.TryGetValue(pair.Key, out ManifestPiece prior);
						manifestPiece = new ManifestPiece { PieceName = pair.Key, DisplayName = prior != null ? prior.DisplayName : DisplayName(pair.Key), Category = prior != null ? prior.Category : CategoryFor(entry), Notes = prior != null ? prior.Notes : string.Empty };
						if (entry.HasValue) { manifestPiece.Token = Hex(entry.Value.Token); manifestPiece.BrickId = Hex(entry.Value.BrickId); manifestPiece.VertexCount = entry.Value.VertexCount; manifestPiece.FaceCount = entry.Value.FaceCount; }
						manifestPieces.Add(pair.Key, manifestPiece);
					}
					manifestPiece.Sets.Add(new ManifestPieceSet { SetTag = Hex(setTag), ValidColors = pair.Value.OrderBy(color => palette.IndexOf(color)).ToList() });
				}
			}

			foreach (ChampionEntry champion in champs.Champions.Where(entry => !playerChassis.Contains(entry.ChassisTag)).OrderBy(entry => entry.ChassisTag, StringComparer.OrdinalIgnoreCase))
				result.Sets.Add(new ManifestSet { SetTag = null, CsetFile = null, Chassis = champion.ChassisTag, DisplayName = DisplayName(champion.ChassisTag), PlayerAccessible = false });
			foreach (ManifestPiece piece in manifestPieces.Values) { piece.SingleColorLock = piece.Sets.All(set => set.ValidColors.Count == 1); result.Pieces.Add(piece); }
			result.Pieces = result.Pieces.OrderBy(piece => piece.PieceName, StringComparer.OrdinalIgnoreCase).ToList();
			Write(p_options.OutputPath, result);
			return result;
		}

		private static BrickManifestDocument LoadExisting(string p_path)
		{
			if (string.IsNullOrWhiteSpace(p_path) || !File.Exists(p_path)) return new BrickManifestDocument();
			return CreateDeserializer().Deserialize<BrickManifestDocument>(File.ReadAllText(p_path)) ?? new BrickManifestDocument();
		}
		private static void Write(string p_path, BrickManifestDocument p_document)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(p_path)));
			File.WriteAllText(p_path, CreateSerializer().Serialize(p_document));
		}
		private static string ResolveCsetPath(string p_piecedb, string p_registryName)
		{
			string candidate = Path.Combine(p_piecedb, Path.ChangeExtension(p_registryName, ".LEB"));
			if (File.Exists(candidate)) return candidate;
			return Directory.GetFiles(p_piecedb, Path.GetFileNameWithoutExtension(p_registryName) + ".*", SearchOption.TopDirectoryOnly).FirstOrDefault(path => Path.GetExtension(path).Equals(".LEB", StringComparison.OrdinalIgnoreCase)) ?? throw new FileNotFoundException("CSET referenced by CRSTMGR was not found.", candidate);
		}
		private static ISerializer CreateSerializer() { return new SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).Build(); }
		private static IDeserializer CreateDeserializer() { return new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).IgnoreUnmatchedProperties().Build(); }
		private static string Hex(byte p_value) { return "0x" + p_value.ToString("X2", CultureInfo.InvariantCulture); }
		private static string DisplayName(string p_value) { return string.IsNullOrEmpty(p_value) ? p_value : p_value.Equals("biege", StringComparison.OrdinalIgnoreCase) ? "Beige" : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(p_value.Replace('_', ' ')); }
		private static string CategoryFor(LPieceEntry? p_entry) { if (!p_entry.HasValue) return "themed"; if (p_entry.Value.Token == 0x08) return "primitive"; if (p_entry.Value.Token == 0x14) return "decoration"; return "basic_brick"; }
		private static string SetDisplayName(byte p_setTag) { return SetTagRegistry.TryGet(p_setTag, out SetTagDefinition set) ? set.Name : Hex(p_setTag); }
	}
}
