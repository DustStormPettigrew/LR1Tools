using LR1Tools.Contracts;
using LibLR1;
using LibLR1.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace LR1Tools.Adapters
{
	internal static class AdapterCommon
	{
		private const float EPSILON = 0.00001f;

		public static Vector2 ToVector2(LRVector2 p_value)
		{
			if (p_value == null)
			{
				return Vector2.Zero;
			}

			return new Vector2(p_value.X, p_value.Y);
		}

		public static Vector3 ToVector3(LRVector3 p_value)
		{
			if (p_value == null)
			{
				return Vector3.Zero;
			}

			return new Vector3(p_value.X, p_value.Y, p_value.Z);
		}

		public static Quaternion ToQuaternion(LRQuaternion p_value)
		{
			if (p_value == null)
			{
				return Quaternion.Identity;
			}

			return new Quaternion(p_value.X, p_value.Y, p_value.Z, p_value.W);
		}

		public static TrackColor ToTrackColor(LRColor p_value, bool p_hasAlpha = true)
		{
			if (p_value == null)
			{
				return new TrackColor(0f, 0f, 0f, p_hasAlpha ? 0f : 1f);
			}

			return new TrackColor(
				p_value.R / 255f,
				p_value.G / 255f,
				p_value.B / 255f,
				p_hasAlpha ? p_value.A / 255f : 1f
			);
		}

		public static void AddMetadata(Dictionary<string, string> p_metadata, string p_key, string p_value)
		{
			if (p_metadata == null || string.IsNullOrEmpty(p_key) || p_value == null)
			{
				return;
			}

			p_metadata[p_key] = p_value;
		}

		public static void AddMetadata(Dictionary<string, string> p_metadata, string p_key, int p_value)
		{
			AddMetadata(p_metadata, p_key, p_value.ToString(CultureInfo.InvariantCulture));
		}

		public static void AddMetadata(Dictionary<string, string> p_metadata, string p_key, float p_value)
		{
			AddMetadata(p_metadata, p_key, p_value.ToString("R", CultureInfo.InvariantCulture));
		}

		public static void AddMetadata(Dictionary<string, string> p_metadata, string p_key, bool p_value)
		{
			AddMetadata(p_metadata, p_key, p_value ? "true" : "false");
		}

		public static void AddArrayMetadata(Dictionary<string, string> p_metadata, string p_prefix, string[] p_values)
		{
			if (p_values == null)
			{
				return;
			}

			AddMetadata(p_metadata, p_prefix + ".Count", p_values.Length);
			for (int i = 0; i < p_values.Length; i++)
			{
				AddMetadata(p_metadata, string.Format("{0}[{1}]", p_prefix, i), p_values[i] ?? string.Empty);
			}
		}

		public static string FormatVector2(LRVector2 p_value)
		{
			if (p_value == null)
			{
				return "0,0";
			}

			return string.Format(
				CultureInfo.InvariantCulture,
				"{0:R},{1:R}",
				p_value.X,
				p_value.Y
			);
		}

		public static string FormatVector3(LRVector3 p_value)
		{
			if (p_value == null)
			{
				return "0,0,0";
			}

			return string.Format(
				CultureInfo.InvariantCulture,
				"{0:R},{1:R},{2:R}",
				p_value.X,
				p_value.Y,
				p_value.Z
			);
		}

		public static string FormatQuaternion(LRQuaternion p_value)
		{
			if (p_value == null)
			{
				return "0,0,0,1";
			}

			return string.Format(
				CultureInfo.InvariantCulture,
				"{0:R},{1:R},{2:R},{3:R}",
				p_value.X,
				p_value.Y,
				p_value.Z,
				p_value.W
			);
		}

		public static string FormatColor(LRColor p_value, bool p_hasAlpha = true)
		{
			if (p_value == null)
			{
				return p_hasAlpha ? "0,0,0,0" : "0,0,0";
			}

			return p_hasAlpha
				? string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", p_value.R, p_value.G, p_value.B, p_value.A)
				: string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", p_value.R, p_value.G, p_value.B);
		}

		public static string ResolveName(string[] p_values, int p_index, string p_fallbackPrefix)
		{
			if (p_values != null && p_index >= 0 && p_index < p_values.Length)
			{
				return p_values[p_index];
			}

			return string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", p_fallbackPrefix, p_index);
		}

		public static Quaternion CreateRotationFromForwardUp(LRVector3 p_forward, LRVector3 p_up)
		{
			Vector3 forward = Normalize(ToVector3(p_forward));
			Vector3 up = Normalize(ToVector3(p_up));

			if (LengthSquared(forward) < EPSILON)
			{
				forward = Vector3.UnitX;
			}

			if (LengthSquared(up) < EPSILON)
			{
				up = Vector3.UnitZ;
			}

			Vector3 right = Normalize(Vector3.Cross(up, forward));
			if (LengthSquared(right) < EPSILON)
			{
				right = Vector3.UnitY;
			}

			Vector3 orthoUp = Normalize(Vector3.Cross(forward, right));
			if (LengthSquared(orthoUp) < EPSILON)
			{
				orthoUp = Vector3.UnitZ;
			}

			// Match LR1TrackEditor.CreateWorldMatrix:
			// local +X = forward, local +Y = right, local +Z = up.
			Matrix4x4 basis = new Matrix4x4(
				forward.X, forward.Y, forward.Z, 0f,
				right.X, right.Y, right.Z, 0f,
				orthoUp.X, orthoUp.Y, orthoUp.Z, 0f,
				0f, 0f, 0f, 1f
			);
			return Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(basis));
		}

		public static Vector3 RotateForward(LRQuaternion p_rotation)
		{
			return Vector3.Transform(Vector3.UnitZ, Quaternion.Normalize(ToQuaternion(p_rotation)));
		}

		public static Vector3 RotateUp(LRQuaternion p_rotation)
		{
			return Vector3.Transform(Vector3.UnitY, Quaternion.Normalize(ToQuaternion(p_rotation)));
		}

		public static TrackVertex CloneVertex(TrackVertex p_vertex)
		{
			TrackVertex output = new TrackVertex();
			output.Position = new Vector3(p_vertex.Position.X, p_vertex.Position.Y, p_vertex.Position.Z);
			output.Normal = new Vector3(p_vertex.Normal.X, p_vertex.Normal.Y, p_vertex.Normal.Z);
			output.PrimaryTexCoord = new Vector2(p_vertex.PrimaryTexCoord.X, p_vertex.PrimaryTexCoord.Y);
			output.Color = new TrackColor(p_vertex.Color.R, p_vertex.Color.G, p_vertex.Color.B, p_vertex.Color.A);

			foreach (KeyValuePair<string, string> pair in p_vertex.Metadata)
			{
				output.Metadata.Add(pair.Key, pair.Value);
			}

			return output;
		}

		public static void SetSceneProvenance(TrackScene p_scene, string p_sourceFormat, string p_name, string p_sourceName = null, string p_sourceId = null, int? p_sourceIndex = null)
		{
			if (p_scene == null)
			{
				return;
			}

			p_scene.Id = !string.IsNullOrWhiteSpace(p_scene.Id) ? p_scene.Id : (p_name ?? string.Empty);
			p_scene.SourceId = !string.IsNullOrWhiteSpace(p_scene.SourceId) ? p_scene.SourceId : (p_sourceId ?? p_scene.Id ?? string.Empty);
			p_scene.SourceName = !string.IsNullOrWhiteSpace(p_scene.SourceName) ? p_scene.SourceName : (p_sourceName ?? p_name ?? string.Empty);
			p_scene.SourceFormat = !string.IsNullOrWhiteSpace(p_scene.SourceFormat) ? p_scene.SourceFormat : (p_sourceFormat ?? string.Empty);
			p_scene.SourceIndex = p_scene.SourceIndex.HasValue ? p_scene.SourceIndex : p_sourceIndex;
			AddMetadata(p_scene.Metadata, "SourceFormat", p_scene.SourceFormat);
		}

		public static void SetMaterialProvenance(TrackMaterial p_material, string p_sourceFormat, string p_name, string p_sourceName = null, string p_sourceId = null, int? p_sourceIndex = null)
		{
			if (p_material == null)
			{
				return;
			}

			p_material.Id = !string.IsNullOrWhiteSpace(p_material.Id) ? p_material.Id : (p_name ?? string.Empty);
			p_material.SourceId = !string.IsNullOrWhiteSpace(p_material.SourceId) ? p_material.SourceId : (p_sourceId ?? string.Empty);
			p_material.SourceName = !string.IsNullOrWhiteSpace(p_material.SourceName) ? p_material.SourceName : (p_sourceName ?? p_name ?? string.Empty);
			p_material.SourceFormat = !string.IsNullOrWhiteSpace(p_material.SourceFormat) ? p_material.SourceFormat : (p_sourceFormat ?? string.Empty);
			p_material.SourceIndex = p_material.SourceIndex.HasValue ? p_material.SourceIndex : p_sourceIndex;
			AddMetadata(p_material.Metadata, "SourceFormat", p_material.SourceFormat);
		}

		public static void SetMeshProvenance(TrackMesh p_mesh, string p_sourceFormat, string p_name, string p_sourceName = null, string p_sourceId = null, int? p_sourceIndex = null)
		{
			if (p_mesh == null)
			{
				return;
			}

			p_mesh.Id = !string.IsNullOrWhiteSpace(p_mesh.Id) ? p_mesh.Id : (p_name ?? string.Empty);
			p_mesh.SourceId = !string.IsNullOrWhiteSpace(p_mesh.SourceId) ? p_mesh.SourceId : (p_sourceId ?? string.Empty);
			p_mesh.SourceName = !string.IsNullOrWhiteSpace(p_mesh.SourceName) ? p_mesh.SourceName : (p_sourceName ?? p_name ?? string.Empty);
			p_mesh.SourceFormat = !string.IsNullOrWhiteSpace(p_mesh.SourceFormat) ? p_mesh.SourceFormat : (p_sourceFormat ?? string.Empty);
			p_mesh.SourceIndex = p_mesh.SourceIndex.HasValue ? p_mesh.SourceIndex : p_sourceIndex;
			AddMetadata(p_mesh.Metadata, "SourceFormat", p_mesh.SourceFormat);
		}

		public static void SetObjectProvenance(TrackObject p_object, string p_sourceFormat, string p_name, string p_sourceName = null, string p_sourceId = null, int? p_sourceIndex = null)
		{
			if (p_object == null)
			{
				return;
			}

			p_object.Id = !string.IsNullOrWhiteSpace(p_object.Id) ? p_object.Id : (p_name ?? string.Empty);
			p_object.SourceId = !string.IsNullOrWhiteSpace(p_object.SourceId) ? p_object.SourceId : (p_sourceId ?? string.Empty);
			p_object.SourceName = !string.IsNullOrWhiteSpace(p_object.SourceName) ? p_object.SourceName : (p_sourceName ?? p_name ?? string.Empty);
			p_object.SourceFormat = !string.IsNullOrWhiteSpace(p_object.SourceFormat) ? p_object.SourceFormat : (p_sourceFormat ?? string.Empty);
			p_object.SourceIndex = p_object.SourceIndex.HasValue ? p_object.SourceIndex : p_sourceIndex;
			AddMetadata(p_object.Metadata, "SourceFormat", p_object.SourceFormat);
		}

		public static void SetPathProvenance(TrackPath p_path, string p_sourceFormat, string p_name, string p_sourceName = null, string p_sourceId = null, int? p_sourceIndex = null)
		{
			if (p_path == null)
			{
				return;
			}

			p_path.Id = !string.IsNullOrWhiteSpace(p_path.Id) ? p_path.Id : (p_name ?? string.Empty);
			p_path.SourceId = !string.IsNullOrWhiteSpace(p_path.SourceId) ? p_path.SourceId : (p_sourceId ?? string.Empty);
			p_path.SourceName = !string.IsNullOrWhiteSpace(p_path.SourceName) ? p_path.SourceName : (p_sourceName ?? p_name ?? string.Empty);
			p_path.SourceFormat = !string.IsNullOrWhiteSpace(p_path.SourceFormat) ? p_path.SourceFormat : (p_sourceFormat ?? string.Empty);
			p_path.SourceIndex = p_path.SourceIndex.HasValue ? p_path.SourceIndex : p_sourceIndex;
			AddMetadata(p_path.Metadata, "SourceFormat", p_path.SourceFormat);
		}

		public static void SetGradientProvenance(TrackGradient p_gradient, string p_sourceFormat, string p_name, string p_sourceName = null, string p_sourceId = null, int? p_sourceIndex = null)
		{
			if (p_gradient == null)
			{
				return;
			}

			p_gradient.Id = !string.IsNullOrWhiteSpace(p_gradient.Id) ? p_gradient.Id : (p_name ?? string.Empty);
			p_gradient.SourceId = !string.IsNullOrWhiteSpace(p_gradient.SourceId) ? p_gradient.SourceId : (p_sourceId ?? string.Empty);
			p_gradient.SourceName = !string.IsNullOrWhiteSpace(p_gradient.SourceName) ? p_gradient.SourceName : (p_sourceName ?? p_name ?? string.Empty);
			p_gradient.SourceFormat = !string.IsNullOrWhiteSpace(p_gradient.SourceFormat) ? p_gradient.SourceFormat : (p_sourceFormat ?? string.Empty);
			p_gradient.SourceIndex = p_gradient.SourceIndex.HasValue ? p_gradient.SourceIndex : p_sourceIndex;
			AddMetadata(p_gradient.Metadata, "SourceFormat", p_gradient.SourceFormat);
		}

		public static void SetMaterialAnimationProvenance(TrackMaterialAnimation p_animation, string p_sourceFormat, string p_name, string p_sourceName = null, string p_sourceId = null, int? p_sourceIndex = null)
		{
			if (p_animation == null)
			{
				return;
			}

			p_animation.Id = !string.IsNullOrWhiteSpace(p_animation.Id) ? p_animation.Id : (p_name ?? string.Empty);
			p_animation.SourceId = !string.IsNullOrWhiteSpace(p_animation.SourceId) ? p_animation.SourceId : (p_sourceId ?? string.Empty);
			p_animation.SourceName = !string.IsNullOrWhiteSpace(p_animation.SourceName) ? p_animation.SourceName : (p_sourceName ?? p_name ?? string.Empty);
			p_animation.SourceFormat = !string.IsNullOrWhiteSpace(p_animation.SourceFormat) ? p_animation.SourceFormat : (p_sourceFormat ?? string.Empty);
			p_animation.SourceIndex = p_animation.SourceIndex.HasValue ? p_animation.SourceIndex : p_sourceIndex;
			AddMetadata(p_animation.Metadata, "SourceFormat", p_animation.SourceFormat);
		}

		private static Vector3 Normalize(Vector3 p_value)
		{
			float length = p_value.Length();
			if (length < EPSILON)
			{
				return Vector3.Zero;
			}

			return Vector3.Normalize(p_value);
		}

		private static float LengthSquared(Vector3 p_value)
		{
			return p_value.LengthSquared();
		}
	}
}


