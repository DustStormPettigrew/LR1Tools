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
	internal static class AnimationExportRunner
	{
		public static void Run(string[] p_args)
		{
			AnimationExportRequest request = ParseRequest(p_args);
			TrackAnimationPackage package = BuildPackage(request);

			LogSummary(request, package);
			TrackAnimationJsonExporter.ExportToFile(package, request.OutputPath);

			Console.WriteLine("Exported animation package JSON:");
			Console.WriteLine(request.OutputPath);
		}

		private static AnimationExportRequest ParseRequest(string[] p_args)
		{
			if (p_args == null || p_args.Length != 3)
			{
				throw new InvalidOperationException("Usage: export-animation <input.MAB|input.ADB> <output.json>");
			}

			string inputPath = ResolveInputPath(p_args[1]);
			if (string.IsNullOrEmpty(inputPath))
			{
				throw new FileNotFoundException("Animation export input could not be resolved: " + p_args[1]);
			}

			string outputPath = Path.GetFullPath(p_args[2]);
			if (!string.Equals(Path.GetExtension(outputPath), ".json", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Animation export output must be a .json path.");
			}

			string sourceFormat = Path.GetExtension(inputPath).TrimStart('.').ToUpperInvariant();
			if (!string.Equals(sourceFormat, "MAB", StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(sourceFormat, "ADB", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Unsupported animation export input type: " + inputPath);
			}

			AnimationExportRequest request = new AnimationExportRequest();
			request.InputPath = inputPath;
			request.OutputPath = outputPath;
			request.SourceFormat = sourceFormat;
			return request;
		}

		private static TrackAnimationPackage BuildPackage(AnimationExportRequest p_request)
		{
			string packageName = Path.GetFileNameWithoutExtension(p_request.InputPath);
			TrackAnimationPackage package = new TrackAnimationPackage();
			package.Name = packageName;
			package.Id = packageName;
			package.SourceId = packageName;
			package.SourceName = packageName;
			package.SourceFormat = p_request.SourceFormat;
			package.SourcePath = p_request.InputPath;
			package.ExportType = TrackSceneExportTypes.AnimationSet;
			package.Metadata["SourceFormat"] = package.SourceFormat;
			package.Metadata["Export.Input.Primary"] = package.SourcePath;

			if (string.Equals(p_request.SourceFormat, "MAB", StringComparison.OrdinalIgnoreCase))
			{
				List<TrackAnimationClip> clips = MABAdapter.ToAnimationClips(new MAB(p_request.InputPath), packageName);
				ApplyClipsSource(clips, p_request.InputPath, "MAB");
				AddClips(package.Clips, clips);
			}
			else if (string.Equals(p_request.SourceFormat, "ADB", StringComparison.OrdinalIgnoreCase))
			{
				List<TrackAnimationClip> clips = ADBAdapter.ToAnimationClips(new ADB(p_request.InputPath), packageName);
				ApplyClipsSource(clips, p_request.InputPath, "ADB");
				AddClips(package.Clips, clips);
			}

			package.Metadata["Export.Summary.ClipCount"] = package.Clips.Count.ToString(CultureInfo.InvariantCulture);
			package.Metadata["Export.Summary.ChannelCount"] = CountChannels(package).ToString(CultureInfo.InvariantCulture);
			package.Metadata["Export.Summary.RecordCount"] = CountRecords(package).ToString(CultureInfo.InvariantCulture);
			return package;
		}

		private static void AddClips(IList<TrackAnimationClip> p_target, IList<TrackAnimationClip> p_source)
		{
			if (p_source == null)
			{
				return;
			}

			HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < p_target.Count; i++)
			{
				string key = GetNameKey(p_target[i] != null ? (!string.IsNullOrWhiteSpace(p_target[i].Id) ? p_target[i].Id : p_target[i].Name) : null);
				if (!string.IsNullOrEmpty(key))
				{
					ids.Add(key);
				}
			}

			for (int i = 0; i < p_source.Count; i++)
			{
				TrackAnimationClip clip = p_source[i];
				string key = GetNameKey(clip != null ? (!string.IsNullOrWhiteSpace(clip.Id) ? clip.Id : clip.Name) : null);
				if (string.IsNullOrEmpty(key) || ids.Add(key))
				{
					p_target.Add(clip);
				}
			}
		}

		private static void ApplyClipsSource(IList<TrackAnimationClip> p_clips, string p_sourcePath, string p_sourceFormat)
		{
			if (p_clips == null)
			{
				return;
			}

			for (int i = 0; i < p_clips.Count; i++)
			{
				TrackAnimationClip clip = p_clips[i];
				if (clip == null)
				{
					continue;
				}

				if (string.IsNullOrEmpty(clip.Id))
				{
					clip.Id = clip.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(clip.SourceName))
				{
					clip.SourceName = clip.Name ?? string.Empty;
				}

				if (string.IsNullOrEmpty(clip.SourceFormat))
				{
					clip.SourceFormat = p_sourceFormat ?? string.Empty;
				}

				if (string.IsNullOrEmpty(clip.SourcePath))
				{
					clip.SourcePath = p_sourcePath ?? string.Empty;
				}

				for (int channelIndex = 0; channelIndex < clip.Channels.Count; channelIndex++)
				{
					TrackAnimationChannel channel = clip.Channels[channelIndex];
					if (channel == null)
					{
						continue;
					}

					if (string.IsNullOrEmpty(channel.Id))
					{
						channel.Id = channel.Name ?? string.Empty;
					}

					if (string.IsNullOrEmpty(channel.SourceName))
					{
						channel.SourceName = channel.Name ?? string.Empty;
					}

					if (string.IsNullOrEmpty(channel.SourceFormat))
					{
						channel.SourceFormat = clip.SourceFormat;
					}

					if (string.IsNullOrEmpty(channel.SourcePath))
					{
						channel.SourcePath = clip.SourcePath;
					}

					TrackAnimationTarget target = channel.Target;
					if (target != null)
					{
						if (string.IsNullOrEmpty(target.Id))
						{
							target.Id = !string.IsNullOrWhiteSpace(target.Name) ? target.Name : channel.Id + ":Target";
						}

						if (string.IsNullOrEmpty(target.SourceName))
						{
							target.SourceName = !string.IsNullOrWhiteSpace(target.Name) ? target.Name : channel.SourceName;
						}

						if (string.IsNullOrEmpty(target.SourceFormat))
						{
							target.SourceFormat = channel.SourceFormat;
						}

						if (string.IsNullOrEmpty(target.SourcePath))
						{
							target.SourcePath = channel.SourcePath;
						}
					}

					for (int keyframeIndex = 0; keyframeIndex < channel.Keyframes.Count; keyframeIndex++)
					{
						TrackAnimationKeyframe keyframe = channel.Keyframes[keyframeIndex];
						if (keyframe == null)
						{
							continue;
						}

						if (string.IsNullOrEmpty(keyframe.SourceName))
						{
							keyframe.SourceName = channel.SourceName;
						}

						if (string.IsNullOrEmpty(keyframe.SourceFormat))
						{
							keyframe.SourceFormat = channel.SourceFormat;
						}

						if (string.IsNullOrEmpty(keyframe.SourcePath))
						{
							keyframe.SourcePath = channel.SourcePath;
						}
					}
				}
			}
		}

		private static void LogSummary(AnimationExportRequest p_request, TrackAnimationPackage p_package)
		{
			int clipCount = p_package != null ? p_package.Clips.Count : 0;
			int channelCount = CountChannels(p_package);
			int recordCount = CountRecords(p_package);
			List<string> unknowns = CollectUnknownOrUnsupportedSections(p_package);

			Console.WriteLine("Animation export:");
			Console.WriteLine("Input file: " + p_request.InputPath);
			Console.WriteLine("Detected source format: " + p_request.SourceFormat);
			Console.WriteLine("Schema: " + TrackAnimationJsonExporter.SchemaId);
			Console.WriteLine("Export type: " + TrackSceneExportTypes.AnimationSet);
			Console.WriteLine("Clips exported: " + clipCount.ToString(CultureInfo.InvariantCulture));
			Console.WriteLine("Channels exported: " + channelCount.ToString(CultureInfo.InvariantCulture));
			Console.WriteLine("Records exported: " + recordCount.ToString(CultureInfo.InvariantCulture));

			if (unknowns.Count == 0)
			{
				Console.WriteLine("Unsupported or unknown sections: <none>");
				return;
			}

			Console.WriteLine("Unsupported or unknown sections:");
			for (int i = 0; i < unknowns.Count; i++)
			{
				Console.WriteLine("  [" + i.ToString(CultureInfo.InvariantCulture) + "] " + unknowns[i]);
			}
		}

		private static int CountChannels(TrackAnimationPackage p_package)
		{
			if (p_package == null)
			{
				return 0;
			}

			int count = 0;
			for (int clipIndex = 0; clipIndex < p_package.Clips.Count; clipIndex++)
			{
				TrackAnimationClip clip = p_package.Clips[clipIndex];
				if (clip != null)
				{
					count += clip.Channels.Count;
				}
			}

			return count;
		}

		private static int CountRecords(TrackAnimationPackage p_package)
		{
			if (p_package == null)
			{
				return 0;
			}

			int count = 0;
			for (int clipIndex = 0; clipIndex < p_package.Clips.Count; clipIndex++)
			{
				TrackAnimationClip clip = p_package.Clips[clipIndex];
				if (clip == null)
				{
					continue;
				}

				for (int channelIndex = 0; channelIndex < clip.Channels.Count; channelIndex++)
				{
					TrackAnimationChannel channel = clip.Channels[channelIndex];
					if (channel != null)
					{
						count += channel.Keyframes.Count;
					}
				}
			}

			return count;
		}

		private static List<string> CollectUnknownOrUnsupportedSections(TrackAnimationPackage p_package)
		{
			List<string> messages = new List<string>();
			if (p_package == null)
			{
				return messages;
			}

			CollectMetadataMessages(messages, "Package", p_package.Metadata);
			for (int clipIndex = 0; clipIndex < p_package.Clips.Count; clipIndex++)
			{
				TrackAnimationClip clip = p_package.Clips[clipIndex];
				if (clip == null)
				{
					messages.Add("Clip[" + clipIndex.ToString(CultureInfo.InvariantCulture) + "]: null clip record");
					continue;
				}

				if (string.Equals(clip.LoopMode, "Unknown", StringComparison.OrdinalIgnoreCase))
				{
					messages.Add("Clip[" + clipIndex.ToString(CultureInfo.InvariantCulture) + "] " + (clip.Name ?? "<unnamed>") + ": loop mode unknown");
				}

				CollectMetadataMessages(messages, "Clip[" + clipIndex.ToString(CultureInfo.InvariantCulture) + "] " + (clip.Name ?? "<unnamed>"), clip.Metadata);

				for (int channelIndex = 0; channelIndex < clip.Channels.Count; channelIndex++)
				{
					TrackAnimationChannel channel = clip.Channels[channelIndex];
					if (channel == null)
					{
						messages.Add("Clip[" + clipIndex.ToString(CultureInfo.InvariantCulture) + "].Channel[" + channelIndex.ToString(CultureInfo.InvariantCulture) + "]: null channel record");
						continue;
					}

					if (string.Equals(channel.Interpolation, "Unknown", StringComparison.OrdinalIgnoreCase))
					{
						messages.Add("Clip[" + clipIndex.ToString(CultureInfo.InvariantCulture) + "].Channel[" + channelIndex.ToString(CultureInfo.InvariantCulture) + "] " + (channel.Name ?? "<unnamed>") + ": interpolation unknown");
					}

					CollectMetadataMessages(messages, "Clip[" + clipIndex.ToString(CultureInfo.InvariantCulture) + "].Channel[" + channelIndex.ToString(CultureInfo.InvariantCulture) + "] " + (channel.Name ?? "<unnamed>"), channel.Metadata);
				}
			}

			return messages;
		}

		private static void CollectMetadataMessages(List<string> p_messages, string p_scope, IDictionary<string, string> p_metadata)
		{
			if (p_metadata == null)
			{
				return;
			}

			foreach (KeyValuePair<string, string> pair in p_metadata)
			{
				if (IsUnknownOrUnsupportedKey(pair.Key))
				{
					p_messages.Add(p_scope + ": " + pair.Key + "=" + pair.Value);
				}
			}
		}

		private static bool IsUnknownOrUnsupportedKey(string p_key)
		{
			if (string.IsNullOrWhiteSpace(p_key))
			{
				return false;
			}

			return p_key.IndexOf("Unknown", StringComparison.OrdinalIgnoreCase) >= 0 ||
				p_key.IndexOf("Unsupported", StringComparison.OrdinalIgnoreCase) >= 0 ||
				p_key.IndexOf("Missing", StringComparison.OrdinalIgnoreCase) >= 0 ||
				p_key.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
				p_key.IndexOf("Variable", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static string ResolveInputPath(string p_inputPath)
		{
			if (string.IsNullOrWhiteSpace(p_inputPath))
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
			return !string.IsNullOrEmpty(repoCandidate) && File.Exists(repoCandidate) ? repoCandidate : null;
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

		private static string GetNameKey(string p_value)
		{
			return string.IsNullOrWhiteSpace(p_value) ? null : p_value.Trim();
		}

		private sealed class AnimationExportRequest
		{
			public string InputPath { get; set; }
			public string OutputPath { get; set; }
			public string SourceFormat { get; set; }
		}
	}
}
