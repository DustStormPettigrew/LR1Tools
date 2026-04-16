using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LR1Tools.Export
{
	internal sealed class TrackSceneJsonPackage
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

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }

		[JsonPropertyName("materials")]
		public List<TrackMaterialJsonDto> Materials { get; set; }

		[JsonPropertyName("materialAnimations")]
		public List<TrackMaterialAnimationJsonDto> MaterialAnimations { get; set; }

		[JsonPropertyName("meshes")]
		public List<TrackMeshJsonDto> Meshes { get; set; }

		[JsonPropertyName("objects")]
		public List<TrackObjectJsonDto> Objects { get; set; }

		[JsonPropertyName("startPositions")]
		public List<TrackObjectJsonDto> StartPositions { get; set; }

		[JsonPropertyName("checkpoints")]
		public List<TrackObjectJsonDto> Checkpoints { get; set; }

		[JsonPropertyName("powerups")]
		public List<TrackObjectJsonDto> Powerups { get; set; }

		[JsonPropertyName("hazards")]
		public List<TrackObjectJsonDto> Hazards { get; set; }

		[JsonPropertyName("emitters")]
		public List<TrackObjectJsonDto> Emitters { get; set; }

		[JsonPropertyName("paths")]
		public List<TrackPathJsonDto> Paths { get; set; }

		[JsonPropertyName("npcPaths")]
		public List<TrackPathJsonDto> NpcPaths { get; set; }

		[JsonPropertyName("gradients")]
		public List<TrackGradientJsonDto> Gradients { get; set; }
	}

	internal sealed class TrackCoordinateSystemJsonDto
	{
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
	}

	internal sealed class TrackMaterialJsonDto
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

		[JsonPropertyName("textureName")]
		public string TextureName { get; set; }

		[JsonPropertyName("alphaTextureName")]
		public string AlphaTextureName { get; set; }

		[JsonPropertyName("diffuseColor")]
		public float[] DiffuseColor { get; set; }

		[JsonPropertyName("opacity")]
		public float Opacity { get; set; }

		[JsonPropertyName("doubleSided")]
		public bool DoubleSided { get; set; }

		[JsonPropertyName("materialAnimationIds")]
		public List<string> MaterialAnimationIds { get; set; }

		[JsonPropertyName("gradients")]
		public List<TrackGradientJsonDto> Gradients { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackMeshJsonDto
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

		[JsonPropertyName("materialName")]
		public string MaterialName { get; set; }

		[JsonPropertyName("isCollisionMesh")]
		public bool IsCollisionMesh { get; set; }

		[JsonPropertyName("vertices")]
		public List<TrackVertexJsonDto> Vertices { get; set; }

		[JsonPropertyName("indices")]
		public List<int> Indices { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackVertexJsonDto
	{
		[JsonPropertyName("position")]
		public float[] Position { get; set; }

		[JsonPropertyName("normal")]
		public float[] Normal { get; set; }

		[JsonPropertyName("primaryTexCoord")]
		public float[] PrimaryTexCoord { get; set; }

		[JsonPropertyName("color")]
		public float[] Color { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackObjectJsonDto
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

		[JsonPropertyName("meshName")]
		public string MeshName { get; set; }

		[JsonPropertyName("materialName")]
		public string MaterialName { get; set; }

		[JsonPropertyName("pathName")]
		public string PathName { get; set; }

		[JsonPropertyName("visible")]
		public bool Visible { get; set; }

		[JsonPropertyName("transform")]
		public TrackTransformJsonDto Transform { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackTransformJsonDto
	{
		[JsonPropertyName("position")]
		public float[] Position { get; set; }

		[JsonPropertyName("rotation")]
		public float[] Rotation { get; set; }

		[JsonPropertyName("scale")]
		public float[] Scale { get; set; }
	}

	internal sealed class TrackPathJsonDto
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

		[JsonPropertyName("closed")]
		public bool Closed { get; set; }

		[JsonPropertyName("nodes")]
		public List<TrackPathNodeJsonDto> Nodes { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackPathNodeJsonDto
	{
		[JsonPropertyName("position")]
		public float[] Position { get; set; }

		[JsonPropertyName("forward")]
		public float[] Forward { get; set; }

		[JsonPropertyName("up")]
		public float[] Up { get; set; }

		[JsonPropertyName("width")]
		public float Width { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackGradientJsonDto
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

		[JsonPropertyName("stops")]
		public List<TrackGradientStopJsonDto> Stops { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackGradientStopJsonDto
	{
		[JsonPropertyName("position")]
		public float Position { get; set; }

		[JsonPropertyName("color")]
		public float[] Color { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackMaterialAnimationJsonDto
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

		[JsonPropertyName("materialName")]
		public string MaterialName { get; set; }

		[JsonPropertyName("behavior")]
		public string Behavior { get; set; }

		[JsonPropertyName("loopMode")]
		public string LoopMode { get; set; }

		[JsonPropertyName("frameCount")]
		public int? FrameCount { get; set; }

		[JsonPropertyName("speed")]
		public float Speed { get; set; }

		[JsonPropertyName("uvOffset")]
		public float[] UvOffset { get; set; }

		[JsonPropertyName("uvVelocity")]
		public float[] UvVelocity { get; set; }

		[JsonPropertyName("frames")]
		public List<TrackMaterialAnimationFrameJsonDto> Frames { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackMaterialAnimationFrameJsonDto
	{
		[JsonPropertyName("materialName")]
		public string MaterialName { get; set; }

		[JsonPropertyName("frameIndex")]
		public int FrameIndex { get; set; }

		[JsonPropertyName("uvOffset")]
		public float[] UvOffset { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}
}

