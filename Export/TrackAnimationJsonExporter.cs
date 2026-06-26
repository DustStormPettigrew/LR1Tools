using LR1Tools.Contracts;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LR1Tools.Export
{
	public static class TrackAnimationJsonExporter
	{
		public const string SchemaId = "lr1tools.animation.v1";

		public static string ToJson(TrackAnimationPackage p_package)
		{
			TrackAnimationJsonPackage package = CreatePackage(p_package ?? new TrackAnimationPackage());
			return JsonSerializer.Serialize(package, CreateOptions());
		}

		public static void ExportToFile(TrackAnimationPackage p_package, string p_outputPath)
		{
			string outputDirectory = Path.GetDirectoryName(p_outputPath);
			if (!string.IsNullOrEmpty(outputDirectory))
			{
				Directory.CreateDirectory(outputDirectory);
			}

			File.WriteAllText(p_outputPath, ToJson(p_package));
		}

		private static JsonSerializerOptions CreateOptions()
		{
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.WriteIndented = true;
			options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
			return options;
		}

		private static TrackAnimationJsonPackage CreatePackage(TrackAnimationPackage p_package)
		{
			TrackCoordinateSystem coordinateSystem = p_package.CoordinateSystem ?? new TrackCoordinateSystem();
			TrackAnimationJsonPackage output = new TrackAnimationJsonPackage();
			output.Schema = SchemaId;
			output.Id = GetId(p_package.Id, p_package.Name);
			output.SourceId = GetStringValue(p_package.SourceId, p_package.Metadata, "SourceId");
			output.ExportType = string.IsNullOrEmpty(p_package.ExportType) ? TrackSceneExportTypes.AnimationSet : p_package.ExportType;
			output.Name = p_package.Name ?? string.Empty;
			output.SourceName = GetStringValue(p_package.SourceName, p_package.Metadata, "SourceName");
			output.SourceFormat = GetStringValue(p_package.SourceFormat, p_package.Metadata, "SourceFormat");
			output.SourcePath = GetStringValue(p_package.SourcePath, p_package.Metadata, "SourcePath", "Export.Input.Primary");
			output.SourceIndex = GetNullableIntValue(p_package.SourceIndex, p_package.Metadata, "SourceIndex");
			output.CoordinateSystem = CreateCoordinateSystem(coordinateSystem);
			output.Handedness = coordinateSystem.Handedness ?? string.Empty;
			output.RightAxis = coordinateSystem.RightAxis ?? string.Empty;
			output.UpAxis = coordinateSystem.UpAxis ?? string.Empty;
			output.ForwardAxis = coordinateSystem.ForwardAxis ?? string.Empty;
			output.Units = coordinateSystem.Units ?? string.Empty;
			output.Metadata = CloneMetadata(p_package.Metadata);
			output.Clips = new List<TrackAnimationClipJsonDto>();

			for (int i = 0; i < p_package.Clips.Count; i++)
			{
				output.Clips.Add(CreateClip(p_package.Clips[i]));
			}

			return output;
		}

		private static TrackCoordinateSystemJsonDto CreateCoordinateSystem(TrackCoordinateSystem p_coordinateSystem)
		{
			TrackCoordinateSystem source = p_coordinateSystem ?? new TrackCoordinateSystem();
			TrackCoordinateSystemJsonDto output = new TrackCoordinateSystemJsonDto();
			output.Handedness = source.Handedness ?? string.Empty;
			output.RightAxis = source.RightAxis ?? string.Empty;
			output.UpAxis = source.UpAxis ?? string.Empty;
			output.ForwardAxis = source.ForwardAxis ?? string.Empty;
			output.Units = source.Units ?? string.Empty;
			return output;
		}

		private static TrackAnimationClipJsonDto CreateClip(TrackAnimationClip p_clip)
		{
			TrackAnimationClip source = p_clip ?? new TrackAnimationClip();
			TrackAnimationClipJsonDto output = new TrackAnimationClipJsonDto();
			output.Id = GetId(source.Id, source.Name);
			output.SourceId = GetStringValue(source.SourceId, source.Metadata, "SourceId");
			output.Name = source.Name ?? string.Empty;
			output.SourceName = GetStringValue(source.SourceName, source.Metadata, "SourceName");
			output.SourceFormat = GetStringValue(source.SourceFormat, source.Metadata, "SourceFormat");
			output.SourcePath = GetStringValue(source.SourcePath, source.Metadata, "SourcePath");
			output.SourceIndex = GetNullableIntValue(source.SourceIndex, source.Metadata, "SourceIndex");
			output.LoopMode = source.LoopMode ?? string.Empty;
			output.FrameCount = source.FrameCount;
			output.Speed = source.Speed;
			output.Metadata = CloneMetadata(source.Metadata);
			output.Channels = new List<TrackAnimationChannelJsonDto>();

			for (int i = 0; i < source.Channels.Count; i++)
			{
				output.Channels.Add(CreateChannel(source.Channels[i]));
			}

			return output;
		}

		private static TrackAnimationChannelJsonDto CreateChannel(TrackAnimationChannel p_channel)
		{
			TrackAnimationChannel source = p_channel ?? new TrackAnimationChannel();
			TrackAnimationChannelJsonDto output = new TrackAnimationChannelJsonDto();
			output.Id = GetId(source.Id, source.Name);
			output.SourceId = GetStringValue(source.SourceId, source.Metadata, "SourceId");
			output.Name = source.Name ?? string.Empty;
			output.SourceName = GetStringValue(source.SourceName, source.Metadata, "SourceName");
			output.SourceFormat = GetStringValue(source.SourceFormat, source.Metadata, "SourceFormat");
			output.SourcePath = GetStringValue(source.SourcePath, source.Metadata, "SourcePath");
			output.SourceIndex = GetNullableIntValue(source.SourceIndex, source.Metadata, "SourceIndex");
			output.Property = source.Property ?? string.Empty;
			output.ValueType = source.ValueType ?? string.Empty;
			output.Interpolation = source.Interpolation ?? string.Empty;
			output.Target = CreateTarget(source.Target);
			output.Metadata = CloneMetadata(source.Metadata);
			output.Keyframes = new List<TrackAnimationKeyframeJsonDto>();

			for (int i = 0; i < source.Keyframes.Count; i++)
			{
				output.Keyframes.Add(CreateKeyframe(source.Keyframes[i]));
			}

			return output;
		}

		private static TrackAnimationTargetJsonDto CreateTarget(TrackAnimationTarget p_target)
		{
			TrackAnimationTarget source = p_target ?? new TrackAnimationTarget();
			TrackAnimationTargetJsonDto output = new TrackAnimationTargetJsonDto();
			output.Id = GetId(source.Id, source.Name);
			output.SourceId = GetStringValue(source.SourceId, source.Metadata, "SourceId");
			output.Name = source.Name ?? string.Empty;
			output.SourceName = GetStringValue(source.SourceName, source.Metadata, "SourceName");
			output.SourceFormat = GetStringValue(source.SourceFormat, source.Metadata, "SourceFormat");
			output.SourcePath = GetStringValue(source.SourcePath, source.Metadata, "SourcePath");
			output.SourceIndex = GetNullableIntValue(source.SourceIndex, source.Metadata, "SourceIndex");
			output.Type = source.Type ?? string.Empty;
			output.Path = source.Path ?? string.Empty;
			output.Slot = source.Slot ?? string.Empty;
			output.Metadata = CloneMetadata(source.Metadata);
			return output;
		}

		private static TrackAnimationKeyframeJsonDto CreateKeyframe(TrackAnimationKeyframe p_keyframe)
		{
			TrackAnimationKeyframe source = p_keyframe ?? new TrackAnimationKeyframe();
			TrackAnimationKeyframeJsonDto output = new TrackAnimationKeyframeJsonDto();
			output.SourceName = GetStringValue(source.SourceName, source.Metadata, "SourceName");
			output.SourceFormat = GetStringValue(source.SourceFormat, source.Metadata, "SourceFormat");
			output.SourcePath = GetStringValue(source.SourcePath, source.Metadata, "SourcePath");
			output.SourceIndex = GetNullableIntValue(source.SourceIndex, source.Metadata, "SourceIndex");
			output.FrameIndex = source.FrameIndex;
			output.Time = source.Time;
			output.IntValue = source.IntValue;
			output.FloatValue = source.FloatValue;
			output.StringValue = source.HasStringValue ? source.StringValue : null;
			output.Vector2Value = source.HasVector2Value ? ToArray(source.Vector2Value) : null;
			output.Vector3Value = source.HasVector3Value ? ToArray(source.Vector3Value) : null;
			output.QuaternionValue = source.HasQuaternionValue ? ToArray(source.QuaternionValue) : null;
			output.Metadata = CloneMetadata(source.Metadata);
			return output;
		}

		private static string GetId(string p_preferredId, string p_fallbackName)
		{
			if (!string.IsNullOrWhiteSpace(p_preferredId))
			{
				return p_preferredId;
			}

			return string.IsNullOrWhiteSpace(p_fallbackName) ? null : p_fallbackName;
		}

		private static string GetStringValue(string p_preferredValue, IDictionary<string, string> p_metadata, params string[] p_metadataKeys)
		{
			if (!string.IsNullOrWhiteSpace(p_preferredValue))
			{
				return p_preferredValue;
			}

			if (p_metadata == null || p_metadataKeys == null)
			{
				return null;
			}

			for (int i = 0; i < p_metadataKeys.Length; i++)
			{
				string value;
				if (p_metadata.TryGetValue(p_metadataKeys[i], out value) && !string.IsNullOrWhiteSpace(value))
				{
					return value;
				}
			}

			return null;
		}

		private static int? GetNullableIntValue(int? p_preferredValue, IDictionary<string, string> p_metadata, params string[] p_metadataKeys)
		{
			if (p_preferredValue.HasValue)
			{
				return p_preferredValue.Value;
			}

			if (p_metadata == null || p_metadataKeys == null)
			{
				return null;
			}

			for (int i = 0; i < p_metadataKeys.Length; i++)
			{
				string value;
				int parsedValue;
				if (p_metadata.TryGetValue(p_metadataKeys[i], out value) &&
					int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue))
				{
					return parsedValue;
				}
			}

			return null;
		}

		private static Dictionary<string, string> CloneMetadata(Dictionary<string, string> p_metadata)
		{
			Dictionary<string, string> output = new Dictionary<string, string>();
			if (p_metadata == null)
			{
				return output;
			}

			foreach (KeyValuePair<string, string> pair in p_metadata)
			{
				output[pair.Key] = pair.Value;
			}

			return output;
		}

		private static float[] ToArray(Vector2 p_value)
		{
			return new float[] { p_value.X, p_value.Y };
		}

		private static float[] ToArray(Vector3 p_value)
		{
			return new float[] { p_value.X, p_value.Y, p_value.Z };
		}

		private static float[] ToArray(Quaternion p_value)
		{
			return new float[] { p_value.X, p_value.Y, p_value.Z, p_value.W };
		}

	}
}
