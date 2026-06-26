using LR1Tools.Adapters;
using LR1Tools.Contracts;
using LR1Tools.Export;
using LibLR1;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LR1Tools.RacerAssets
{
	/// <summary>
	/// Extracts native racer resources into portable 3D scene and PNG assets. The source game
	/// files are never modified.
	/// </summary>
	public static class RacerAssetExtractor
	{
		public const string ManifestFileName = "racer-assets.json";

		public static RacerAssetManifest Extract(string p_gameInstallPath, string p_outputDirectory)
		{
			if (string.IsNullOrWhiteSpace(p_gameInstallPath) || !Directory.Exists(p_gameInstallPath))
				throw new DirectoryNotFoundException("LEGO Racers install directory was not found: " + p_gameInstallPath);
			if (string.IsNullOrWhiteSpace(p_outputDirectory))
				throw new ArgumentException("An output directory is required.", nameof(p_outputDirectory));

			string gameRoot = Path.GetFullPath(p_gameInstallPath);
			string outputRoot = Path.GetFullPath(p_outputDirectory);
			Directory.CreateDirectory(outputRoot);

			RacerAssetManifest manifest = new RacerAssetManifest();
			manifest.GameInstallPath = gameRoot;
			ExtractModels(gameRoot, outputRoot, manifest);
			ExtractCharacterImages(gameRoot, outputRoot, manifest);
			ExtractCsetRostersAndPalettes(gameRoot, manifest);

			string manifestPath = Path.Combine(outputRoot, ManifestFileName);
			File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, CreateJsonOptions()));
			return manifest;
		}

		private static void ExtractModels(string p_gameRoot, string p_outputRoot, RacerAssetManifest p_manifest)
		{
			string modelsRoot = Path.Combine(p_outputRoot, "Models");
			foreach (string path in Directory.GetFiles(p_gameRoot, "*.GDB", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
			{
				try
				{
					string relativeSource = Path.GetRelativePath(p_gameRoot, path);
					string relativeModel = Path.ChangeExtension(Path.Combine("Models", relativeSource), ".json");
					string modelPath = Path.Combine(p_outputRoot, relativeModel);
					TrackScene scene = GDBAdapter.ToScene(new GDB(path), Path.GetFileNameWithoutExtension(path));
					scene.SourcePath = path;
					scene.Metadata["RacerAsset.Category"] = ClassifyModel(path);
					ApplyNativeMaterials(scene, path, p_manifest);
					TrackTextureImageExporter.ExportSceneTextures(scene, modelPath);
					TrackSceneJsonExporter.ExportToFile(scene, modelPath);

					RacerAssetEntry entry = new RacerAssetEntry();
					entry.Id = "model:" + relativeSource.Replace('\\', '/');
					entry.Category = ClassifyModel(path);
					entry.Name = Path.GetFileNameWithoutExtension(path);
					entry.SourcePath = relativeSource;
					entry.ModelPath = relativeModel.Replace('\\', '/');
					for (int i = 0; i < scene.Textures.Count; i++)
					{
						if (!string.IsNullOrWhiteSpace(scene.Textures[i].ExportPath))
							entry.TexturePaths.Add(Path.Combine(Path.GetDirectoryName(relativeModel) ?? string.Empty, scene.Textures[i].ExportPath).Replace('\\', '/'));
					}
					p_manifest.Assets.Add(entry);
				}
				catch (Exception ex)
				{
					p_manifest.Warnings.Add("Model export failed for " + Path.GetRelativePath(p_gameRoot, path) + ": " + ex.Message);
				}
			}
		}

		private static void ApplyNativeMaterials(TrackScene p_scene, string p_gdbPath, RacerAssetManifest p_manifest)
		{
			string directory = Path.GetDirectoryName(p_gdbPath) ?? string.Empty;
			string baseName = Path.GetFileNameWithoutExtension(p_gdbPath);
			string mdbPath = Path.Combine(directory, baseName + ".MDB");
			if (!File.Exists(mdbPath))
				return;

			MDB mdb = new MDB(mdbPath);
			Dictionary<string, MDB_Material> nativeMaterials = mdb.Materials ?? new Dictionary<string, MDB_Material>();
			for (int i = 0; i < p_scene.Materials.Count; i++)
			{
				TrackMaterial material = p_scene.Materials[i];
				if (material == null || !nativeMaterials.TryGetValue(material.Name ?? string.Empty, out MDB_Material native))
					continue;

				if (native.DiffuseColor != null)
					material.DiffuseColor = new TrackColor(native.DiffuseColor.R / 255f, native.DiffuseColor.G / 255f, native.DiffuseColor.B / 255f, native.DiffuseColor.A / 255f);
				if (!string.IsNullOrWhiteSpace(native.TextureName))
					AddTexture(p_scene, material, directory, native.TextureName.Trim());
			}
		}

		private static void AddTexture(TrackScene p_scene, TrackMaterial p_material, string p_directory, string p_textureName)
		{
			string texturePath = ResolveBitmapPath(p_directory, p_textureName);
			if (texturePath == null)
			{
				p_material.Metadata["Texture.Unresolved"] = p_textureName;
				return;
			}

			string id = Path.GetFullPath(texturePath);
			TrackTexture texture = p_scene.Textures.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
			if (texture == null)
			{
				BMP bitmap = new BMP(texturePath);
				texture = new TrackTexture();
				texture.Id = id;
				texture.Name = Path.GetFileNameWithoutExtension(texturePath);
				texture.SourcePath = texturePath;
				texture.Width = bitmap.Width;
				texture.Height = bitmap.Height;
				texture.Format = "LR1.BMP";
				p_scene.Textures.Add(texture);
			}

			p_material.TextureName = p_textureName;
			p_material.TextureRef = new TrackTextureReference
			{
				TextureId = texture.Id,
				Name = texture.Name,
				SourcePath = texture.SourcePath
			};
		}

		private static string ResolveBitmapPath(string p_directory, string p_textureName)
		{
			string value = p_textureName;
			string extension = Path.GetExtension(value);
			string candidate = Path.Combine(p_directory, string.IsNullOrEmpty(extension) ? value + ".BMP" : value);
			if (File.Exists(candidate) && string.Equals(Path.GetExtension(candidate), ".BMP", StringComparison.OrdinalIgnoreCase))
				return candidate;
			return null;
		}

		private static void ExtractCharacterImages(string p_gameRoot, string p_outputRoot, RacerAssetManifest p_manifest)
		{
			string partDb = Path.Combine(p_gameRoot, "MENUDATA", "PARTDB");
			if (!Directory.Exists(partDb))
			{
				p_manifest.Warnings.Add("MENUDATA\\PARTDB was not found; character textures were skipped.");
				return;
			}

			string imagesRoot = Path.Combine(p_outputRoot, "CharacterImages");
			Directory.CreateDirectory(imagesRoot);
			foreach (string path in Directory.GetFiles(partDb, "*.BMP").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
			{
				try
				{
					string name = Path.GetFileNameWithoutExtension(path);
					string relativeImage = Path.Combine("CharacterImages", name + ".png");
					TrackTextureImageExporter.ExportBitmapToPng(new BMP(path), Path.Combine(p_outputRoot, relativeImage));
					p_manifest.Assets.Add(new RacerAssetEntry
					{
						Id = "character:" + name,
						Category = ClassifyCharacterImage(name),
						Name = name,
						SourcePath = Path.GetRelativePath(p_gameRoot, path),
						ImagePath = relativeImage.Replace('\\', '/')
					});
				}
				catch (Exception ex)
				{
					p_manifest.Warnings.Add("Character image export failed for " + Path.GetFileName(path) + ": " + ex.Message);
				}
			}
		}

		private static void ExtractCsetRostersAndPalettes(string p_gameRoot, RacerAssetManifest p_manifest)
		{
			string pieceDb = Path.Combine(p_gameRoot, "MENUDATA", "PIECEDB");
			if (!Directory.Exists(pieceDb))
				return;

			foreach (string path in Directory.GetFiles(pieceDb, "*_CSET.LEB").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
			{
				try
				{
					LEB leb = new LEB(path);
					string[] pieces = leb.Pieces ?? Array.Empty<string>();
					for (int i = 0; i + 1 < pieces.Length; i += 2)
					{
						p_manifest.LogicalBricks.Add(new RacerLogicalBrick
						{
							ChassisTag = leb.ChassisName ?? string.Empty,
							PieceName = pieces[i] ?? string.Empty,
							ColorName = pieces[i + 1] ?? string.Empty
						});
						AddPaletteName(p_manifest.PaletteNames, pieces[i + 1]);
					}
				}
				catch (Exception ex)
				{
					p_manifest.Warnings.Add("CSET export failed for " + Path.GetFileName(path) + ": " + ex.Message);
				}
			}
		}

		private static void AddPaletteName(List<string> p_names, string p_value)
		{
			if (!string.IsNullOrWhiteSpace(p_value) && !p_names.Any(n => string.Equals(n, p_value, StringComparison.OrdinalIgnoreCase)))
				p_names.Add(p_value);
		}

		private static string ClassifyModel(string p_path)
		{
			string name = Path.GetFileNameWithoutExtension(p_path).ToUpperInvariant();
			if (name.Contains("BRK") || name.Contains("BRICK")) return "BrickModel";
			if (name.EndsWith("PELVIS", StringComparison.Ordinal) || name.EndsWith("JMW", StringComparison.Ordinal)) return "CharacterModel";
			if (name.EndsWith("CM", StringComparison.Ordinal)) return "ChassisModel";
			return "Model";
		}

		private static string ClassifyCharacterImage(string p_name)
		{
			string name = p_name.ToUpperInvariant();
			if (name.EndsWith("_ANGRY", StringComparison.Ordinal) || name.EndsWith("_BLINK", StringComparison.Ordinal) || name.EndsWith("_HAPPY", StringComparison.Ordinal) || name.EndsWith("_SAD", StringComparison.Ordinal) || name.EndsWith("_SUPRZ", StringComparison.Ordinal) || name.EndsWith("_DFLT", StringComparison.Ordinal)) return "SnapshotFace";
			if (name.Contains("HAT") || name.Contains("HELMET") || name.Contains("HAIR")) return "Hat";
			if (name.Contains("LEG")) return "Legs";
			if (name.EndsWith("_CHST", StringComparison.Ordinal)) return "Body";
			return "CharacterTexture";
		}

		private static JsonSerializerOptions CreateJsonOptions()
		{
			return new JsonSerializerOptions { WriteIndented = true };
		}
	}
}
