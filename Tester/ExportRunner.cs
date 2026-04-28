using LR1Tools.Adapters;
using LR1Tools.Contracts;
using LR1Tools.Export;
using LibLR1;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace LR1Tools.Tester
{
	internal static class ExportRunner
	{
		public static void Run(string[] p_args)
		{
			string installationPath;
			string configurationError;
			if (!TesterConfiguration.TryGetInstallationPath(out installationPath, out configurationError))
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(configurationError);
				Console.ResetColor();
				return;
			}

			LocalGameFiles gameFiles = TesterConfiguration.LoadJson<LocalGameFiles>("localGameFiles.json") ?? new LocalGameFiles();
			SceneExportSelection selection = ResolveSelection(installationPath, gameFiles, p_args);
			TrackScene scene = BuildScene(selection);
			string outputPath = ResolveOutputPath(selection.OutputPath, scene.Name);

			LogSelection(selection, outputPath);
			scene.Metadata["Export.OutputPath"] = outputPath;

			TrackSceneJsonExporter.ExportToFile(scene, outputPath);
			Console.WriteLine("Exported track scene JSON:");
			Console.WriteLine(outputPath);
		}

		private static TrackScene BuildScene(SceneExportSelection p_selection)
		{
			TrackScene scene = null;
			WDB wdb = null;

			if (!string.IsNullOrEmpty(p_selection.WdbPath))
			{
				wdb = LoadWdb(p_selection.WdbPath);
				scene = WDBAdapter.ToScene(wdb, Path.GetFileNameWithoutExtension(p_selection.WdbPath));
			}

			if (scene == null)
			{
				scene = new TrackScene();
				scene.Name = !string.IsNullOrEmpty(p_selection.SceneName) ? p_selection.SceneName : "ExportedScene";
			}

			if (!string.IsNullOrEmpty(p_selection.RabPath))
			{
				AddRABMetadata(scene.Metadata, new RAB(p_selection.RabPath));
			}

			AddGdbMeshes(scene, p_selection.GdbPaths);
			AddCollisionAssets(scene, p_selection);

			if (!string.IsNullOrEmpty(p_selection.SkbPath))
			{
				SKB skb = new SKB(p_selection.SkbPath);
				TrackScene skbScene = SKBAdapter.ToScene(skb, Path.GetFileNameWithoutExtension(p_selection.SkbPath));
				ApplySceneSource(skbScene, p_selection.SkbPath, "SKB");
				MergeScene(scene, skbScene, false, true, false, false, false, "SKB");
				MergeGradients(scene.Gradients, skbScene.Gradients);
			}

			ApplyMaterialReferences(scene, p_selection);
			AddMaterialAnimations(scene, p_selection);
			AddStartPositions(scene, p_selection.SpbPath);
			AddCheckpoints(scene, p_selection.CpbPath);
			AddPowerups(scene, p_selection.PwbPaths);
			AddHazards(scene, p_selection.HzbPath);
			AddEmitters(scene, p_selection.EmbPaths);
			AddNpcPaths(scene, p_selection);

			WDBAdapter.ResolveMeshReferences(scene);
			AddMeshResolutionMetadata(scene);
			scene.ExportType = DetermineExportType(p_selection);

			if (!string.IsNullOrEmpty(p_selection.SceneName))
			{
				scene.Name = p_selection.SceneName;
			}

			ApplySelectionProvenance(scene, p_selection);
			AddSelectionMetadata(scene, p_selection);
			return scene;
		}

		private static WDB LoadWdb(string p_path)
		{
			try
			{
				return new WDB(p_path);
			}
			catch (NullReferenceException ex)
			{
				throw new InvalidOperationException("Failed to load WDB scene data from '" + p_path + "'.", ex);
			}
		}

		private static void AddGdbMeshes(TrackScene p_target, IList<string> p_gdbPaths)
		{
			if (p_target == null || p_gdbPaths == null || p_gdbPaths.Count == 0)
			{
				return;
			}

			HashSet<string> materialNames = CreateMaterialNameSet(p_target.Materials);
			HashSet<string> meshNames = CreateMeshNameSet(p_target.Meshes);

			for (int i = 0; i < p_gdbPaths.Count; i++)
			{
				GDB gdb = new GDB(p_gdbPaths[i]);
				TrackScene gdbScene = GDBAdapter.ToScene(gdb, Path.GetFileNameWithoutExtension(p_gdbPaths[i]));
				ApplySceneSource(gdbScene, p_gdbPaths[i], "GDB");
				MergeUniqueMaterials(p_target.Materials, gdbScene.Materials, materialNames);
				MergeUniqueMeshes(p_target.Meshes, gdbScene.Meshes, meshNames);
				CopyMetadata(p_target.Metadata, gdbScene.Metadata, "GDB." + i);
			}
		}

		private static void AddCollisionAssets(TrackScene p_scene, SceneExportSelection p_selection)
		{
			if (p_scene == null || p_selection == null)
			{
				return;
			}

			for (int i = 0; i < p_selection.CollisionWdbPaths.Count; i++)
			{
				string collisionWdbPath = p_selection.CollisionWdbPaths[i];
				TrackScene collisionScene = WDBAdapter.ToScene(LoadWdb(collisionWdbPath), Path.GetFileNameWithoutExtension(collisionWdbPath));
				ApplySceneSource(collisionScene, collisionWdbPath, "WDB");
				MarkObjectsAsCollision(collisionScene.Objects);
				MergeScene(p_scene, collisionScene, false, false, true, false, false, "CollisionWDB." + i.ToString(CultureInfo.InvariantCulture));
			}

			HashSet<string> meshNames = CreateMeshNameSet(p_scene.Meshes);

			for (int i = 0; i < p_selection.CollisionGdbPaths.Count; i++)
			{
				string collisionGdbPath = p_selection.CollisionGdbPaths[i];
				TrackScene gdbScene = GDBAdapter.ToScene(new GDB(collisionGdbPath), Path.GetFileNameWithoutExtension(collisionGdbPath));
				ApplySceneSource(gdbScene, collisionGdbPath, "GDB");
				MarkMeshesAsCollision(gdbScene.Meshes);
				MergeUniqueMeshes(p_scene.Meshes, gdbScene.Meshes, meshNames);
				AddIdentityCollisionObjectsForUnreferencedMeshes(p_scene, gdbScene.Meshes, collisionGdbPath, "GDB");
				CopyMetadata(p_scene.Metadata, gdbScene.Metadata, "CollisionGDB." + i.ToString(CultureInfo.InvariantCulture));
			}

			for (int i = 0; i < p_selection.CollisionBvbPaths.Count; i++)
			{
				string collisionBvbPath = p_selection.CollisionBvbPaths[i];
				TrackScene bvbScene = BVBAdapter.ToScene(new BVB(collisionBvbPath), Path.GetFileNameWithoutExtension(collisionBvbPath));
				ApplySceneSource(bvbScene, collisionBvbPath, "BVB");
				MarkMeshesAsCollision(bvbScene.Meshes);
				MergeUniqueMeshes(p_scene.Meshes, bvbScene.Meshes, meshNames);
				AddIdentityCollisionObjectsForUnreferencedMeshes(p_scene, bvbScene.Meshes, collisionBvbPath, "BVB");
				CopyMetadata(p_scene.Metadata, bvbScene.Metadata, "CollisionBVB." + i.ToString(CultureInfo.InvariantCulture));
			}
		}

		private static void ApplyMaterialReferences(TrackScene p_scene, SceneExportSelection p_selection)
		{
			if (p_scene == null)
			{
				return;
			}

			Dictionary<string, TrackMaterial> materialsByName = new Dictionary<string, TrackMaterial>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < p_scene.Materials.Count; i++)
			{
				TrackMaterial material = p_scene.Materials[i];
				string key = GetNameKey(material != null ? material.Name : null);
				if (!string.IsNullOrEmpty(key))
				{
					materialsByName[key] = material;
				}
			}

			for (int i = 0; i < p_selection.MdbPaths.Count; i++)
			{
				List<TrackMaterial> importedMaterials = MDBAdapter.ToMaterials(new MDB(p_selection.MdbPaths[i]));
				for (int materialIndex = 0; materialIndex < importedMaterials.Count; materialIndex++)
				{
					TrackMaterial importedMaterial = importedMaterials[materialIndex];
					ApplyMaterialSource(importedMaterial, p_selection.MdbPaths[i], "MDB");
					string key = GetNameKey(importedMaterial.Name);
					if (string.IsNullOrEmpty(key))
					{
						continue;
					}

					TrackMaterial existingMaterial;
					if (materialsByName.TryGetValue(key, out existingMaterial))
					{
						MergeMaterialData(existingMaterial, importedMaterial, "MDB." + i);
						continue;
					}

					p_scene.Materials.Add(importedMaterial);
					materialsByName[key] = importedMaterial;
				}
			}

			Dictionary<string, TDB_Texture> texturesByName = LoadTexturesByName(p_selection.TdbPaths);
			foreach (TrackMaterial material in materialsByName.Values)
			{
				ApplyTextureReferenceMetadata(material, texturesByName, p_selection);
			}
		}

		private static void AddMaterialAnimations(TrackScene p_scene, SceneExportSelection p_selection)
		{
			if (p_scene == null || p_selection == null || p_selection.MabPaths == null)
			{
				return;
			}

			Dictionary<string, TrackMaterial> materialsByName = CreateMaterialLookup(p_scene.Materials);
			HashSet<string> animationIds = CreateMaterialAnimationIdSet(p_scene.MaterialAnimations);

			for (int i = 0; i < p_selection.MabPaths.Count; i++)
			{
				string mabPath = p_selection.MabPaths[i];
				List<TrackMaterialAnimation> animations = MABAdapter.ToMaterialAnimations(new MAB(mabPath), Path.GetFileNameWithoutExtension(mabPath));
				ApplyMaterialAnimationsSource(animations, mabPath, "MAB");

				for (int animationIndex = 0; animationIndex < animations.Count; animationIndex++)
				{
					TrackMaterialAnimation animation = animations[animationIndex];
					string animationId = !string.IsNullOrWhiteSpace(animation.Id) ? animation.Id : animation.Name;
					if (string.IsNullOrWhiteSpace(animationId) || !animationIds.Add(animationId))
					{
						continue;
					}

					p_scene.MaterialAnimations.Add(animation);
					AttachMaterialAnimation(p_scene, materialsByName, animation, mabPath);
				}
			}
		}

		private static Dictionary<string, TDB_Texture> LoadTexturesByName(IList<string> p_tdbPaths)
		{
			Dictionary<string, TDB_Texture> textures = new Dictionary<string, TDB_Texture>(StringComparer.OrdinalIgnoreCase);
			if (p_tdbPaths == null)
			{
				return textures;
			}

			for (int i = 0; i < p_tdbPaths.Count; i++)
			{
				TDB tdb = new TDB(p_tdbPaths[i]);
				if (tdb.Textures == null)
				{
					continue;
				}

				foreach (KeyValuePair<string, TDB_Texture> pair in tdb.Textures)
				{
					if (!string.IsNullOrWhiteSpace(pair.Key))
					{
						textures[pair.Key.Trim()] = pair.Value;
					}
				}
			}

			return textures;
		}

		private static void MergeMaterialData(TrackMaterial p_target, TrackMaterial p_source, string p_metadataPrefix)
		{
			if (p_target == null || p_source == null)
			{
				return;
			}

			if (string.IsNullOrEmpty(p_target.TextureName) && !string.IsNullOrEmpty(p_source.TextureName))
			{
				p_target.TextureName = p_source.TextureName;
			}

			if (p_target.Opacity >= 0.9999f && p_source.Opacity < 0.9999f)
			{
				p_target.Opacity = p_source.Opacity;
			}

			if (IsDefaultColor(p_target.DiffuseColor) && !IsDefaultColor(p_source.DiffuseColor))
			{
				p_target.DiffuseColor = p_source.DiffuseColor;
			}

			foreach (KeyValuePair<string, string> pair in p_source.Metadata)
			{
				p_target.Metadata[p_metadataPrefix + "." + pair.Key] = pair.Value;
			}
		}

		private static void ApplyTextureReferenceMetadata(
			TrackMaterial p_material,
			Dictionary<string, TDB_Texture> p_texturesByName,
			SceneExportSelection p_selection)
		{
			if (p_material == null || string.IsNullOrWhiteSpace(p_material.TextureName))
			{
				return;
			}

			string textureName = p_material.TextureName.Trim();
			TDB_Texture texture;
			if (p_texturesByName.TryGetValue(textureName, out texture))
			{
				p_material.Metadata["Texture.SourceFormat"] = "TDB";
				p_material.Metadata["Texture.IsBitmap"] = texture.IsBitmap ? "true" : "false";
				p_material.Metadata["Texture.Bool28"] = texture.Bool28 ? "true" : "false";
				p_material.Metadata["Texture.Bool2B"] = texture.Bool2B ? "true" : "false";
				p_material.Metadata["Texture.Bool2D"] = texture.Bool2D ? "true" : "false";
				if (texture.HasColor2C)
				{
					p_material.Metadata["Texture.Color2C"] = string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", texture.Color2C.R, texture.Color2C.G, texture.Color2C.B);
				}
			}

			string texturePath = ResolveTexturePath(p_selection, textureName);
			if (!string.IsNullOrEmpty(texturePath))
			{
				p_material.Metadata["Texture.Path"] = texturePath;
				p_material.Metadata["Texture.Extension"] = Path.GetExtension(texturePath);
				if (string.Equals(Path.GetExtension(texturePath), ".BMP", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						BMP bitmap = new BMP(texturePath);
						p_material.Metadata["Texture.Width"] = bitmap.Width.ToString(CultureInfo.InvariantCulture);
						p_material.Metadata["Texture.Height"] = bitmap.Height.ToString(CultureInfo.InvariantCulture);
					}
					catch (Exception ex)
					{
						p_material.Metadata["Texture.ReadError"] = ex.GetType().Name;
					}
				}
			}
		}

		private static void AddStartPositions(TrackScene p_scene, string p_spbPath)
		{
			if (p_scene == null || string.IsNullOrEmpty(p_spbPath))
			{
				return;
			}

			List<TrackObject> items = SPBAdapter.ToStartPositions(new SPB(p_spbPath), Path.GetFileNameWithoutExtension(p_spbPath));
			ApplyObjectsSource(items, p_spbPath, "SPB");
			for (int i = 0; i < items.Count; i++)
			{
				p_scene.StartPositions.Add(items[i]);
			}
		}

		private static void AddCheckpoints(TrackScene p_scene, string p_cpbPath)
		{
			if (p_scene == null || string.IsNullOrEmpty(p_cpbPath))
			{
				return;
			}

			List<TrackObject> items = CPBAdapter.ToCheckpoints(new CPB(p_cpbPath), Path.GetFileNameWithoutExtension(p_cpbPath));
			ApplyObjectsSource(items, p_cpbPath, "CPB");
			for (int i = 0; i < items.Count; i++)
			{
				p_scene.Checkpoints.Add(items[i]);
			}
		}

		private static void AddPowerups(TrackScene p_scene, IList<string> p_pwbPaths)
		{
			if (p_scene == null || p_pwbPaths == null)
			{
				return;
			}

			for (int i = 0; i < p_pwbPaths.Count; i++)
			{
				List<TrackObject> items = PWBAdapter.ToPowerups(new PWB(p_pwbPaths[i]), Path.GetFileNameWithoutExtension(p_pwbPaths[i]));
				ApplyObjectsSource(items, p_pwbPaths[i], "PWB");
				for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
				{
					p_scene.Powerups.Add(items[itemIndex]);
				}
			}
		}

		private static void AddHazards(TrackScene p_scene, string p_hzbPath)
		{
			if (p_scene == null || string.IsNullOrEmpty(p_hzbPath))
			{
				return;
			}

			List<TrackObject> items = HZBAdapter.ToHazards(new HZB(p_hzbPath), Path.GetFileNameWithoutExtension(p_hzbPath));
			ApplyObjectsSource(items, p_hzbPath, "HZB");
			for (int i = 0; i < items.Count; i++)
			{
				p_scene.Hazards.Add(items[i]);
			}
		}

		private static void AddEmitters(TrackScene p_scene, IList<string> p_embPaths)
		{
			if (p_scene == null || p_embPaths == null)
			{
				return;
			}

			for (int i = 0; i < p_embPaths.Count; i++)
			{
				List<TrackObject> items = EMBAdapter.ToEmitters(new EMB(p_embPaths[i]), Path.GetFileNameWithoutExtension(p_embPaths[i]));
				ApplyObjectsSource(items, p_embPaths[i], "EMB");
				for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
				{
					p_scene.Emitters.Add(items[itemIndex]);
				}
			}
		}

		private static void AddNpcPaths(TrackScene p_scene, SceneExportSelection p_selection)
		{
			if (p_scene == null || p_selection == null || p_selection.RrbPaths == null)
			{
				return;
			}

			for (int i = 0; i < p_selection.RrbPaths.Count;)
			{
				string rrbPath = p_selection.RrbPaths[i];
				string skipReason;
				TrackScene rrbScene;
				if (!TryLoadRrbScene(rrbPath, out rrbScene, out skipReason))
				{
					AddFailedParseAsset(p_selection, "RRB[" + i.ToString(CultureInfo.InvariantCulture) + "]", Path.GetFileName(rrbPath), skipReason);
					RemoveResolvedAssetByPath(p_selection, rrbPath);
					p_selection.RrbPaths.RemoveAt(i);
					continue;
				}

				ApplySceneSource(rrbScene, rrbPath, "RRB");
				MergeScene(p_scene, rrbScene, true, false, true, true, false, "RRB." + i);
				for (int pathIndex = 0; pathIndex < rrbScene.Paths.Count; pathIndex++)
				{
					p_scene.NpcPaths.Add(rrbScene.Paths[pathIndex]);
				}

				i++;
			}
		}

		private static bool TryLoadRrbScene(string p_rrbPath, out TrackScene p_scene, out string p_skipReason)
		{
			p_scene = null;
			p_skipReason = null;

			if (string.IsNullOrWhiteSpace(p_rrbPath) || !File.Exists(p_rrbPath))
			{
				p_skipReason = "missing file";
				return false;
			}

			if (LooksLikeJsonTextFile(p_rrbPath))
			{
				p_skipReason = "looks like exported JSON, not a native RRB";
				return false;
			}

			try
			{
				RRB rrb = new RRB(p_rrbPath);
				p_scene = RRBAdapter.ToScene(rrb, Path.GetFileNameWithoutExtension(p_rrbPath));
				return true;
			}
			catch (LibLR1.Exceptions.UnexpectedBlockException ex)
			{
				p_skipReason = ex.Message;
				return false;
			}
			catch (InvalidDataException ex)
			{
				p_skipReason = ex.Message;
				return false;
			}
		}

		private static bool LooksLikeJsonTextFile(string p_path)
		{
			using (FileStream stream = File.OpenRead(p_path))
			{
				byte[] buffer = new byte[Math.Min(128, (int)stream.Length)];
				int bytesRead = stream.Read(buffer, 0, buffer.Length);
				if (bytesRead <= 0)
				{
					return false;
				}

				int offset = 0;
				if (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
				{
					offset = 3;
				}

				int printableCount = 0;
				int inspectedCount = 0;
				char firstNonWhitespace = '\0';
				for (int i = offset; i < bytesRead; i++)
				{
					byte value = buffer[i];
					if (value == 0)
					{
						continue;
					}

					char c = (char)value;
					bool isPrintable = value == 9 || value == 10 || value == 13 || (value >= 32 && value <= 126);
					if (isPrintable)
					{
						printableCount++;
					}

					inspectedCount++;
					if (firstNonWhitespace == '\0' && !char.IsWhiteSpace(c))
					{
						firstNonWhitespace = c;
					}
				}

				if (firstNonWhitespace != '{' && firstNonWhitespace != '[')
				{
					return false;
				}

				return inspectedCount > 0 && printableCount * 10 >= inspectedCount * 9;
			}
		}

		private static void MergeScene(
			TrackScene p_target,
			TrackScene p_source,
			bool p_includeMeshes,
			bool p_includeMaterials,
			bool p_includeObjects,
			bool p_includePaths,
			bool p_includeGradients,
			string p_metadataPrefix)
		{
			if (p_source == null)
			{
				return;
			}

			if (p_includeMeshes)
			{
				for (int i = 0; i < p_source.Meshes.Count; i++)
				{
					p_target.Meshes.Add(p_source.Meshes[i]);
				}
			}

			if (p_includeMaterials)
			{
				for (int i = 0; i < p_source.Materials.Count; i++)
				{
					p_target.Materials.Add(p_source.Materials[i]);
				}
			}

			if (p_includeObjects)
			{
				for (int i = 0; i < p_source.Objects.Count; i++)
				{
					p_target.Objects.Add(p_source.Objects[i]);
				}
			}

			if (p_includePaths)
			{
				for (int i = 0; i < p_source.Paths.Count; i++)
				{
					p_target.Paths.Add(p_source.Paths[i]);
				}
			}

			if (p_includeGradients)
			{
				for (int i = 0; i < p_source.Gradients.Count; i++)
				{
					p_target.Gradients.Add(p_source.Gradients[i]);
				}
			}

			CopyMetadata(p_target.Metadata, p_source.Metadata, p_metadataPrefix);
		}

		private static void MergeGradients(IList<TrackGradient> p_target, IList<TrackGradient> p_source)
		{
			if (p_target == null || p_source == null)
			{
				return;
			}

			HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < p_target.Count; i++)
			{
				string key = GetNameKey(p_target[i] != null ? p_target[i].Name : null);
				if (!string.IsNullOrEmpty(key))
				{
					names.Add(key);
				}
			}

			for (int i = 0; i < p_source.Count; i++)
			{
				TrackGradient gradient = p_source[i];
				string key = GetNameKey(gradient != null ? gradient.Name : null);
				if (!string.IsNullOrEmpty(key) && !names.Add(key))
				{
					continue;
				}

				p_target.Add(gradient);
			}
		}

		private static void MergeUniqueMaterials(IList<TrackMaterial> p_target, IList<TrackMaterial> p_source, HashSet<string> p_knownNames)
		{
			if (p_target == null || p_source == null)
			{
				return;
			}

			for (int i = 0; i < p_source.Count; i++)
			{
				TrackMaterial material = p_source[i];
				string key = GetNameKey(material != null ? material.Name : null);
				if (!string.IsNullOrEmpty(key) && !p_knownNames.Add(key))
				{
					continue;
				}

				p_target.Add(material);
			}
		}

		private static void MergeUniqueMeshes(IList<TrackMesh> p_target, IList<TrackMesh> p_source, HashSet<string> p_knownNames)
		{
			if (p_target == null || p_source == null)
			{
				return;
			}

			for (int i = 0; i < p_source.Count; i++)
			{
				TrackMesh mesh = p_source[i];
				string key = GetNameKey(mesh != null ? mesh.Name : null);
				if (!string.IsNullOrEmpty(key) && !p_knownNames.Add(key))
				{
					continue;
				}

				p_target.Add(mesh);
			}
		}

		private static void MarkMeshesAsCollision(IList<TrackMesh> p_meshes)
		{
			if (p_meshes == null)
			{
				return;
			}

			for (int i = 0; i < p_meshes.Count; i++)
			{
				TrackMesh mesh = p_meshes[i];
				if (mesh == null)
				{
					continue;
				}

				mesh.IsCollisionMesh = true;
				mesh.Metadata["IsCollisionMesh"] = "true";
			}
		}

		private static void MarkObjectsAsCollision(IList<TrackObject> p_objects)
		{
			if (p_objects == null)
			{
				return;
			}

			for (int i = 0; i < p_objects.Count; i++)
			{
				TrackObject obj = p_objects[i];
				if (obj == null)
				{
					continue;
				}

				obj.Metadata["IsCollisionObject"] = "true";
			}
		}

		private static void AddIdentityCollisionObjectsForUnreferencedMeshes(
			TrackScene p_scene,
			IList<TrackMesh> p_meshes,
			string p_sourcePath,
			string p_sourceFormat)
		{
			if (p_scene == null || p_meshes == null)
			{
				return;
			}

			for (int i = 0; i < p_meshes.Count; i++)
			{
				TrackMesh mesh = p_meshes[i];
				if (mesh == null || string.IsNullOrWhiteSpace(mesh.Name) || IsMeshReferencedByObject(p_scene.Objects, mesh.Name))
				{
					continue;
				}

				TrackObject obj = new TrackObject();
				obj.Name = mesh.Name + ".CollisionObject";
				obj.MeshName = mesh.Name;
				obj.MaterialName = mesh.MaterialName ?? string.Empty;
				obj.Metadata["IsCollisionObject"] = "true";
				obj.Metadata["NativeType"] = "CollisionMesh";
				ApplyObjectSource(obj, p_sourcePath, p_sourceFormat);
				p_scene.Objects.Add(obj);
			}
		}

		private static bool IsMeshReferencedByObject(IList<TrackObject> p_objects, string p_meshName)
		{
			if (p_objects == null || string.IsNullOrWhiteSpace(p_meshName))
			{
				return false;
			}

			string normalizedMeshName = NormalizeMeshReference(p_meshName);
			for (int i = 0; i < p_objects.Count; i++)
			{
				TrackObject obj = p_objects[i];
				if (obj == null || string.IsNullOrWhiteSpace(obj.MeshName))
				{
					continue;
				}

				if (string.Equals(obj.MeshName, p_meshName, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(NormalizeMeshReference(obj.MeshName), normalizedMeshName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private static HashSet<string> CreateMaterialNameSet(IList<TrackMaterial> p_items)
		{
			HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (p_items == null)
			{
				return names;
			}

			for (int i = 0; i < p_items.Count; i++)
			{
				string key = GetNameKey(p_items[i] != null ? p_items[i].Name : null);
				if (!string.IsNullOrEmpty(key))
				{
					names.Add(key);
				}
			}

			return names;
		}

		private static HashSet<string> CreateMeshNameSet(IList<TrackMesh> p_items)
		{
			HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (p_items == null)
			{
				return names;
			}

			for (int i = 0; i < p_items.Count; i++)
			{
				string key = GetNameKey(p_items[i] != null ? p_items[i].Name : null);
				if (!string.IsNullOrEmpty(key))
				{
					names.Add(key);
				}
			}

			return names;
		}

		private static Dictionary<string, TrackMaterial> CreateMaterialLookup(IList<TrackMaterial> p_items)
		{
			Dictionary<string, TrackMaterial> lookup = new Dictionary<string, TrackMaterial>(StringComparer.OrdinalIgnoreCase);
			if (p_items == null)
			{
				return lookup;
			}

			for (int i = 0; i < p_items.Count; i++)
			{
				TrackMaterial material = p_items[i];
				string key = GetNameKey(material != null ? material.Name : null);
				if (!string.IsNullOrEmpty(key))
				{
					lookup[key] = material;
				}
			}

			return lookup;
		}

		private static HashSet<string> CreateMaterialAnimationIdSet(IList<TrackMaterialAnimation> p_items)
		{
			HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (p_items == null)
			{
				return ids;
			}

			for (int i = 0; i < p_items.Count; i++)
			{
				TrackMaterialAnimation animation = p_items[i];
				string key = GetNameKey(animation != null ? (!string.IsNullOrWhiteSpace(animation.Id) ? animation.Id : animation.Name) : null);
				if (!string.IsNullOrEmpty(key))
				{
					ids.Add(key);
				}
			}

			return ids;
		}

		private static string GetNameKey(string p_name)
		{
			return string.IsNullOrWhiteSpace(p_name) ? null : p_name.Trim();
		}

		private static string NormalizeMeshReference(string p_name)
		{
			if (string.IsNullOrWhiteSpace(p_name))
			{
				return string.Empty;
			}

			string fileName = Path.GetFileName(p_name.Trim());
			string withoutExtension = Path.GetFileNameWithoutExtension(fileName);
			return string.IsNullOrEmpty(withoutExtension) ? fileName.ToUpperInvariant() : withoutExtension.ToUpperInvariant();
		}

		private static bool IsDefaultColor(TrackColor p_color)
		{
			return Math.Abs(p_color.R - 1f) < 0.0001f &&
				Math.Abs(p_color.G - 1f) < 0.0001f &&
				Math.Abs(p_color.B - 1f) < 0.0001f &&
				Math.Abs(p_color.A - 1f) < 0.0001f;
		}

		private static void CopyMetadata(IDictionary<string, string> p_target, IDictionary<string, string> p_source, string p_metadataPrefix)
		{
			if (p_target == null || p_source == null)
			{
				return;
			}

			foreach (KeyValuePair<string, string> pair in p_source)
			{
				p_target[p_metadataPrefix + "." + pair.Key] = pair.Value;
			}
		}

		private static void AddMeshResolutionMetadata(TrackScene p_scene)
		{
			if (p_scene == null)
			{
				return;
			}

			HashSet<string> meshNames = CreateMeshNameSet(p_scene.Meshes);
			int referencedObjectCount = 0;
			int resolvedObjectCount = 0;
			int unresolvedObjectCount = 0;

			for (int i = 0; i < p_scene.Objects.Count; i++)
			{
				TrackObject obj = p_scene.Objects[i];
				string meshName = obj != null ? GetNameKey(obj.MeshName) : null;
				if (string.IsNullOrEmpty(meshName))
				{
					continue;
				}

				referencedObjectCount++;
				if (meshNames.Contains(meshName))
				{
					resolvedObjectCount++;
				}
				else
				{
					unresolvedObjectCount++;
				}
			}

			p_scene.Metadata["Export.MeshReference.ObjectCount"] = referencedObjectCount.ToString(CultureInfo.InvariantCulture);
			p_scene.Metadata["Export.MeshReference.ResolvedCount"] = resolvedObjectCount.ToString(CultureInfo.InvariantCulture);
			p_scene.Metadata["Export.MeshReference.UnresolvedCount"] = unresolvedObjectCount.ToString(CultureInfo.InvariantCulture);
		}

		private static void AddRABMetadata(IDictionary<string, string> p_metadata, RAB p_rab)
		{
			if (p_metadata == null || p_rab == null || p_rab.Track == null)
			{
				return;
			}

			RAB_Track track = p_rab.Track;
			p_metadata["RAB.TrackTitle"] = p_rab.TrackTitle ?? string.Empty;
			p_metadata["RAB.TrackScene"] = track.MaybeTrackScene ?? string.Empty;
			p_metadata["RAB.StartPosFile"] = track.StartPosFile ?? string.Empty;
			p_metadata["RAB.SkyBoxFile"] = track.SkyBoxFile ?? string.Empty;
			p_metadata["RAB.HazardFile"] = track.HazardFile ?? string.Empty;
			p_metadata["RAB.EventScriptFile"] = track.EventScriptFile ?? string.Empty;
			p_metadata["RAB.MusicListFile"] = track.MusicListFile ?? string.Empty;
			AddMetadataArray(p_metadata, "RAB.PowerupFiles", track.PowerupFiles);
			AddMetadataArray(p_metadata, "RAB.EmitterFiles", track.EmitterFiles);
			AddMetadataArray(p_metadata, "RAB.CheckpointFiles", track.CheckpointFiles);
			AddMetadataArray(p_metadata, "RAB.CollisionFiles", track.MaybeCollisionMeshes);
		}

		private static string DetermineExportType(SceneExportSelection p_selection)
		{
			bool hasTrackLevelSceneData =
				!string.IsNullOrEmpty(p_selection.RabPath) ||
				!string.IsNullOrEmpty(p_selection.SpbPath) ||
				!string.IsNullOrEmpty(p_selection.CpbPath) ||
				!string.IsNullOrEmpty(p_selection.HzbPath) ||
				p_selection.PwbPaths.Count > 0 ||
				p_selection.EmbPaths.Count > 0;

			if (hasTrackLevelSceneData)
			{
				return TrackSceneExportTypes.Scene;
			}

			if (!string.IsNullOrEmpty(p_selection.WdbPath))
			{
				return TrackSceneExportTypes.ObjectSet;
			}

			bool hasMeshes = p_selection.GdbPaths.Count > 0 || p_selection.CollisionGdbPaths.Count > 0 || p_selection.CollisionBvbPaths.Count > 0;
			bool hasPaths = p_selection.RrbPaths.Count > 0;
			bool hasMaterials = !string.IsNullOrEmpty(p_selection.SkbPath) || p_selection.MabPaths.Count > 0 || p_selection.MdbPaths.Count > 0 || p_selection.TdbPaths.Count > 0;

			if (hasMeshes && !hasPaths && !hasMaterials)
			{
				return TrackSceneExportTypes.Mesh;
			}

			if (hasPaths && !hasMeshes && !hasMaterials)
			{
				return TrackSceneExportTypes.PathSet;
			}

			if (hasMaterials && !hasMeshes && !hasPaths)
			{
				return TrackSceneExportTypes.MaterialSet;
			}

			if (hasMeshes || hasPaths || hasMaterials)
			{
				return TrackSceneExportTypes.AssetBundle;
			}

			return TrackSceneExportTypes.Scene;
		}

		private static void ApplySelectionProvenance(TrackScene p_scene, SceneExportSelection p_selection)
		{
			if (p_scene == null)
			{
				return;
			}

			string sourcePath = !string.IsNullOrEmpty(p_selection.PrimaryInputPath) ? p_selection.PrimaryInputPath : GetPrimarySelectedPath(p_selection);
			string sourceName = !string.IsNullOrEmpty(sourcePath) ? Path.GetFileNameWithoutExtension(sourcePath) : p_scene.Name;
			string sourceFormat = DeterminePrimarySourceFormat(p_selection, sourcePath);

			if (string.IsNullOrEmpty(p_scene.Id))
			{
				p_scene.Id = p_scene.Name ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_scene.SourceId))
			{
				p_scene.SourceId = !string.IsNullOrEmpty(sourceName) ? sourceName : p_scene.Id;
			}

			if (string.IsNullOrEmpty(p_scene.SourceName))
			{
				p_scene.SourceName = sourceName ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_scene.SourceFormat))
			{
				p_scene.SourceFormat = sourceFormat;
			}

			if (string.IsNullOrEmpty(p_scene.SourcePath))
			{
				p_scene.SourcePath = sourcePath ?? string.Empty;
			}
		}

		private static string DeterminePrimarySourceFormat(SceneExportSelection p_selection, string p_sourcePath)
		{
			if (!string.IsNullOrEmpty(p_selection.RabPath)) return "RAB";
			if (!string.IsNullOrEmpty(p_selection.WdbPath)) return "WDB";
			if (p_selection.GdbPaths.Count > 0) return "GDB";
			if (p_selection.CollisionBvbPaths.Count > 0) return "BVB";
			if (p_selection.CollisionGdbPaths.Count > 0) return "GDB";
			if (!string.IsNullOrEmpty(p_selection.SkbPath)) return "SKB";
			if (p_selection.MabPaths.Count > 0) return "MAB";
			if (!string.IsNullOrEmpty(p_selection.SpbPath)) return "SPB";
			if (!string.IsNullOrEmpty(p_selection.CpbPath)) return "CPB";
			if (p_selection.PwbPaths.Count > 0) return "PWB";
			if (!string.IsNullOrEmpty(p_selection.HzbPath)) return "HZB";
			if (p_selection.EmbPaths.Count > 0) return "EMB";
			if (p_selection.RrbPaths.Count > 0) return "RRB";
			if (!string.IsNullOrEmpty(p_sourcePath))
			{
				return Path.GetExtension(p_sourcePath).TrimStart('.').ToUpperInvariant();
			}

			return string.Empty;
		}

		private static void ApplySceneSource(TrackScene p_scene, string p_sourcePath, string p_sourceFormat)
		{
			if (p_scene == null)
			{
				return;
			}

			string sourceName = !string.IsNullOrEmpty(p_sourcePath) ? Path.GetFileNameWithoutExtension(p_sourcePath) : p_scene.Name;
			if (string.IsNullOrEmpty(p_scene.Id))
			{
				p_scene.Id = p_scene.Name ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_scene.SourceId))
			{
				p_scene.SourceId = sourceName ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_scene.SourceName))
			{
				p_scene.SourceName = sourceName ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_scene.SourceFormat))
			{
				p_scene.SourceFormat = p_sourceFormat ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_scene.SourcePath))
			{
				p_scene.SourcePath = p_sourcePath ?? string.Empty;
			}

			ApplyMaterialsSource(p_scene.Materials, p_sourcePath, p_sourceFormat);
			ApplyMeshesSource(p_scene.Meshes, p_sourcePath, p_sourceFormat);
			ApplyObjectsSource(p_scene.Objects, p_sourcePath, p_sourceFormat);
			ApplyPathsSource(p_scene.Paths, p_sourcePath, p_sourceFormat);
			ApplyPathsSource(p_scene.NpcPaths, p_sourcePath, p_sourceFormat);
			ApplyMaterialAnimationsSource(p_scene.MaterialAnimations, p_sourcePath, p_sourceFormat);
			ApplyGradientsSource(p_scene.Gradients, p_sourcePath, p_sourceFormat);
		}

		private static void ApplyMaterialsSource(IList<TrackMaterial> p_materials, string p_sourcePath, string p_sourceFormat)
		{
			if (p_materials == null)
			{
				return;
			}

			for (int i = 0; i < p_materials.Count; i++)
			{
				ApplyMaterialSource(p_materials[i], p_sourcePath, p_sourceFormat);
			}
		}

		private static void ApplyMaterialSource(TrackMaterial p_material, string p_sourcePath, string p_sourceFormat)
		{
			if (p_material == null)
			{
				return;
			}

			if (string.IsNullOrEmpty(p_material.Id))
			{
				p_material.Id = p_material.Name ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_material.SourceName))
			{
				p_material.SourceName = p_material.Name ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_material.SourceFormat))
			{
				p_material.SourceFormat = p_sourceFormat ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_material.SourcePath))
			{
				p_material.SourcePath = p_sourcePath ?? string.Empty;
			}
		}

		private static void ApplyMaterialAnimationsSource(IList<TrackMaterialAnimation> p_animations, string p_sourcePath, string p_sourceFormat)
		{
			if (p_animations == null)
			{
				return;
			}

			for (int i = 0; i < p_animations.Count; i++)
			{
				TrackMaterialAnimation animation = p_animations[i];
				if (animation == null)
				{
					continue;
				}

				if (string.IsNullOrEmpty(animation.Id))
				{
					animation.Id = animation.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(animation.SourceName))
				{
					animation.SourceName = animation.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(animation.SourceFormat))
				{
					animation.SourceFormat = p_sourceFormat ?? string.Empty;
				}

				if (string.IsNullOrEmpty(animation.SourcePath))
				{
					animation.SourcePath = p_sourcePath ?? string.Empty;
				}
			}
		}

		private static void AttachMaterialAnimation(
			TrackScene p_scene,
			Dictionary<string, TrackMaterial> p_materialsByName,
			TrackMaterialAnimation p_animation,
			string p_sourcePath)
		{
			if (p_scene == null || p_materialsByName == null || p_animation == null)
			{
				return;
			}

			HashSet<string> referencedMaterialNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < p_animation.Frames.Count; i++)
			{
				string materialName = GetNameKey(p_animation.Frames[i] != null ? p_animation.Frames[i].MaterialName : null);
				if (!string.IsNullOrEmpty(materialName))
				{
					referencedMaterialNames.Add(materialName);
				}
			}

			string primaryMaterialName = GetNameKey(p_animation.MaterialName);
			if (!string.IsNullOrEmpty(primaryMaterialName))
			{
				referencedMaterialNames.Add(primaryMaterialName);
			}

			foreach (string materialName in referencedMaterialNames)
			{
				TrackMaterial material = FindOrCreateMaterial(p_scene, p_materialsByName, materialName, p_sourcePath);
				AddUniqueName(material.MaterialAnimationIds, p_animation.Id);
			}
		}

		private static TrackMaterial FindOrCreateMaterial(
			TrackScene p_scene,
			Dictionary<string, TrackMaterial> p_materialsByName,
			string p_materialName,
			string p_sourcePath)
		{
			string key = GetNameKey(p_materialName);
			TrackMaterial material;
			if (!string.IsNullOrEmpty(key) && p_materialsByName.TryGetValue(key, out material))
			{
				return material;
			}

			material = new TrackMaterial();
			material.Name = key ?? string.Empty;
			material.SourceId = material.Name;
			material.Metadata["MaterialAnimation.Placeholder"] = "true";
			ApplyMaterialSource(material, p_sourcePath, "MAB");
			p_scene.Materials.Add(material);
			if (!string.IsNullOrEmpty(key))
			{
				p_materialsByName[key] = material;
			}

			return material;
		}

		private static void ApplyMeshesSource(IList<TrackMesh> p_meshes, string p_sourcePath, string p_sourceFormat)
		{
			if (p_meshes == null)
			{
				return;
			}

			for (int i = 0; i < p_meshes.Count; i++)
			{
				TrackMesh mesh = p_meshes[i];
				if (mesh == null)
				{
					continue;
				}

				if (string.IsNullOrEmpty(mesh.Id))
				{
					mesh.Id = mesh.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(mesh.SourceName))
				{
					mesh.SourceName = mesh.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(mesh.SourceFormat))
				{
					mesh.SourceFormat = p_sourceFormat ?? string.Empty;
				}

				if (string.IsNullOrEmpty(mesh.SourcePath))
				{
					mesh.SourcePath = p_sourcePath ?? string.Empty;
				}
			}
		}

		private static void ApplyObjectSource(TrackObject p_object, string p_sourcePath, string p_sourceFormat)
		{
			if (p_object == null)
			{
				return;
			}

			if (string.IsNullOrEmpty(p_object.Id))
			{
				p_object.Id = p_object.Name ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_object.SourceName))
			{
				p_object.SourceName = p_object.Name ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_object.SourceFormat))
			{
				p_object.SourceFormat = p_sourceFormat ?? string.Empty;
			}

			if (string.IsNullOrEmpty(p_object.SourcePath))
			{
				p_object.SourcePath = p_sourcePath ?? string.Empty;
			}
		}

		private static void ApplyObjectsSource(IList<TrackObject> p_objects, string p_sourcePath, string p_sourceFormat)
		{
			if (p_objects == null)
			{
				return;
			}

			for (int i = 0; i < p_objects.Count; i++)
			{
				TrackObject obj = p_objects[i];
				if (obj == null)
				{
					continue;
				}

				if (string.IsNullOrEmpty(obj.Id))
				{
					obj.Id = obj.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(obj.SourceName))
				{
					obj.SourceName = obj.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(obj.SourceFormat))
				{
					obj.SourceFormat = p_sourceFormat ?? string.Empty;
				}

				if (string.IsNullOrEmpty(obj.SourcePath))
				{
					obj.SourcePath = p_sourcePath ?? string.Empty;
				}
			}
		}

		private static void ApplyPathsSource(IList<TrackPath> p_paths, string p_sourcePath, string p_sourceFormat)
		{
			if (p_paths == null)
			{
				return;
			}

			for (int i = 0; i < p_paths.Count; i++)
			{
				TrackPath path = p_paths[i];
				if (path == null)
				{
					continue;
				}

				if (string.IsNullOrEmpty(path.Id))
				{
					path.Id = path.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(path.SourceName))
				{
					path.SourceName = path.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(path.SourceFormat))
				{
					path.SourceFormat = p_sourceFormat ?? string.Empty;
				}

				if (string.IsNullOrEmpty(path.SourcePath))
				{
					path.SourcePath = p_sourcePath ?? string.Empty;
				}
			}
		}

		private static void ApplyGradientsSource(IList<TrackGradient> p_gradients, string p_sourcePath, string p_sourceFormat)
		{
			if (p_gradients == null)
			{
				return;
			}

			for (int i = 0; i < p_gradients.Count; i++)
			{
				TrackGradient gradient = p_gradients[i];
				if (gradient == null)
				{
					continue;
				}

				if (string.IsNullOrEmpty(gradient.Id))
				{
					gradient.Id = gradient.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(gradient.SourceName))
				{
					gradient.SourceName = gradient.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(gradient.SourceFormat))
				{
					gradient.SourceFormat = p_sourceFormat ?? string.Empty;
				}

				if (string.IsNullOrEmpty(gradient.SourcePath))
				{
					gradient.SourcePath = p_sourcePath ?? string.Empty;
				}
			}
		}

		private static SceneExportSelection ResolveSelection(string p_installationPath, LocalGameFiles p_gameFiles, string[] p_args)
		{
			SceneExportSelection selection = new SceneExportSelection();
			selection.InstallationPath = p_installationPath;
			selection.AutoResolveGdbReferences = ResolveAutoResolveGdbReferences(p_gameFiles);
			selection.AutoResolveReferencedAssets = ResolveAutoResolveReferencedAssets(p_gameFiles);

			List<string> inputArgs = GetInputArguments(p_args);
			if (inputArgs.Count > 0)
			{
				ApplyExplicitInputs(selection, inputArgs);
				selection.OutputPath = GetCommandLineOutputPath(p_args);
			}
			else
			{
				ApplyConfiguredInputs(selection, p_gameFiles);
				selection.OutputPath = GetConfiguredOutputPath(p_gameFiles.OutputPath);
			}

			ApplyRABReferences(selection);
			ResolveWdbReferencedAssets(selection);
			ResolveCollisionWdbReferencedAssets(selection);
			ResolveNpcPathReferences(selection);
			selection.SceneName = GetSceneName(selection);

			if (string.IsNullOrEmpty(selection.WdbPath) &&
				string.IsNullOrEmpty(selection.RabPath) &&
				string.IsNullOrEmpty(selection.SkbPath) &&
				string.IsNullOrEmpty(selection.SpbPath) &&
				string.IsNullOrEmpty(selection.CpbPath) &&
				string.IsNullOrEmpty(selection.HzbPath) &&
				selection.MabPaths.Count == 0 &&
				selection.GdbPaths.Count == 0 &&
				selection.CollisionWdbPaths.Count == 0 &&
				selection.CollisionGdbPaths.Count == 0 &&
				selection.CollisionBvbPaths.Count == 0 &&
				selection.MdbPaths.Count == 0 &&
				selection.TdbPaths.Count == 0 &&
				selection.PwbPaths.Count == 0 &&
				selection.EmbPaths.Count == 0 &&
				selection.RrbPaths.Count == 0)
			{
				throw new FileNotFoundException("No representative scene inputs could be resolved for export.");
			}

			return selection;
		}

		private static List<string> GetInputArguments(string[] p_args)
		{
			List<string> inputs = new List<string>();
			if (p_args == null || p_args.Length <= 1)
			{
				return inputs;
			}

			int lastArgumentIndex = p_args.Length - 1;
			bool hasOutputArgument = IsJsonPath(p_args[lastArgumentIndex]);

			for (int i = 1; i < p_args.Length; i++)
			{
				if (hasOutputArgument && i == lastArgumentIndex)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(p_args[i]))
				{
					inputs.Add(p_args[i]);
				}
			}

			return inputs;
		}

		private static void ApplyExplicitInputs(SceneExportSelection p_selection, List<string> p_inputArgs)
		{
			for (int i = 0; i < p_inputArgs.Count; i++)
			{
				string resolved = ResolveExplicitPath(p_selection.InstallationPath, p_inputArgs[i]);
				if (string.IsNullOrEmpty(resolved))
				{
					throw new FileNotFoundException("Explicit export input could not be resolved: " + p_inputArgs[i]);
				}

				if (string.IsNullOrEmpty(p_selection.PrimaryInputPath))
				{
					p_selection.PrimaryInputPath = resolved;
				}

				string extension = Path.GetExtension(resolved);
				if (string.Equals(extension, ".RAB", StringComparison.OrdinalIgnoreCase))
				{
					p_selection.RabPath = AssignSingleInput("RAB", resolved, p_selection.RabPath);
				}
				else if (string.Equals(extension, ".GDB", StringComparison.OrdinalIgnoreCase))
				{
					AddUniquePath(p_selection.GdbPaths, resolved);
					AddUniquePath(p_selection.ExplicitGdbPaths, resolved);
				}
				else if (string.Equals(extension, ".WDB", StringComparison.OrdinalIgnoreCase))
				{
					p_selection.WdbPath = AssignSingleInput("WDB", resolved, p_selection.WdbPath);
				}
				else if (string.Equals(extension, ".BVB", StringComparison.OrdinalIgnoreCase))
				{
					AddUniquePath(p_selection.CollisionBvbPaths, resolved);
				}
				else if (string.Equals(extension, ".SKB", StringComparison.OrdinalIgnoreCase))
				{
					p_selection.SkbPath = AssignSingleInput("SKB", resolved, p_selection.SkbPath);
				}
				else if (string.Equals(extension, ".MAB", StringComparison.OrdinalIgnoreCase))
				{
					AddUniquePath(p_selection.MabPaths, resolved);
				}
				else if (string.Equals(extension, ".SPB", StringComparison.OrdinalIgnoreCase))
				{
					p_selection.SpbPath = AssignSingleInput("SPB", resolved, p_selection.SpbPath);
				}
				else if (string.Equals(extension, ".CPB", StringComparison.OrdinalIgnoreCase))
				{
					p_selection.CpbPath = AssignSingleInput("CPB", resolved, p_selection.CpbPath);
				}
				else if (string.Equals(extension, ".PWB", StringComparison.OrdinalIgnoreCase))
				{
					AddUniquePath(p_selection.PwbPaths, resolved);
				}
				else if (string.Equals(extension, ".HZB", StringComparison.OrdinalIgnoreCase))
				{
					p_selection.HzbPath = AssignSingleInput("HZB", resolved, p_selection.HzbPath);
				}
				else if (string.Equals(extension, ".EMB", StringComparison.OrdinalIgnoreCase))
				{
					AddUniquePath(p_selection.EmbPaths, resolved);
				}
				else if (string.Equals(extension, ".RRB", StringComparison.OrdinalIgnoreCase))
				{
					AddUniquePath(p_selection.RrbPaths, resolved);
				}
				else if (string.Equals(extension, ".MDB", StringComparison.OrdinalIgnoreCase))
				{
					AddUniquePath(p_selection.MdbPaths, resolved);
				}
				else if (string.Equals(extension, ".TDB", StringComparison.OrdinalIgnoreCase))
				{
					AddUniquePath(p_selection.TdbPaths, resolved);
				}
				else
				{
					throw new InvalidOperationException("Unsupported export input type: " + resolved);
				}
			}
		}

		private static void ApplyConfiguredInputs(SceneExportSelection p_selection, LocalGameFiles p_gameFiles)
		{
			p_selection.RabPath = ResolvePath(p_selection.InstallationPath, p_gameFiles.RabPath);
			p_selection.WdbPath = ResolveFilePath(p_selection.InstallationPath, p_gameFiles.WdbPath, "*.WDB");
			p_selection.SkbPath = ResolvePath(p_selection.InstallationPath, p_gameFiles.SkbPath);
			p_selection.SpbPath = ResolvePath(p_selection.InstallationPath, p_gameFiles.SpbPath);
			p_selection.CpbPath = ResolvePath(p_selection.InstallationPath, p_gameFiles.CpbPath);
			p_selection.HzbPath = ResolvePath(p_selection.InstallationPath, p_gameFiles.HzbPath);

			AddConfiguredPaths(p_selection.InstallationPath, p_gameFiles.GdbPaths, p_selection.GdbPaths, p_selection.ExplicitGdbPaths);
			AddConfiguredPaths(p_selection.InstallationPath, p_gameFiles.MabPaths, p_selection.MabPaths, null);
			AddConfiguredPaths(p_selection.InstallationPath, p_gameFiles.PwbPaths, p_selection.PwbPaths, null);
			AddConfiguredPaths(p_selection.InstallationPath, p_gameFiles.EmbPaths, p_selection.EmbPaths, null);
			AddConfiguredPaths(p_selection.InstallationPath, p_gameFiles.RrbPaths, p_selection.RrbPaths, null);
			AddConfiguredPaths(p_selection.InstallationPath, p_gameFiles.MdbPaths, p_selection.MdbPaths, null);
			AddConfiguredPaths(p_selection.InstallationPath, p_gameFiles.TdbPaths, p_selection.TdbPaths, null);

			p_selection.PrimaryInputPath = GetPrimarySelectedPath(p_selection);
		}

		private static void AddConfiguredPaths(string p_installationPath, List<string> p_configuredPaths, List<string> p_target, List<string> p_trackingTarget)
		{
			if (p_configuredPaths == null)
			{
				return;
			}

			for (int i = 0; i < p_configuredPaths.Count; i++)
			{
				string resolved = ResolvePath(p_installationPath, p_configuredPaths[i]);
				if (string.IsNullOrEmpty(resolved))
				{
					continue;
				}

				AddUniquePath(p_target, resolved);
				if (p_trackingTarget != null)
				{
					AddUniquePath(p_trackingTarget, resolved);
				}
			}
		}

		private static void ApplyRABReferences(SceneExportSelection p_selection)
		{
			if (p_selection == null || string.IsNullOrEmpty(p_selection.RabPath))
			{
				return;
			}

			RAB rab = new RAB(p_selection.RabPath);
			RAB_Track track = rab.Track;
			if (track == null)
			{
				AddSkippedAsset(p_selection, "RAB.Track", "<null track>");
				return;
			}

			string checkpointReference = GetArrayValue(track.CheckpointFiles, 0);
			string checkpointCollisionReference = GetArrayValue(track.CheckpointFiles, 1);
			string powerupLayoutReference = GetArrayValue(track.PowerupFiles, 0);
			string powerupMaterialAnimation = GetArrayValue(track.PowerupFiles, 1);
			string auxiliaryPowerupScene = GetArrayValue(track.PowerupFiles, 2);

			ResolveReferencedSingleAsset(p_selection, "RAB.TrackScene", track.MaybeTrackScene, new[] { ".WDB" }, p_selection.WdbPath, value => p_selection.WdbPath = value);
			ResolveReferencedSingleAsset(p_selection, "RAB.SkyBox", track.SkyBoxFile, new[] { ".SKB" }, p_selection.SkbPath, value => p_selection.SkbPath = value);
			ResolveReferencedSingleAsset(p_selection, "RAB.StartPos", track.StartPosFile, new[] { ".SPB" }, p_selection.SpbPath, value => p_selection.SpbPath = value);
			ResolveReferencedSingleAsset(p_selection, "RAB.Hazard", track.HazardFile, new[] { ".HZB" }, p_selection.HzbPath, value => p_selection.HzbPath = value);
			ResolveReferencedSingleAsset(p_selection, "RAB.Checkpoints", checkpointReference, new[] { ".CPB" }, p_selection.CpbPath, value => p_selection.CpbPath = value);
			ResolveCollisionReference(p_selection, "RAB.CheckpointCollision", checkpointCollisionReference);
			ResolveReferencedListAsset(p_selection, "RAB.PowerupLayout", powerupLayoutReference, new[] { ".PWB" }, p_selection.PwbPaths);

			ResolveReferencedListAsset(p_selection, "RAB.PowerupMaterialAnimation", powerupMaterialAnimation, new[] { ".MAB" }, p_selection.MabPaths);

			if (!string.IsNullOrEmpty(auxiliaryPowerupScene))
			{
				AddSkippedAsset(p_selection, "RAB.PowerupScene", auxiliaryPowerupScene + " (auxiliary WDB not imported into scene collections)");
			}

			if (track.EmitterFiles != null)
			{
				for (int i = 0; i < track.EmitterFiles.Length; i++)
				{
					ResolveReferencedListAsset(p_selection, "RAB.Emitter[" + i + "]", track.EmitterFiles[i], new[] { ".EMB" }, p_selection.EmbPaths);
				}
			}

			if (track.MaybeCollisionMeshes != null)
			{
				for (int i = 0; i < track.MaybeCollisionMeshes.Length; i++)
				{
					string reference = track.MaybeCollisionMeshes[i];
					if (!string.IsNullOrWhiteSpace(reference))
					{
						ResolveCollisionReference(p_selection, "RAB.Collision[" + i + "]", reference);
					}
				}
			}

			if (p_selection.RrbPaths.Count == 0 && p_selection.AutoResolveReferencedAssets)
			{
				string rabDirectory = Path.GetDirectoryName(p_selection.RabPath);
				if (!string.IsNullOrEmpty(rabDirectory) && Directory.Exists(rabDirectory))
				{
					string[] rrbFiles = Directory.GetFiles(rabDirectory, "*.RRB", SearchOption.TopDirectoryOnly);
					for (int i = 0; i < rrbFiles.Length; i++)
					{
						AddUniquePath(p_selection.RrbPaths, rrbFiles[i]);
						AddResolvedAsset(p_selection, "RAB.NpcPath[" + i + "]", Path.GetFileName(rrbFiles[i]), rrbFiles[i]);
					}
				}
			}
			else if (p_selection.RrbPaths.Count == 0)
			{
				AddSkippedAsset(p_selection, "RAB.NpcPath", "Auto-resolution disabled");
			}
		}

		private static void ResolveWdbReferencedAssets(SceneExportSelection p_selection)
		{
			if (p_selection == null || string.IsNullOrEmpty(p_selection.WdbPath))
			{
				return;
			}

			WDB wdb = LoadWdb(p_selection.WdbPath);
			string wdbDirectory = Path.GetDirectoryName(p_selection.WdbPath);

			ResolveReferencedGdbPaths(p_selection, wdbDirectory, GetReferencedNames(wdb.GDBs), "WDB.GDB");
			ResolveReferencedGdbPaths(p_selection, wdbDirectory, GetReferencedNames(wdb.GDB2s), "WDB.GDB2");
			ResolveReferencedCollisionPaths(p_selection, wdbDirectory, GetReferencedNames(wdb.BVBs), new[] { ".BVB" }, p_selection.CollisionBvbPaths, "WDB.BVB");
			ResolveReferencedListAssets(p_selection, "WDB.MAB", GetReferencedNames(wdb.MABs), new[] { ".MAB" }, p_selection.MabPaths);
			ResolveReferencedListAssets(p_selection, "WDB.MDB", GetReferencedNames(wdb.MDBs), new[] { ".MDB" }, p_selection.MdbPaths);
			ResolveReferencedListAssets(p_selection, "WDB.TDB", GetReferencedNames(wdb.TDBs), new[] { ".TDB" }, p_selection.TdbPaths);
			ResolveCollisionWdbReferencedAssets(p_selection);
		}

		private static void ResolveNpcPathReferences(SceneExportSelection p_selection)
		{
			if (p_selection == null || p_selection.RrbPaths.Count > 0 || string.IsNullOrEmpty(p_selection.WdbPath) || !p_selection.AutoResolveReferencedAssets)
			{
				return;
			}

			string directory = Path.GetDirectoryName(p_selection.WdbPath);
			if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
			{
				return;
			}

			string[] rrbFiles = Directory.GetFiles(directory, "*.RRB", SearchOption.TopDirectoryOnly);
			for (int i = 0; i < rrbFiles.Length; i++)
			{
				AddUniquePath(p_selection.RrbPaths, rrbFiles[i]);
				AddResolvedAsset(p_selection, "WDB.RRB[" + i + "]", Path.GetFileName(rrbFiles[i]), rrbFiles[i]);
			}
		}

		private static void ResolveReferencedGdbPaths(SceneExportSelection p_selection, string p_primaryDirectory, IList<string> p_referenceNames, string p_labelPrefix)
		{
			if (p_referenceNames == null)
			{
				return;
			}

			for (int i = 0; i < p_referenceNames.Count; i++)
			{
				string referenceName = p_referenceNames[i];
				if (HasResolvedGdbReference(p_selection.GdbPaths, referenceName))
				{
					continue;
				}

				if (!p_selection.AutoResolveGdbReferences)
				{
					AddUniqueName(p_selection.UnresolvedGdbReferences, referenceName);
					AddSkippedAsset(p_selection, p_labelPrefix + "[" + i + "]", referenceName + " (auto-resolution disabled)");
					continue;
				}

				string resolved = ResolveAliasedAsset(p_selection, p_primaryDirectory, referenceName, new[] { ".GDB" });
				if (!string.IsNullOrEmpty(resolved))
				{
					AddUniquePath(p_selection.GdbPaths, resolved);
					AddUniquePath(p_selection.AutoResolvedGdbPaths, resolved);
					AddResolvedAsset(p_selection, p_labelPrefix + "[" + i + "]", referenceName, resolved);
					continue;
				}

				AddUniqueName(p_selection.UnresolvedGdbReferences, referenceName);
				AddSkippedAsset(p_selection, p_labelPrefix + "[" + i + "]", referenceName);
			}
		}

		private static void ResolveReferencedListAssets(SceneExportSelection p_selection, string p_labelPrefix, IList<string> p_references, string[] p_extensions, List<string> p_target)
		{
			if (p_references == null)
			{
				return;
			}

			for (int i = 0; i < p_references.Count; i++)
			{
				ResolveReferencedListAsset(p_selection, p_labelPrefix + "[" + i + "]", p_references[i], p_extensions, p_target);
			}
		}

		private static void ResolveReferencedListAsset(SceneExportSelection p_selection, string p_label, string p_reference, string[] p_extensions, List<string> p_target)
		{
			if (string.IsNullOrWhiteSpace(p_reference))
			{
				return;
			}

			if (ContainsResolvedReference(p_target, p_reference))
			{
				return;
			}

			if (!p_selection.AutoResolveReferencedAssets)
			{
				AddSkippedAsset(p_selection, p_label, p_reference + " (auto-resolution disabled)");
				return;
			}

			string resolved = ResolveAliasedAsset(p_selection, GetPrimaryDirectory(p_selection), p_reference, p_extensions);
			if (!string.IsNullOrEmpty(resolved))
			{
				AddUniquePath(p_target, resolved);
				AddResolvedAsset(p_selection, p_label, p_reference, resolved);
				return;
			}

			AddSkippedAsset(p_selection, p_label, p_reference);
		}

		private static void ResolveReferencedSingleAsset(
			SceneExportSelection p_selection,
			string p_label,
			string p_reference,
			string[] p_extensions,
			string p_existingValue,
			Action<string> p_assign)
		{
			if (string.IsNullOrWhiteSpace(p_reference))
			{
				return;
			}

			if (!string.IsNullOrEmpty(p_existingValue))
			{
				AddSkippedAsset(p_selection, p_label, p_reference + " (explicit input already selected)");
				return;
			}

			if (!p_selection.AutoResolveReferencedAssets)
			{
				AddSkippedAsset(p_selection, p_label, p_reference + " (auto-resolution disabled)");
				return;
			}

			string resolved = ResolveAliasedAsset(p_selection, GetPrimaryDirectory(p_selection), p_reference, p_extensions);
			if (!string.IsNullOrEmpty(resolved))
			{
				p_assign(resolved);
				AddResolvedAsset(p_selection, p_label, p_reference, resolved);
				return;
			}

			AddSkippedAsset(p_selection, p_label, p_reference);
		}

		private static void ResolveCollisionReference(SceneExportSelection p_selection, string p_label, string p_reference)
		{
			if (string.IsNullOrWhiteSpace(p_reference))
			{
				return;
			}

			if (!p_selection.AutoResolveReferencedAssets)
			{
				AddSkippedAsset(p_selection, p_label, p_reference + " (auto-resolution disabled)");
				return;
			}

			string primaryDirectory = GetPrimaryDirectory(p_selection);
			string resolved = ResolveAliasedAsset(p_selection, primaryDirectory, p_reference, new[] { ".WDB", ".WDF" });
			if (!string.IsNullOrEmpty(resolved))
			{
				AddUniquePath(p_selection.CollisionWdbPaths, resolved);
				AddResolvedAsset(p_selection, p_label, p_reference, resolved);
				return;
			}

			resolved = ResolveAliasedAsset(p_selection, primaryDirectory, p_reference, new[] { ".BVB" });
			if (!string.IsNullOrEmpty(resolved))
			{
				AddUniquePath(p_selection.CollisionBvbPaths, resolved);
				AddResolvedAsset(p_selection, p_label, p_reference, resolved);
				return;
			}

			resolved = ResolveAliasedAsset(p_selection, primaryDirectory, p_reference, new[] { ".GDB" });
			if (!string.IsNullOrEmpty(resolved))
			{
				AddUniquePath(p_selection.CollisionGdbPaths, resolved);
				AddResolvedAsset(p_selection, p_label, p_reference, resolved);
				return;
			}

			AddSkippedAsset(p_selection, p_label, p_reference);
		}

		private static void ResolveCollisionWdbReferencedAssets(SceneExportSelection p_selection)
		{
			if (p_selection == null || p_selection.CollisionWdbPaths.Count == 0)
			{
				return;
			}

			for (int i = 0; i < p_selection.CollisionWdbPaths.Count; i++)
			{
				string collisionWdbPath = p_selection.CollisionWdbPaths[i];
				if (string.IsNullOrEmpty(collisionWdbPath) || !File.Exists(collisionWdbPath))
				{
					continue;
				}

				WDB collisionWdb = LoadWdb(collisionWdbPath);
				string collisionDirectory = Path.GetDirectoryName(collisionWdbPath);
				ResolveReferencedCollisionPaths(p_selection, collisionDirectory, GetReferencedNames(collisionWdb.GDBs), new[] { ".GDB" }, p_selection.CollisionGdbPaths, "CollisionWDB.GDB");
				ResolveReferencedCollisionPaths(p_selection, collisionDirectory, GetReferencedNames(collisionWdb.BVBs), new[] { ".BVB" }, p_selection.CollisionBvbPaths, "CollisionWDB.BVB");
			}
		}

		private static void ResolveReferencedCollisionPaths(
			SceneExportSelection p_selection,
			string p_primaryDirectory,
			IList<string> p_references,
			string[] p_extensions,
			List<string> p_target,
			string p_labelPrefix)
		{
			if (p_references == null)
			{
				return;
			}

			for (int i = 0; i < p_references.Count; i++)
			{
				string reference = p_references[i];
				if (string.IsNullOrWhiteSpace(reference) || ContainsResolvedReference(p_target, reference))
				{
					continue;
				}

				string label = p_labelPrefix + "[" + i.ToString(CultureInfo.InvariantCulture) + "]";
				string resolved = ResolveAliasedAsset(p_selection, p_primaryDirectory, reference, p_extensions);
				if (!string.IsNullOrEmpty(resolved))
				{
					AddUniquePath(p_target, resolved);
					AddResolvedAsset(p_selection, label, reference, resolved);
				}
				else
				{
					AddSkippedAsset(p_selection, label, reference);
				}
			}
		}

		private static string ResolveOutputPath(string p_selectionOutputPath, string p_sceneName)
		{
			if (!string.IsNullOrEmpty(p_selectionOutputPath))
			{
				return p_selectionOutputPath;
			}

			string exportFileName = p_sceneName + ".json";
			return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports", exportFileName);
		}

		private static string GetCommandLineOutputPath(string[] p_args)
		{
			if (p_args == null || p_args.Length <= 1)
			{
				return null;
			}

			string lastArgument = p_args[p_args.Length - 1];
			return IsJsonPath(lastArgument) ? Path.GetFullPath(lastArgument) : null;
		}

		private static string GetConfiguredOutputPath(string p_configuredOutputPath)
		{
			return string.IsNullOrEmpty(p_configuredOutputPath) ? null : Path.GetFullPath(p_configuredOutputPath);
		}

		private static bool IsJsonPath(string p_path)
		{
			return !string.IsNullOrEmpty(p_path) && string.Equals(Path.GetExtension(p_path), ".json", StringComparison.OrdinalIgnoreCase);
		}

		private static string AssignSingleInput(string p_label, string p_resolvedPath, string p_existingPath)
		{
			if (!string.IsNullOrEmpty(p_existingPath) && !string.Equals(p_existingPath, p_resolvedPath, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Multiple " + p_label + " inputs were provided. Only one is supported.");
			}

			return p_resolvedPath;
		}

		private static void AddUniquePath(List<string> p_paths, string p_path)
		{
			if (string.IsNullOrEmpty(p_path))
			{
				return;
			}

			for (int i = 0; i < p_paths.Count; i++)
			{
				if (string.Equals(p_paths[i], p_path, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
			}

			p_paths.Add(p_path);
		}

		private static string GetSceneName(SceneExportSelection p_selection)
		{
			string primaryPath = !string.IsNullOrEmpty(p_selection.PrimaryInputPath) ? p_selection.PrimaryInputPath : GetPrimarySelectedPath(p_selection);
			return !string.IsNullOrEmpty(primaryPath) ? Path.GetFileNameWithoutExtension(primaryPath) : "ExportedScene";
		}

		private static string GetPrimarySelectedPath(SceneExportSelection p_selection)
		{
			if (!string.IsNullOrEmpty(p_selection.RabPath)) return p_selection.RabPath;
			if (!string.IsNullOrEmpty(p_selection.WdbPath)) return p_selection.WdbPath;
			if (p_selection.GdbPaths.Count > 0) return p_selection.GdbPaths[0];
			if (p_selection.CollisionBvbPaths.Count > 0) return p_selection.CollisionBvbPaths[0];
			if (p_selection.CollisionGdbPaths.Count > 0) return p_selection.CollisionGdbPaths[0];
			if (!string.IsNullOrEmpty(p_selection.SkbPath)) return p_selection.SkbPath;
			if (p_selection.MabPaths.Count > 0) return p_selection.MabPaths[0];
			if (!string.IsNullOrEmpty(p_selection.SpbPath)) return p_selection.SpbPath;
			if (!string.IsNullOrEmpty(p_selection.CpbPath)) return p_selection.CpbPath;
			if (p_selection.PwbPaths.Count > 0) return p_selection.PwbPaths[0];
			if (!string.IsNullOrEmpty(p_selection.HzbPath)) return p_selection.HzbPath;
			if (p_selection.EmbPaths.Count > 0) return p_selection.EmbPaths[0];
			if (p_selection.RrbPaths.Count > 0) return p_selection.RrbPaths[0];
			return null;
		}

		private static string GetPrimaryDirectory(SceneExportSelection p_selection)
		{
			string primaryPath = GetPrimarySelectedPath(p_selection);
			return !string.IsNullOrEmpty(primaryPath) ? Path.GetDirectoryName(primaryPath) : null;
		}

		private static void AddSelectionMetadata(TrackScene p_scene, SceneExportSelection p_selection)
		{
			AddMetadataValue(p_scene.Metadata, "Export.Input.Primary", p_selection.PrimaryInputPath);
			AddMetadataValue(p_scene.Metadata, "Export.Input.RAB", p_selection.RabPath);
			AddMetadataValue(p_scene.Metadata, "Export.Input.WDB", p_selection.WdbPath);
			AddMetadataArray(p_scene.Metadata, "Export.Input.CollisionWDB", p_selection.CollisionWdbPaths);
			AddMetadataArray(p_scene.Metadata, "Export.Input.CollisionGDB", p_selection.CollisionGdbPaths);
			AddMetadataArray(p_scene.Metadata, "Export.Input.CollisionBVB", p_selection.CollisionBvbPaths);
			AddMetadataValue(p_scene.Metadata, "Export.Input.SKB", p_selection.SkbPath);
			AddMetadataArray(p_scene.Metadata, "Export.Input.MAB", p_selection.MabPaths);
			AddMetadataValue(p_scene.Metadata, "Export.Input.SPB", p_selection.SpbPath);
			AddMetadataValue(p_scene.Metadata, "Export.Input.CPB", p_selection.CpbPath);
			AddMetadataValue(p_scene.Metadata, "Export.Input.HZB", p_selection.HzbPath);
			AddMetadataArray(p_scene.Metadata, "Export.Input.GDB", p_selection.GdbPaths);
			AddMetadataArray(p_scene.Metadata, "Export.Input.GDB.Explicit", p_selection.ExplicitGdbPaths);
			AddMetadataArray(p_scene.Metadata, "Export.Input.GDB.AutoResolved", p_selection.AutoResolvedGdbPaths);
			AddMetadataArray(p_scene.Metadata, "Export.Input.GDB.Unresolved", p_selection.UnresolvedGdbReferences);
			AddMetadataArray(p_scene.Metadata, "Export.Input.PWB", p_selection.PwbPaths);
			AddMetadataArray(p_scene.Metadata, "Export.Input.EMB", p_selection.EmbPaths);
			AddMetadataArray(p_scene.Metadata, "Export.Input.RRB", p_selection.RrbPaths);
			AddMetadataArray(p_scene.Metadata, "Export.Input.MDB", p_selection.MdbPaths);
			AddMetadataArray(p_scene.Metadata, "Export.Input.TDB", p_selection.TdbPaths);
			AddMetadataArray(p_scene.Metadata, "Export.ResolvedAsset", p_selection.ResolvedAssetMessages);
			AddMetadataArray(p_scene.Metadata, "Export.FailedParseAsset", p_selection.FailedParseMessages);
			AddMetadataArray(p_scene.Metadata, "Export.SkippedAsset", p_selection.SkippedAssetMessages);
		}

		private static void LogSelection(SceneExportSelection p_selection, string p_outputPath)
		{
			Console.WriteLine("Selected export files:");
			Console.WriteLine("RAB: " + (!string.IsNullOrEmpty(p_selection.RabPath) ? p_selection.RabPath : "<none>"));
			Console.WriteLine("WDB: " + (!string.IsNullOrEmpty(p_selection.WdbPath) ? p_selection.WdbPath : "<none>"));
			LogPathList("Collision WDB(s)", p_selection.CollisionWdbPaths);
			LogPathList("Collision GDB(s)", p_selection.CollisionGdbPaths);
			LogPathList("Collision BVB(s)", p_selection.CollisionBvbPaths);
			LogPathList("Explicit GDB(s)", p_selection.ExplicitGdbPaths);
			LogPathList("Auto-resolved GDB(s)", p_selection.AutoResolvedGdbPaths);
			LogPathList("Unresolved GDB reference(s)", p_selection.UnresolvedGdbReferences);
			Console.WriteLine("SKB: " + (!string.IsNullOrEmpty(p_selection.SkbPath) ? p_selection.SkbPath : "<none>"));
			LogPathList("MAB(s)", p_selection.MabPaths);
			Console.WriteLine("SPB: " + (!string.IsNullOrEmpty(p_selection.SpbPath) ? p_selection.SpbPath : "<none>"));
			Console.WriteLine("CPB: " + (!string.IsNullOrEmpty(p_selection.CpbPath) ? p_selection.CpbPath : "<none>"));
			Console.WriteLine("HZB: " + (!string.IsNullOrEmpty(p_selection.HzbPath) ? p_selection.HzbPath : "<none>"));
			LogPathList("PWB(s)", p_selection.PwbPaths);
			LogPathList("EMB(s)", p_selection.EmbPaths);
			LogPathList("RRB(s)", p_selection.RrbPaths);
			LogPathList("MDB(s)", p_selection.MdbPaths);
			LogPathList("TDB(s)", p_selection.TdbPaths);
			LogPathList("Resolved referenced asset(s)", p_selection.ResolvedAssetMessages);
			LogPathList("Failed to parse asset(s)", p_selection.FailedParseMessages);
			LogPathList("Skipped referenced asset(s)", p_selection.SkippedAssetMessages);
			Console.WriteLine("GDB(s): " + p_selection.GdbPaths.Count.ToString(CultureInfo.InvariantCulture));
			Console.WriteLine("Output: " + p_outputPath);
		}

		private static void LogPathList(string p_label, IList<string> p_values)
		{
			if (p_values == null || p_values.Count == 0)
			{
				Console.WriteLine(p_label + ": <none>");
				return;
			}

			Console.WriteLine(p_label + ":");
			for (int i = 0; i < p_values.Count; i++)
			{
				Console.WriteLine("  [" + i.ToString(CultureInfo.InvariantCulture) + "] " + p_values[i]);
			}
		}

		private static bool ResolveAutoResolveGdbReferences(LocalGameFiles p_gameFiles)
		{
			string envValue = Environment.GetEnvironmentVariable("LR1TOOLS_AUTO_RESOLVE_GDB_REFERENCES");
			bool parsedValue;
			if (TryParseBoolean(envValue, out parsedValue))
			{
				return parsedValue;
			}

			return !p_gameFiles.AutoResolveGdbReferences.HasValue || p_gameFiles.AutoResolveGdbReferences.Value;
		}

		private static bool ResolveAutoResolveReferencedAssets(LocalGameFiles p_gameFiles)
		{
			string envValue = Environment.GetEnvironmentVariable("LR1TOOLS_AUTO_RESOLVE_REFERENCED_ASSETS");
			bool parsedValue;
			if (TryParseBoolean(envValue, out parsedValue))
			{
				return parsedValue;
			}

			return !p_gameFiles.AutoResolveReferencedAssets.HasValue || p_gameFiles.AutoResolveReferencedAssets.Value;
		}

		private static bool TryParseBoolean(string p_value, out bool p_result)
		{
			p_result = false;
			if (string.IsNullOrWhiteSpace(p_value)) return false;

			string normalized = p_value.Trim();
			if (string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "on", StringComparison.OrdinalIgnoreCase))
			{
				p_result = true;
				return true;
			}

			if (string.Equals(normalized, "0", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "false", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "no", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "off", StringComparison.OrdinalIgnoreCase))
			{
				p_result = false;
				return true;
			}

			return false;
		}

		private static string ResolveFilePath(string p_installationPath, string p_configuredPath, string p_fallbackPattern)
		{
			string resolved = ResolvePath(p_installationPath, p_configuredPath);
			if (!string.IsNullOrEmpty(resolved))
			{
				return resolved;
			}

			string[] matches = Directory.GetFiles(p_installationPath, p_fallbackPattern, SearchOption.AllDirectories);
			return matches.Length > 0 ? matches[0] : null;
		}

		private static string ResolveExplicitPath(string p_installationPath, string p_inputPath)
		{
			if (string.IsNullOrEmpty(p_inputPath))
			{
				return null;
			}

			if (Path.IsPathRooted(p_inputPath))
			{
				return File.Exists(p_inputPath) ? Path.GetFullPath(p_inputPath) : null;
			}

			string directCandidate = Path.GetFullPath(p_inputPath);
			if (File.Exists(directCandidate))
			{
				return directCandidate;
			}

			string repoCandidate = ResolveRepoFile(p_inputPath);
			if (!string.IsNullOrEmpty(repoCandidate) && File.Exists(repoCandidate))
			{
				return repoCandidate;
			}

			string installationCandidate = Path.Combine(p_installationPath, p_inputPath);
			return File.Exists(installationCandidate) ? installationCandidate : null;
		}

		private static string ResolvePath(string p_installationPath, string p_configuredPath)
		{
			if (string.IsNullOrEmpty(p_configuredPath))
			{
				return null;
			}

			if (Path.IsPathRooted(p_configuredPath))
			{
				return File.Exists(p_configuredPath) ? p_configuredPath : null;
			}

			string installationCandidate = Path.Combine(p_installationPath, p_configuredPath);
			if (File.Exists(installationCandidate))
			{
				return installationCandidate;
			}

			string repoCandidate = ResolveRepoFile(p_configuredPath);
			return !string.IsNullOrEmpty(repoCandidate) && File.Exists(repoCandidate) ? repoCandidate : null;
		}

		private static string ResolveAliasedAsset(SceneExportSelection p_selection, string p_primaryDirectory, string p_reference, string[] p_extensions)
		{
			if (string.IsNullOrWhiteSpace(p_reference))
			{
				return null;
			}

			string normalizedReference = Path.GetFileName(p_reference.Trim());
			List<string> candidateNames = BuildCandidateNames(normalizedReference, p_extensions);
			List<string> roots = new List<string>();

			AddUniqueDirectory(roots, p_primaryDirectory);
			AddUniqueDirectory(roots, GetPrimaryDirectory(p_selection));
			AddUniqueDirectory(roots, GetGameDataRoot(p_selection.InstallationPath, p_primaryDirectory));
			AddUniqueDirectory(roots, p_selection.InstallationPath);

			for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
			{
				string root = roots[rootIndex];
				for (int candidateIndex = 0; candidateIndex < candidateNames.Count; candidateIndex++)
				{
					string directCandidate = Path.Combine(root, candidateNames[candidateIndex]);
					if (File.Exists(directCandidate))
					{
						return Path.GetFullPath(directCandidate);
					}
				}
			}

			for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
			{
				string root = roots[rootIndex];
				for (int candidateIndex = 0; candidateIndex < candidateNames.Count; candidateIndex++)
				{
					string[] matches = Directory.GetFiles(root, candidateNames[candidateIndex], SearchOption.AllDirectories);
					if (matches.Length > 0)
					{
						return matches[0];
					}
				}
			}

			return null;
		}

		private static string ResolveTexturePath(SceneExportSelection p_selection, string p_textureName)
		{
			if (string.IsNullOrWhiteSpace(p_textureName))
			{
				return null;
			}

			string extension = Path.GetExtension(p_textureName);
			string[] candidateExtensions = string.IsNullOrEmpty(extension) ? new[] { ".BMP", ".TGA" } : new[] { extension };
			return ResolveAliasedAsset(p_selection, GetPrimaryDirectory(p_selection), p_textureName, candidateExtensions);
		}

		private static List<string> BuildCandidateNames(string p_reference, string[] p_extensions)
		{
			List<string> candidates = new List<string>();
			string fileName = Path.GetFileName(p_reference);
			if (string.IsNullOrEmpty(fileName))
			{
				return candidates;
			}

			AddUniqueName(candidates, fileName);
			string stem = Path.GetFileNameWithoutExtension(fileName);
			if (!string.IsNullOrEmpty(stem) && p_extensions != null)
			{
				for (int i = 0; i < p_extensions.Length; i++)
				{
					if (!string.IsNullOrEmpty(p_extensions[i]))
					{
						AddUniqueName(candidates, stem + p_extensions[i]);
					}
				}

				foreach (string aliasStem in GetAssetStemAliases(stem))
				{
					for (int i = 0; i < p_extensions.Length; i++)
					{
						if (!string.IsNullOrEmpty(p_extensions[i]))
						{
							AddUniqueName(candidates, aliasStem + p_extensions[i]);
						}
					}
				}
			}

			return candidates;
		}

		private static IEnumerable<string> GetAssetStemAliases(string p_stem)
		{
			if (string.IsNullOrWhiteSpace(p_stem))
			{
				yield break;
			}

			if (string.Equals(p_stem, "strtlne", StringComparison.OrdinalIgnoreCase))
			{
				yield return "startfin";
				yield return "starfin";
				yield break;
			}

			if (string.Equals(p_stem, "startfin", StringComparison.OrdinalIgnoreCase))
			{
				yield return "strtlne";
				yield return "starfin";
				yield break;
			}

			if (string.Equals(p_stem, "starfin", StringComparison.OrdinalIgnoreCase))
			{
				yield return "startfin";
				yield return "strtlne";
			}
		}

		private static IList<string> GetReferencedNames(string[] p_values)
		{
			List<string> names = new List<string>();
			if (p_values == null)
			{
				return names;
			}

			for (int i = 0; i < p_values.Length; i++)
			{
				AddUniqueName(names, p_values[i]);
			}

			return names;
		}

		private static bool ContainsResolvedReference(IList<string> p_paths, string p_reference)
		{
			string expectedStem = Path.GetFileNameWithoutExtension(Path.GetFileName(p_reference));
			if (string.IsNullOrEmpty(expectedStem))
			{
				return false;
			}

			for (int i = 0; i < p_paths.Count; i++)
			{
				if (!string.IsNullOrEmpty(p_paths[i]) && string.Equals(Path.GetFileNameWithoutExtension(p_paths[i]), expectedStem, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private static bool HasResolvedGdbReference(IList<string> p_paths, string p_referenceName)
		{
			return ContainsResolvedReference(p_paths, p_referenceName);
		}

		private static string GetGameDataRoot(string p_installationPath, string p_assetPath)
		{
			if (string.IsNullOrEmpty(p_assetPath))
			{
				return null;
			}

			string assetDirectory = Directory.Exists(p_assetPath) ? p_assetPath : Path.GetDirectoryName(Path.GetFullPath(p_assetPath));
			if (string.IsNullOrEmpty(assetDirectory))
			{
				return null;
			}

			if (string.IsNullOrEmpty(p_installationPath))
			{
				return assetDirectory;
			}

			string installationPath = Path.GetFullPath(p_installationPath);
			if (!assetDirectory.StartsWith(installationPath, StringComparison.OrdinalIgnoreCase))
			{
				return assetDirectory;
			}

			string relativeDirectory = assetDirectory.Substring(installationPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (string.IsNullOrEmpty(relativeDirectory))
			{
				return assetDirectory;
			}

			string[] segments = relativeDirectory.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			return segments.Length > 0 ? Path.Combine(installationPath, segments[0]) : assetDirectory;
		}

		private static void AddUniqueDirectory(List<string> p_directories, string p_directory)
		{
			if (string.IsNullOrWhiteSpace(p_directory) || !Directory.Exists(p_directory))
			{
				return;
			}

			for (int i = 0; i < p_directories.Count; i++)
			{
				if (string.Equals(p_directories[i], p_directory, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
			}

			p_directories.Add(p_directory);
		}

		private static void AddUniqueName(List<string> p_names, string p_name)
		{
			string normalized = string.IsNullOrWhiteSpace(p_name) ? null : p_name.Trim();
			if (string.IsNullOrEmpty(normalized))
			{
				return;
			}

			for (int i = 0; i < p_names.Count; i++)
			{
				if (string.Equals(p_names[i], normalized, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
			}

			p_names.Add(normalized);
		}

		private static void AddResolvedAsset(SceneExportSelection p_selection, string p_label, string p_reference, string p_resolvedPath)
		{
			p_selection.ResolvedAssetMessages.Add(p_label + ": " + p_reference + " -> " + p_resolvedPath);
		}

		private static void RemoveResolvedAssetByPath(SceneExportSelection p_selection, string p_resolvedPath)
		{
			if (p_selection == null || string.IsNullOrEmpty(p_resolvedPath))
			{
				return;
			}

			string suffix = "-> " + p_resolvedPath;
			for (int i = p_selection.ResolvedAssetMessages.Count - 1; i >= 0; i--)
			{
				string message = p_selection.ResolvedAssetMessages[i];
				if (!string.IsNullOrEmpty(message) && message.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
				{
					p_selection.ResolvedAssetMessages.RemoveAt(i);
				}
			}
		}

		private static void AddSkippedAsset(SceneExportSelection p_selection, string p_label, string p_reference)
		{
			p_selection.SkippedAssetMessages.Add(p_label + ": " + p_reference);
		}

		private static void AddFailedParseAsset(SceneExportSelection p_selection, string p_label, string p_path, string p_reason)
		{
			string reason = string.IsNullOrWhiteSpace(p_reason) ? "unknown parse error" : p_reason.Trim();
			p_selection.FailedParseMessages.Add(p_label + ": " + p_path + " (" + reason + ")");
		}

		private static void AddMetadataValue(IDictionary<string, string> p_metadata, string p_key, string p_value)
		{
			if (!string.IsNullOrEmpty(p_value))
			{
				p_metadata[p_key] = p_value;
			}
		}

		private static void AddMetadataArray(IDictionary<string, string> p_metadata, string p_prefix, IList<string> p_values)
		{
			if (p_values == null || p_values.Count == 0)
			{
				return;
			}

			p_metadata[p_prefix + ".Count"] = p_values.Count.ToString(CultureInfo.InvariantCulture);
			for (int i = 0; i < p_values.Count; i++)
			{
				p_metadata[string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", p_prefix, i)] = p_values[i] ?? string.Empty;
			}
		}

		private static void AddMetadataArray(IDictionary<string, string> p_metadata, string p_prefix, string[] p_values)
		{
			if (p_values == null || p_values.Length == 0)
			{
				return;
			}

			p_metadata[p_prefix + ".Count"] = p_values.Length.ToString(CultureInfo.InvariantCulture);
			for (int i = 0; i < p_values.Length; i++)
			{
				p_metadata[string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", p_prefix, i)] = p_values[i] ?? string.Empty;
			}
		}

		private static string GetArrayValue(string[] p_values, int p_index)
		{
			return p_values != null && p_index >= 0 && p_index < p_values.Length ? p_values[p_index] : null;
		}

		private static string ResolveRepoFile(string p_relativePath)
		{
			string[] candidates = new string[]
			{
				Path.Combine(Directory.GetCurrentDirectory(), p_relativePath),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p_relativePath),
				Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\", p_relativePath)),
				Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\", p_relativePath))
			};

			for (int i = 0; i < candidates.Length; i++)
			{
				if (File.Exists(candidates[i]))
				{
					return candidates[i];
				}
			}

			return candidates[0];
		}

		private sealed class LocalGameFiles
		{
			public string RabPath { get; set; }
			public string WdbPath { get; set; }
			public List<string> GdbPaths { get; set; }
			public string SkbPath { get; set; }
			public List<string> MabPaths { get; set; }
			public string SpbPath { get; set; }
			public string CpbPath { get; set; }
			public List<string> PwbPaths { get; set; }
			public string HzbPath { get; set; }
			public List<string> EmbPaths { get; set; }
			public List<string> RrbPaths { get; set; }
			public List<string> MdbPaths { get; set; }
			public List<string> TdbPaths { get; set; }
			public string OutputPath { get; set; }
			public bool? AutoResolveGdbReferences { get; set; }
			public bool? AutoResolveReferencedAssets { get; set; }
		}

		private sealed class SceneExportSelection
		{
			public string InstallationPath { get; set; }
			public bool AutoResolveGdbReferences { get; set; }
			public bool AutoResolveReferencedAssets { get; set; }
			public string PrimaryInputPath { get; set; }
			public string SceneName { get; set; }
			public string RabPath { get; set; }
			public string WdbPath { get; set; }
			public List<string> GdbPaths { get; private set; }
			public List<string> ExplicitGdbPaths { get; private set; }
			public List<string> AutoResolvedGdbPaths { get; private set; }
			public List<string> UnresolvedGdbReferences { get; private set; }
			public List<string> CollisionWdbPaths { get; private set; }
			public List<string> CollisionGdbPaths { get; private set; }
			public List<string> CollisionBvbPaths { get; private set; }
			public string SkbPath { get; set; }
			public List<string> MabPaths { get; private set; }
			public string SpbPath { get; set; }
			public string CpbPath { get; set; }
			public List<string> PwbPaths { get; private set; }
			public string HzbPath { get; set; }
			public List<string> EmbPaths { get; private set; }
			public List<string> RrbPaths { get; private set; }
			public List<string> MdbPaths { get; private set; }
			public List<string> TdbPaths { get; private set; }
			public List<string> ResolvedAssetMessages { get; private set; }
			public List<string> FailedParseMessages { get; private set; }
			public List<string> SkippedAssetMessages { get; private set; }
			public string OutputPath { get; set; }

			public SceneExportSelection()
			{
				GdbPaths = new List<string>();
				ExplicitGdbPaths = new List<string>();
				AutoResolvedGdbPaths = new List<string>();
				UnresolvedGdbReferences = new List<string>();
				CollisionWdbPaths = new List<string>();
				CollisionGdbPaths = new List<string>();
				CollisionBvbPaths = new List<string>();
				MabPaths = new List<string>();
				PwbPaths = new List<string>();
				EmbPaths = new List<string>();
				RrbPaths = new List<string>();
				MdbPaths = new List<string>();
				TdbPaths = new List<string>();
				ResolvedAssetMessages = new List<string>();
				FailedParseMessages = new List<string>();
				SkippedAssetMessages = new List<string>();
			}
		}
	}
}
