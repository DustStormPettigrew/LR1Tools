using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LR1Tools.Export
{
	internal sealed class TrackAnimationJsonPackage
	{
		[JsonPropertyName("schema")]
		public string Schema { get; set; }

		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("sourceId")]
		public string SourceId { get; set; }

		[JsonPropertyName("exportType")]
		public string ExportType { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("sourceName")]
		public string SourceName { get; set; }

		[JsonPropertyName("sourceFormat")]
		public string SourceFormat { get; set; }

		[JsonPropertyName("sourcePath")]
		public string SourcePath { get; set; }

		[JsonPropertyName("sourceIndex")]
		public int? SourceIndex { get; set; }

		[JsonPropertyName("coordinateSystem")]
		public TrackCoordinateSystemJsonDto CoordinateSystem { get; set; }

		[JsonPropertyName("handedness")]
		public string Handedness { get; set; }

		[JsonPropertyName("rightAxis")]
		public string RightAxis { get; set; }

		[JsonPropertyName("upAxis")]
		public string UpAxis { get; set; }

		[JsonPropertyName("forwardAxis")]
		public string ForwardAxis { get; set; }

		[JsonPropertyName("units")]
		public string Units { get; set; }

		[JsonPropertyName("clips")]
		public List<TrackAnimationClipJsonDto> Clips { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackAnimationClipJsonDto
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("sourceId")]
		public string SourceId { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("sourceName")]
		public string SourceName { get; set; }

		[JsonPropertyName("sourceFormat")]
		public string SourceFormat { get; set; }

		[JsonPropertyName("sourcePath")]
		public string SourcePath { get; set; }

		[JsonPropertyName("sourceIndex")]
		public int? SourceIndex { get; set; }

		[JsonPropertyName("loopMode")]
		public string LoopMode { get; set; }

		[JsonPropertyName("frameCount")]
		public int? FrameCount { get; set; }

		[JsonPropertyName("speed")]
		public float Speed { get; set; }

		[JsonPropertyName("channels")]
		public List<TrackAnimationChannelJsonDto> Channels { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackAnimationChannelJsonDto
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("sourceId")]
		public string SourceId { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("sourceName")]
		public string SourceName { get; set; }

		[JsonPropertyName("sourceFormat")]
		public string SourceFormat { get; set; }

		[JsonPropertyName("sourcePath")]
		public string SourcePath { get; set; }

		[JsonPropertyName("sourceIndex")]
		public int? SourceIndex { get; set; }

		[JsonPropertyName("property")]
		public string Property { get; set; }

		[JsonPropertyName("valueType")]
		public string ValueType { get; set; }

		[JsonPropertyName("interpolation")]
		public string Interpolation { get; set; }

		[JsonPropertyName("target")]
		public TrackAnimationTargetJsonDto Target { get; set; }

		[JsonPropertyName("keyframes")]
		public List<TrackAnimationKeyframeJsonDto> Keyframes { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackAnimationTargetJsonDto
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("sourceId")]
		public string SourceId { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("sourceName")]
		public string SourceName { get; set; }

		[JsonPropertyName("sourceFormat")]
		public string SourceFormat { get; set; }

		[JsonPropertyName("sourcePath")]
		public string SourcePath { get; set; }

		[JsonPropertyName("sourceIndex")]
		public int? SourceIndex { get; set; }

		[JsonPropertyName("type")]
		public string Type { get; set; }

		[JsonPropertyName("path")]
		public string Path { get; set; }

		[JsonPropertyName("slot")]
		public string Slot { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackAnimationKeyframeJsonDto
	{
		[JsonPropertyName("sourceName")]
		public string SourceName { get; set; }

		[JsonPropertyName("sourceFormat")]
		public string SourceFormat { get; set; }

		[JsonPropertyName("sourcePath")]
		public string SourcePath { get; set; }

		[JsonPropertyName("sourceIndex")]
		public int? SourceIndex { get; set; }

		[JsonPropertyName("frameIndex")]
		public int? FrameIndex { get; set; }

		[JsonPropertyName("time")]
		public float? Time { get; set; }

		[JsonPropertyName("intValue")]
		public int? IntValue { get; set; }

		[JsonPropertyName("floatValue")]
		public float? FloatValue { get; set; }

		[JsonPropertyName("stringValue")]
		public string StringValue { get; set; }

		[JsonPropertyName("vector2Value")]
		public float[] Vector2Value { get; set; }

		[JsonPropertyName("vector3Value")]
		public float[] Vector3Value { get; set; }

		[JsonPropertyName("quaternionValue")]
		public float[] QuaternionValue { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}
}
