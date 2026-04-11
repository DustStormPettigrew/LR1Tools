using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LR1Tools.Export
{
	internal sealed class TrackSceneJsonPackage
	{
		[JsonPropertyName("schema")]
		public string Schema { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("sourceName")]
		public string SourceName { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }

		[JsonPropertyName("materials")]
		public List<TrackMaterialJsonDto> Materials { get; set; }

		[JsonPropertyName("meshes")]
		public List<TrackMeshJsonDto> Meshes { get; set; }

		[JsonPropertyName("objects")]
		public List<TrackObjectJsonDto> Objects { get; set; }

		[JsonPropertyName("paths")]
		public List<TrackPathJsonDto> Paths { get; set; }

		[JsonPropertyName("gradients")]
		public List<TrackGradientJsonDto> Gradients { get; set; }
	}

	internal sealed class TrackMaterialJsonDto
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

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

		[JsonPropertyName("gradients")]
		public List<TrackGradientJsonDto> Gradients { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, string> Metadata { get; set; }
	}

	internal sealed class TrackMeshJsonDto
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

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
		[JsonPropertyName("name")]
		public string Name { get; set; }

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
		[JsonPropertyName("name")]
		public string Name { get; set; }

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
		[JsonPropertyName("name")]
		public string Name { get; set; }

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
}

