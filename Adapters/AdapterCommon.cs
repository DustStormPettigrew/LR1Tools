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
				forward = Vector3.UnitZ;
			}

			if (LengthSquared(up) < EPSILON)
			{
				up = Vector3.UnitY;
			}

			Vector3 right = Normalize(Vector3.Cross(up, forward));
			if (LengthSquared(right) < EPSILON)
			{
				right = Vector3.UnitX;
			}

			Vector3 orthoUp = Normalize(Vector3.Cross(forward, right));
			Matrix4x4 basis = new Matrix4x4(
				right.X, right.Y, right.Z, 0f,
				orthoUp.X, orthoUp.Y, orthoUp.Z, 0f,
				forward.X, forward.Y, forward.Z, 0f,
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


