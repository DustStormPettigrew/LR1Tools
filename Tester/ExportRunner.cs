using LR1Tools.Adapters;
using LR1Tools.Contracts;
using LR1Tools.Export;
using LibLR1;
using System;
using System.Collections.Generic;
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
				wdb = new WDB(p_selection.WdbPath);
				scene = WDBAdapter.ToScene(wdb, Path.GetFileNameWithoutExtension(p_selection.WdbPath));
			}

			if (scene == null)
			{
				scene = new TrackScene();
				scene.Name = !string.IsNullOrEmpty(p_selection.SceneName) ? p_selection.SceneName : "ExportedScene";
				scene.SourceName = "Contracts";
			}

			AddGdbMeshes(scene, p_selection.GdbPaths);

			if (!string.IsNullOrEmpty(p_selection.SkbPath))
			{
				SKB skb = new SKB(p_selection.SkbPath);
				TrackScene skbScene = SKBAdapter.ToScene(skb, Path.GetFileNameWithoutExtension(p_selection.SkbPath));
				MergeScene(scene, skbScene, false, true, false, false, true, "SKB");
			}

			if (!string.IsNullOrEmpty(p_selection.RrbPath))
			{
				RRB rrb = new RRB(p_selection.RrbPath);
				TrackScene rrbScene = RRBAdapter.ToScene(rrb, Path.GetFileNameWithoutExtension(p_selection.RrbPath));
				MergeScene(scene, rrbScene, true, false, true, true, false, "RRB");
			}

			WDBAdapter.ResolveMeshReferences(scene);
			AddMeshResolutionMetadata(scene);

			if (!string.IsNullOrEmpty(p_selection.SceneName))
			{
				scene.Name = p_selection.SceneName;
			}

			AddSelectionMetadata(scene, p_selection);
			return scene;
		}

		private static void AddGdbMeshes(TrackScene p_target, IList<string> p_gdbPaths)
		{
			if (p_target == null || p_gdbPaths == null || p_gdbPaths.Count == 0)
			{
				return;
			}

			HashSet<string> materialNames = CreateNameSet(p_target.Materials);
			HashSet<string> meshNames = CreateNameSet(p_target.Meshes);

			for (int i = 0; i < p_gdbPaths.Count; i++)
			{
				GDB gdb = new GDB(p_gdbPaths[i]);
				TrackScene gdbScene = GDBAdapter.ToScene(gdb, Path.GetFileNameWithoutExtension(p_gdbPaths[i]));
				MergeUniqueMaterials(p_target.Materials, gdbScene.Materials, materialNames);
				MergeUniqueMeshes(p_target.Meshes, gdbScene.Meshes, meshNames);
				CopyMetadata(p_target.Metadata, gdbScene.Metadata, "GDB." + i);
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

			foreach (KeyValuePair<string, string> pair in p_source.Metadata)
			{
				p_target.Metadata[p_metadataPrefix + "." + pair.Key] = pair.Value;
			}
		}

		private static void MergeUniqueMaterials(
			IList<TrackMaterial> p_target,
			IList<TrackMaterial> p_source,
			HashSet<string> p_knownNames)
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

		private static void MergeUniqueMeshes(
			IList<TrackMesh> p_target,
			IList<TrackMesh> p_source,
			HashSet<string> p_knownNames)
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

		private static HashSet<string> CreateNameSet(IList<TrackMaterial> p_items)
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

		private static HashSet<string> CreateNameSet(IList<TrackMesh> p_items)
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

		private static string GetNameKey(string p_name)
		{
			return string.IsNullOrWhiteSpace(p_name) ? null : p_name.Trim();
		}

		private static void CopyMetadata(
			IDictionary<string, string> p_target,
			IDictionary<string, string> p_source,
			string p_metadataPrefix)
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

			HashSet<string> meshNames = CreateNameSet(p_scene.Meshes);
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

			p_scene.Metadata["Export.MeshReference.ObjectCount"] = referencedObjectCount.ToString();
			p_scene.Metadata["Export.MeshReference.ResolvedCount"] = resolvedObjectCount.ToString();
			p_scene.Metadata["Export.MeshReference.UnresolvedCount"] = unresolvedObjectCount.ToString();
		}

		private static SceneExportSelection ResolveSelection(string p_installationPath, LocalGameFiles p_gameFiles, string[] p_args)
		{
			SceneExportSelection selection = new SceneExportSelection();
			selection.InstallationPath = p_installationPath;

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

			ResolveDependentGdbPaths(selection);
			selection.SceneName = GetSceneName(selection);

			if (string.IsNullOrEmpty(selection.WdbPath) &&
				string.IsNullOrEmpty(selection.SkbPath) &&
				string.IsNullOrEmpty(selection.RrbPath) &&
				selection.GdbPaths.Count == 0)
			{
				throw new FileNotFoundException("No representative WDB, GDB, SKB, or RRB files could be resolved for export.");
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
				if (string.Equals(extension, ".GDB", StringComparison.OrdinalIgnoreCase))
				{
					AddUniquePath(p_selection.GdbPaths, resolved);
				}
				else if (string.Equals(extension, ".WDB", StringComparison.OrdinalIgnoreCase))
				{
					p_selection.WdbPath = AssignSingleInput("WDB", resolved, p_selection.WdbPath);
				}
				else if (string.Equals(extension, ".SKB", StringComparison.OrdinalIgnoreCase))
				{
					p_selection.SkbPath = AssignSingleInput("SKB", resolved, p_selection.SkbPath);
				}
				else if (string.Equals(extension, ".RRB", StringComparison.OrdinalIgnoreCase))
				{
					p_selection.RrbPath = AssignSingleInput("RRB", resolved, p_selection.RrbPath);
				}
				else
				{
					throw new InvalidOperationException("Unsupported export input type: " + resolved);
				}
			}
		}

		private static void ApplyConfiguredInputs(SceneExportSelection p_selection, LocalGameFiles p_gameFiles)
		{
			p_selection.WdbPath = ResolveFilePath(p_selection.InstallationPath, p_gameFiles.WdbPath, "*.WDB");
			p_selection.SkbPath = ResolveFilePath(p_selection.InstallationPath, p_gameFiles.SkbPath, "*.SKB");
			p_selection.RrbPath = ResolveFilePath(p_selection.InstallationPath, p_gameFiles.RrbPath, "*.RRB");

			if (p_gameFiles.GdbPaths != null)
			{
				for (int i = 0; i < p_gameFiles.GdbPaths.Count; i++)
				{
					string resolved = ResolvePath(p_selection.InstallationPath, p_gameFiles.GdbPaths[i]);
					if (!string.IsNullOrEmpty(resolved))
					{
						AddUniquePath(p_selection.GdbPaths, resolved);
					}
				}
			}

			p_selection.PrimaryInputPath = GetPrimarySelectedPath(p_selection);
		}

		private static void ResolveDependentGdbPaths(SceneExportSelection p_selection)
		{
			if (!string.IsNullOrEmpty(p_selection.WdbPath) && p_selection.GdbPaths.Count == 0)
			{
				WDB wdb = new WDB(p_selection.WdbPath);
				if (wdb.GDBs != null)
				{
					for (int i = 0; i < wdb.GDBs.Length; i++)
					{
						string resolved = FindFileByName(p_selection.InstallationPath, wdb.GDBs[i]);
						if (!string.IsNullOrEmpty(resolved))
						{
							AddUniquePath(p_selection.GdbPaths, resolved);
						}
					}
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
			if (string.IsNullOrEmpty(p_configuredOutputPath))
			{
				return null;
			}

			return Path.GetFullPath(p_configuredOutputPath);
		}

		private static bool IsJsonPath(string p_path)
		{
			return !string.IsNullOrEmpty(p_path) &&
				string.Equals(Path.GetExtension(p_path), ".json", StringComparison.OrdinalIgnoreCase);
		}

		private static string AssignSingleInput(string p_label, string p_resolvedPath, string p_existingPath)
		{
			if (!string.IsNullOrEmpty(p_existingPath) &&
				!string.Equals(p_existingPath, p_resolvedPath, StringComparison.OrdinalIgnoreCase))
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
			string primaryPath = !string.IsNullOrEmpty(p_selection.PrimaryInputPath)
				? p_selection.PrimaryInputPath
				: GetPrimarySelectedPath(p_selection);

			return !string.IsNullOrEmpty(primaryPath)
				? Path.GetFileNameWithoutExtension(primaryPath)
				: "ExportedScene";
		}

		private static string GetPrimarySelectedPath(SceneExportSelection p_selection)
		{
			if (!string.IsNullOrEmpty(p_selection.WdbPath))
			{
				return p_selection.WdbPath;
			}

			if (p_selection.GdbPaths.Count > 0)
			{
				return p_selection.GdbPaths[0];
			}

			if (!string.IsNullOrEmpty(p_selection.SkbPath))
			{
				return p_selection.SkbPath;
			}

			if (!string.IsNullOrEmpty(p_selection.RrbPath))
			{
				return p_selection.RrbPath;
			}

			return null;
		}

		private static void AddSelectionMetadata(TrackScene p_scene, SceneExportSelection p_selection)
		{
			if (!string.IsNullOrEmpty(p_selection.PrimaryInputPath))
			{
				p_scene.Metadata["Export.Input.Primary"] = p_selection.PrimaryInputPath;
			}

			if (!string.IsNullOrEmpty(p_selection.WdbPath))
			{
				p_scene.Metadata["Export.Input.WDB"] = p_selection.WdbPath;
			}

			if (p_selection.GdbPaths.Count > 0)
			{
				p_scene.Metadata["Export.Input.GDB.Count"] = p_selection.GdbPaths.Count.ToString();
				for (int i = 0; i < p_selection.GdbPaths.Count; i++)
				{
					p_scene.Metadata["Export.Input.GDB[" + i + "]"] = p_selection.GdbPaths[i];
				}
			}

			if (!string.IsNullOrEmpty(p_selection.SkbPath))
			{
				p_scene.Metadata["Export.Input.SKB"] = p_selection.SkbPath;
			}

			if (!string.IsNullOrEmpty(p_selection.RrbPath))
			{
				p_scene.Metadata["Export.Input.RRB"] = p_selection.RrbPath;
			}
		}

		private static void LogSelection(SceneExportSelection p_selection, string p_outputPath)
		{
			Console.WriteLine("Selected export files:");
			Console.WriteLine("WDB: " + (!string.IsNullOrEmpty(p_selection.WdbPath) ? p_selection.WdbPath : "<none>"));
			if (p_selection.GdbPaths.Count == 0)
			{
				Console.WriteLine("GDB(s): <none>");
			}
			else
			{
				Console.WriteLine("GDB(s):");
				for (int i = 0; i < p_selection.GdbPaths.Count; i++)
				{
					Console.WriteLine("  [" + i + "] " + p_selection.GdbPaths[i]);
				}
			}
			Console.WriteLine("SKB: " + (!string.IsNullOrEmpty(p_selection.SkbPath) ? p_selection.SkbPath : "<none>"));
			Console.WriteLine("RRB: " + (!string.IsNullOrEmpty(p_selection.RrbPath) ? p_selection.RrbPath : "<none>"));
			Console.WriteLine("Output: " + p_outputPath);
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

		private static string FindFileByName(string p_root, string p_fileName)
		{
			if (string.IsNullOrEmpty(p_root) || string.IsNullOrEmpty(p_fileName))
			{
				return null;
			}

			string searchName = Path.GetFileName(p_fileName);
			string[] matches = Directory.GetFiles(p_root, searchName, SearchOption.AllDirectories);
			return matches.Length > 0 ? matches[0] : null;
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
			public string WdbPath { get; set; }
			public List<string> GdbPaths { get; set; }
			public string SkbPath { get; set; }
			public string RrbPath { get; set; }
			public string OutputPath { get; set; }
		}

		private sealed class SceneExportSelection
		{
			public string InstallationPath { get; set; }
			public string PrimaryInputPath { get; set; }
			public string SceneName { get; set; }
			public string WdbPath { get; set; }
			public List<string> GdbPaths { get; private set; }
			public string SkbPath { get; set; }
			public string RrbPath { get; set; }
			public string OutputPath { get; set; }

			public SceneExportSelection()
			{
				GdbPaths = new List<string>();
			}
		}
	}
}

