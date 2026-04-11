using LR1Tools.Contracts;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LR1Tools.Export
{
	public static class TrackSceneJsonExporter
	{
		public const string SchemaId = "lr1tools.track-scene.v1";

		public static string ToJson(TrackScene p_scene)
		{
			TrackSceneJsonPackage package = CreatePackage(p_scene ?? new TrackScene());
			return JsonSerializer.Serialize(package, CreateOptions());
		}

		public static void ExportToFile(TrackScene p_scene, string p_outputPath)
		{
			string outputDirectory = Path.GetDirectoryName(p_outputPath);
			if (!string.IsNullOrEmpty(outputDirectory))
			{
				Directory.CreateDirectory(outputDirectory);
			}

			File.WriteAllText(p_outputPath, ToJson(p_scene));
		}

		private static JsonSerializerOptions CreateOptions()
		{
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.WriteIndented = true;
			options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
			return options;
		}

		private static TrackSceneJsonPackage CreatePackage(TrackScene p_scene)
		{
			TrackSceneJsonPackage package = new TrackSceneJsonPackage();
			package.Schema = SchemaId;
			package.Name = p_scene.Name ?? string.Empty;
			package.SourceName = p_scene.SourceName ?? string.Empty;
			package.Metadata = CloneMetadata(p_scene.Metadata);
			package.Materials = new List<TrackMaterialJsonDto>();
			package.Meshes = new List<TrackMeshJsonDto>();
			package.Objects = new List<TrackObjectJsonDto>();
			package.Paths = new List<TrackPathJsonDto>();
			package.Gradients = new List<TrackGradientJsonDto>();

			for (int i = 0; i < p_scene.Materials.Count; i++)
			{
				package.Materials.Add(CreateMaterial(p_scene.Materials[i]));
			}

			for (int i = 0; i < p_scene.Meshes.Count; i++)
			{
				package.Meshes.Add(CreateMesh(p_scene.Meshes[i]));
			}

			for (int i = 0; i < p_scene.Objects.Count; i++)
			{
				package.Objects.Add(CreateObject(p_scene.Objects[i]));
			}

			for (int i = 0; i < p_scene.Paths.Count; i++)
			{
				package.Paths.Add(CreatePath(p_scene.Paths[i]));
			}

			for (int i = 0; i < p_scene.Gradients.Count; i++)
			{
				package.Gradients.Add(CreateGradient(p_scene.Gradients[i]));
			}

			return package;
		}

		private static TrackMaterialJsonDto CreateMaterial(TrackMaterial p_material)
		{
			TrackMaterialJsonDto output = new TrackMaterialJsonDto();
			output.Name = p_material.Name ?? string.Empty;
			output.TextureName = p_material.TextureName ?? string.Empty;
			output.AlphaTextureName = p_material.AlphaTextureName ?? string.Empty;
			output.DiffuseColor = ToColorArray(p_material.DiffuseColor);
			output.Opacity = p_material.Opacity;
			output.DoubleSided = p_material.DoubleSided;
			output.Metadata = CloneMetadata(p_material.Metadata);
			output.Gradients = new List<TrackGradientJsonDto>();

			for (int i = 0; i < p_material.Gradients.Count; i++)
			{
				output.Gradients.Add(CreateGradient(p_material.Gradients[i]));
			}

			return output;
		}

		private static TrackMeshJsonDto CreateMesh(TrackMesh p_mesh)
		{
			TrackMeshJsonDto output = new TrackMeshJsonDto();
			output.Name = p_mesh.Name ?? string.Empty;
			output.MaterialName = p_mesh.MaterialName ?? string.Empty;
			output.IsCollisionMesh = p_mesh.IsCollisionMesh;
			output.Metadata = CloneMetadata(p_mesh.Metadata);
			output.Vertices = new List<TrackVertexJsonDto>();
			output.Indices = new List<int>();

			for (int i = 0; i < p_mesh.Vertices.Count; i++)
			{
				output.Vertices.Add(CreateVertex(p_mesh.Vertices[i]));
			}

			for (int i = 0; i < p_mesh.Indices.Count; i++)
			{
				output.Indices.Add(p_mesh.Indices[i]);
			}

			return output;
		}

		private static TrackVertexJsonDto CreateVertex(TrackVertex p_vertex)
		{
			TrackVertexJsonDto output = new TrackVertexJsonDto();
			output.Position = ToArray(p_vertex.Position);
			output.Normal = ToArray(p_vertex.Normal);
			output.PrimaryTexCoord = ToArray(p_vertex.PrimaryTexCoord);
			output.Color = ToColorArray(p_vertex.Color);
			output.Metadata = CloneMetadata(p_vertex.Metadata);
			return output;
		}

		private static TrackObjectJsonDto CreateObject(TrackObject p_object)
		{
			TrackObjectJsonDto output = new TrackObjectJsonDto();
			output.Name = p_object.Name ?? string.Empty;
			output.MeshName = p_object.MeshName ?? string.Empty;
			output.MaterialName = p_object.MaterialName ?? string.Empty;
			output.PathName = p_object.PathName ?? string.Empty;
			output.Visible = p_object.Visible;
			output.Transform = CreateTransform(p_object.Transform);
			output.Metadata = CloneMetadata(p_object.Metadata);
			return output;
		}

		private static TrackTransformJsonDto CreateTransform(TrackTransform p_transform)
		{
			TrackTransformJsonDto output = new TrackTransformJsonDto();
			output.Position = ToArray(p_transform != null ? p_transform.Position : Vector3.Zero);
			output.Rotation = ToArray(p_transform != null ? p_transform.Rotation : Quaternion.Identity);
			output.Scale = ToArray(p_transform != null ? p_transform.Scale : Vector3.One);
			return output;
		}

		private static TrackPathJsonDto CreatePath(TrackPath p_path)
		{
			TrackPathJsonDto output = new TrackPathJsonDto();
			output.Name = p_path.Name ?? string.Empty;
			output.Closed = p_path.Closed;
			output.Metadata = CloneMetadata(p_path.Metadata);
			output.Nodes = new List<TrackPathNodeJsonDto>();

			for (int i = 0; i < p_path.Nodes.Count; i++)
			{
				output.Nodes.Add(CreatePathNode(p_path.Nodes[i]));
			}

			return output;
		}

		private static TrackPathNodeJsonDto CreatePathNode(TrackPathNode p_node)
		{
			TrackPathNodeJsonDto output = new TrackPathNodeJsonDto();
			output.Position = ToArray(p_node.Position);
			output.Forward = ToArray(p_node.Forward);
			output.Up = ToArray(p_node.Up);
			output.Width = p_node.Width;
			output.Metadata = CloneMetadata(p_node.Metadata);
			return output;
		}

		private static TrackGradientJsonDto CreateGradient(TrackGradient p_gradient)
		{
			TrackGradientJsonDto output = new TrackGradientJsonDto();
			output.Name = p_gradient.Name ?? string.Empty;
			output.Metadata = CloneMetadata(p_gradient.Metadata);
			output.Stops = new List<TrackGradientStopJsonDto>();

			for (int i = 0; i < p_gradient.Stops.Count; i++)
			{
				output.Stops.Add(CreateGradientStop(p_gradient.Stops[i]));
			}

			return output;
		}

		private static TrackGradientStopJsonDto CreateGradientStop(TrackGradientStop p_stop)
		{
			TrackGradientStopJsonDto output = new TrackGradientStopJsonDto();
			output.Position = p_stop.Position;
			output.Color = ToColorArray(p_stop.Color);
			output.Metadata = CloneMetadata(p_stop.Metadata);
			return output;
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

		private static float[] ToColorArray(TrackColor p_value)
		{
			return new float[] { p_value.R, p_value.G, p_value.B, p_value.A };
		}
	}
}

