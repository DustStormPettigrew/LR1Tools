using LR1Tools.Contracts;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LR1Tools.Export
{
	public static class TrackSceneJsonExporter
	{
		public const string SchemaId = "lr1tools.track-scene.v2";

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
			TrackCoordinateSystem coordinateSystem = p_scene.CoordinateSystem ?? new TrackCoordinateSystem();
			TrackSceneJsonPackage package = new TrackSceneJsonPackage();
			package.Schema = SchemaId;
			package.Id = GetId(p_scene.Id, p_scene.Name);
			package.SourceId = GetStringValue(p_scene.SourceId, p_scene.Metadata, "SourceId");
			package.ExportType = string.IsNullOrEmpty(p_scene.ExportType) ? TrackSceneExportTypes.Scene : p_scene.ExportType;
			package.Name = p_scene.Name ?? string.Empty;
			package.SourceName = GetStringValue(p_scene.SourceName, p_scene.Metadata, "SourceName");
			package.SourceFormat = GetStringValue(p_scene.SourceFormat, p_scene.Metadata, "SourceFormat");
			package.SourcePath = GetStringValue(p_scene.SourcePath, p_scene.Metadata, "SourcePath", "Export.Input.Primary");
			package.SourceIndex = GetNullableIntValue(p_scene.SourceIndex, p_scene.Metadata, "SourceIndex");
			package.CoordinateSystem = CreateCoordinateSystem(coordinateSystem);
			package.Handedness = coordinateSystem.Handedness ?? string.Empty;
			package.RightAxis = coordinateSystem.RightAxis ?? string.Empty;
			package.UpAxis = coordinateSystem.UpAxis ?? string.Empty;
			package.ForwardAxis = coordinateSystem.ForwardAxis ?? string.Empty;
			package.Units = coordinateSystem.Units ?? string.Empty;
			package.Metadata = CloneMetadata(p_scene.Metadata);
			package.Materials = new List<TrackMaterialJsonDto>();
			package.MaterialAnimations = new List<TrackMaterialAnimationJsonDto>();
			package.Meshes = new List<TrackMeshJsonDto>();
			package.Objects = new List<TrackObjectJsonDto>();
			package.StartPositions = new List<TrackObjectJsonDto>();
			package.Checkpoints = new List<TrackObjectJsonDto>();
			package.Powerups = new List<TrackObjectJsonDto>();
			package.Hazards = new List<TrackObjectJsonDto>();
			package.Emitters = new List<TrackObjectJsonDto>();
			package.Paths = new List<TrackPathJsonDto>();
			package.NpcPaths = new List<TrackPathJsonDto>();
			package.Gradients = new List<TrackGradientJsonDto>();

			for (int i = 0; i < p_scene.Materials.Count; i++)
			{
				package.Materials.Add(CreateMaterial(p_scene.Materials[i]));
			}

			for (int i = 0; i < p_scene.MaterialAnimations.Count; i++)
			{
				package.MaterialAnimations.Add(CreateMaterialAnimation(p_scene.MaterialAnimations[i]));
			}

			for (int i = 0; i < p_scene.Meshes.Count; i++)
			{
				package.Meshes.Add(CreateMesh(p_scene.Meshes[i]));
			}

			for (int i = 0; i < p_scene.Objects.Count; i++)
			{
				package.Objects.Add(CreateObject(p_scene.Objects[i]));
			}

			for (int i = 0; i < p_scene.StartPositions.Count; i++)
			{
				package.StartPositions.Add(CreateObject(p_scene.StartPositions[i]));
			}

			for (int i = 0; i < p_scene.Checkpoints.Count; i++)
			{
				package.Checkpoints.Add(CreateObject(p_scene.Checkpoints[i]));
			}

			for (int i = 0; i < p_scene.Powerups.Count; i++)
			{
				package.Powerups.Add(CreateObject(p_scene.Powerups[i]));
			}

			for (int i = 0; i < p_scene.Hazards.Count; i++)
			{
				package.Hazards.Add(CreateObject(p_scene.Hazards[i]));
			}

			for (int i = 0; i < p_scene.Emitters.Count; i++)
			{
				package.Emitters.Add(CreateObject(p_scene.Emitters[i]));
			}

			for (int i = 0; i < p_scene.Paths.Count; i++)
			{
				package.Paths.Add(CreatePath(p_scene.Paths[i]));
			}

			for (int i = 0; i < p_scene.NpcPaths.Count; i++)
			{
				package.NpcPaths.Add(CreatePath(p_scene.NpcPaths[i]));
			}

			for (int i = 0; i < p_scene.Gradients.Count; i++)
			{
				package.Gradients.Add(CreateGradient(p_scene.Gradients[i]));
			}

			return package;
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

		private static TrackMaterialJsonDto CreateMaterial(TrackMaterial p_material)
		{
			TrackMaterialJsonDto output = new TrackMaterialJsonDto();
			TrackMaterial source = p_material ?? new TrackMaterial();
			output.Id = GetId(source.Id, source.Name);
			output.SourceId = GetStringValue(source.SourceId, source.Metadata, "SourceId", "MaterialIndex", "GradientSetIndex");
			output.Name = source.Name ?? string.Empty;
			output.SourceName = GetStringValue(source.SourceName, source.Metadata, "SourceName", "GradientKey");
			output.SourceFormat = GetStringValue(source.SourceFormat, source.Metadata, "SourceFormat");
			output.SourcePath = GetStringValue(source.SourcePath, source.Metadata, "SourcePath");
			output.SourceIndex = GetNullableIntValue(source.SourceIndex, source.Metadata, "SourceIndex", "MaterialIndex", "GradientSetIndex");
			output.TextureName = source.TextureName ?? string.Empty;
			output.AlphaTextureName = source.AlphaTextureName ?? string.Empty;
			output.DiffuseColor = ToColorArray(source.DiffuseColor);
			output.Opacity = source.Opacity;
			output.DoubleSided = source.DoubleSided;
			output.MaterialAnimationIds = new List<string>();
			output.Metadata = CloneMetadata(source.Metadata);
			output.Gradients = new List<TrackGradientJsonDto>();

			for (int i = 0; i < source.MaterialAnimationIds.Count; i++)
			{
				output.MaterialAnimationIds.Add(source.MaterialAnimationIds[i]);
			}

			for (int i = 0; i < source.Gradients.Count; i++)
			{
				output.Gradients.Add(CreateGradient(source.Gradients[i]));
			}

			return output;
		}

		private static TrackMeshJsonDto CreateMesh(TrackMesh p_mesh)
		{
			TrackMeshJsonDto output = new TrackMeshJsonDto();
			TrackMesh source = p_mesh ?? new TrackMesh();
			output.Id = GetId(source.Id, source.Name);
			output.SourceId = GetStringValue(source.SourceId, source.Metadata, "SourceId");
			output.Name = source.Name ?? string.Empty;
			output.SourceName = GetStringValue(source.SourceName, source.Metadata, "SourceName");
			output.SourceFormat = GetStringValue(source.SourceFormat, source.Metadata, "SourceFormat");
			output.SourcePath = GetStringValue(source.SourcePath, source.Metadata, "SourcePath");
			output.SourceIndex = GetNullableIntValue(source.SourceIndex, source.Metadata, "SourceIndex");
			output.MaterialName = source.MaterialName ?? string.Empty;
			output.IsCollisionMesh = source.IsCollisionMesh;
			output.Metadata = CloneMetadata(source.Metadata);
			output.Vertices = new List<TrackVertexJsonDto>();
			output.Indices = new List<int>();

			for (int i = 0; i < source.Vertices.Count; i++)
			{
				output.Vertices.Add(CreateVertex(source.Vertices[i]));
			}

			for (int i = 0; i < source.Indices.Count; i++)
			{
				output.Indices.Add(source.Indices[i]);
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
			TrackObject source = p_object ?? new TrackObject();
			output.Id = GetId(source.Id, source.Name);
			output.SourceId = GetStringValue(source.SourceId, source.Metadata, "SourceId", "StartIndex", "MeshIndex", "GDBIndex", "BDBIndex", "BVBIndex", "EmitterPositionIndex");
			output.Name = source.Name ?? string.Empty;
			output.SourceName = GetStringValue(source.SourceName, source.Metadata, "SourceName", "GDBName", "EmitterName");
			output.SourceFormat = GetStringValue(source.SourceFormat, source.Metadata, "SourceFormat");
			output.SourcePath = GetStringValue(source.SourcePath, source.Metadata, "SourcePath");
			output.SourceIndex = GetNullableIntValue(source.SourceIndex, source.Metadata, "SourceIndex", "StartIndex", "MeshIndex", "GDBIndex", "BDBIndex", "BVBIndex", "EmitterPositionIndex");
			output.MeshName = source.MeshName ?? string.Empty;
			output.MaterialName = source.MaterialName ?? string.Empty;
			output.PathName = source.PathName ?? string.Empty;
			output.Visible = source.Visible;
			output.Transform = CreateTransform(source.Transform);
			output.Metadata = CloneMetadata(source.Metadata);
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
			TrackPath source = p_path ?? new TrackPath();
			output.Id = GetId(source.Id, source.Name);
			output.SourceId = GetStringValue(source.SourceId, source.Metadata, "SourceId");
			output.Name = source.Name ?? string.Empty;
			output.SourceName = GetStringValue(source.SourceName, source.Metadata, "SourceName");
			output.SourceFormat = GetStringValue(source.SourceFormat, source.Metadata, "SourceFormat");
			output.SourcePath = GetStringValue(source.SourcePath, source.Metadata, "SourcePath");
			output.SourceIndex = GetNullableIntValue(source.SourceIndex, source.Metadata, "SourceIndex");
			output.Closed = source.Closed;
			output.Metadata = CloneMetadata(source.Metadata);
			output.Nodes = new List<TrackPathNodeJsonDto>();

			for (int i = 0; i < source.Nodes.Count; i++)
			{
				output.Nodes.Add(CreatePathNode(source.Nodes[i]));
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
			TrackGradient source = p_gradient ?? new TrackGradient();
			output.Id = GetId(source.Id, source.Name);
			output.SourceId = GetStringValue(source.SourceId, source.Metadata, "SourceId", "GradientSetIndex");
			output.Name = source.Name ?? string.Empty;
			output.SourceName = GetStringValue(source.SourceName, source.Metadata, "SourceName", "GradientKey");
			output.SourceFormat = GetStringValue(source.SourceFormat, source.Metadata, "SourceFormat");
			output.SourcePath = GetStringValue(source.SourcePath, source.Metadata, "SourcePath");
			output.SourceIndex = GetNullableIntValue(source.SourceIndex, source.Metadata, "SourceIndex", "GradientSetIndex");
			output.Metadata = CloneMetadata(source.Metadata);
			output.Stops = new List<TrackGradientStopJsonDto>();

			for (int i = 0; i < source.Stops.Count; i++)
			{
				output.Stops.Add(CreateGradientStop(source.Stops[i]));
			}

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

		private static TrackGradientStopJsonDto CreateGradientStop(TrackGradientStop p_stop)
		{
			TrackGradientStopJsonDto output = new TrackGradientStopJsonDto();
			output.Position = p_stop.Position;
			output.Color = ToColorArray(p_stop.Color);
			output.Metadata = CloneMetadata(p_stop.Metadata);
			return output;
		}

		private static TrackMaterialAnimationJsonDto CreateMaterialAnimation(TrackMaterialAnimation p_animation)
		{
			TrackMaterialAnimationJsonDto output = new TrackMaterialAnimationJsonDto();
			TrackMaterialAnimation source = p_animation ?? new TrackMaterialAnimation();
			output.Id = GetId(source.Id, source.Name);
			output.SourceId = GetStringValue(source.SourceId, source.Metadata, "SourceId");
			output.Name = source.Name ?? string.Empty;
			output.SourceName = GetStringValue(source.SourceName, source.Metadata, "SourceName");
			output.SourceFormat = GetStringValue(source.SourceFormat, source.Metadata, "SourceFormat");
			output.SourcePath = GetStringValue(source.SourcePath, source.Metadata, "SourcePath");
			output.SourceIndex = GetNullableIntValue(source.SourceIndex, source.Metadata, "SourceIndex");
			output.MaterialName = source.MaterialName ?? string.Empty;
			output.Behavior = source.Behavior ?? string.Empty;
			output.LoopMode = source.LoopMode ?? string.Empty;
			output.FrameCount = source.FrameCount;
			output.Speed = source.Speed;
			output.UvOffset = ToArray(source.UvOffset);
			output.UvVelocity = ToArray(source.UvVelocity);
			output.Metadata = CloneMetadata(source.Metadata);
			output.Frames = new List<TrackMaterialAnimationFrameJsonDto>();

			for (int i = 0; i < source.Frames.Count; i++)
			{
				output.Frames.Add(CreateMaterialAnimationFrame(source.Frames[i]));
			}

			return output;
		}

		private static TrackMaterialAnimationFrameJsonDto CreateMaterialAnimationFrame(TrackMaterialAnimationFrame p_frame)
		{
			TrackMaterialAnimationFrameJsonDto output = new TrackMaterialAnimationFrameJsonDto();
			TrackMaterialAnimationFrame source = p_frame ?? new TrackMaterialAnimationFrame();
			output.MaterialName = source.MaterialName ?? string.Empty;
			output.FrameIndex = source.FrameIndex;
			output.UvOffset = ToArray(source.UvOffset);
			output.Metadata = CloneMetadata(source.Metadata);
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

