using LR1Tools.Contracts;
using LibLR1;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace LR1Tools.Export
{
	public static class TrackTextureImageExporter
	{
		private static readonly byte[] ms_pngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

		public static void ExportSceneTextures(TrackScene p_scene, string p_outputJsonPath)
		{
			if (p_scene == null || string.IsNullOrWhiteSpace(p_outputJsonPath) || p_scene.Textures == null || p_scene.Textures.Count == 0)
			{
				return;
			}

			string jsonDirectory = Path.GetDirectoryName(Path.GetFullPath(p_outputJsonPath));
			string jsonName = Path.GetFileNameWithoutExtension(p_outputJsonPath);
			string textureDirectory = Path.Combine(jsonDirectory ?? Directory.GetCurrentDirectory(), jsonName + "_Textures");
			Dictionary<string, string> exportedPathsByTextureId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			bool wroteAnyTexture = false;

			for (int i = 0; i < p_scene.Textures.Count; i++)
			{
				TrackTexture texture = p_scene.Textures[i];
				if (texture == null || string.IsNullOrWhiteSpace(texture.SourcePath))
				{
					continue;
				}

				if (!File.Exists(texture.SourcePath))
				{
					texture.Metadata["Export.MissingSource"] = "true";
					continue;
				}

				if (!string.Equals(Path.GetExtension(texture.SourcePath), ".BMP", StringComparison.OrdinalIgnoreCase))
				{
					texture.Metadata["Export.UnsupportedSourceFormat"] = Path.GetExtension(texture.SourcePath);
					continue;
				}

				try
				{
					BMP bitmap = new BMP(texture.SourcePath);
					if (!wroteAnyTexture)
					{
						Directory.CreateDirectory(textureDirectory);
						wroteAnyTexture = true;
					}

					string fileName = GetUniqueTextureFileName(texture, usedFileNames);
					string absoluteExportPath = Path.Combine(textureDirectory, fileName);
					WritePng(bitmap, absoluteExportPath);
					string relativeExportPath = Path.Combine(Path.GetFileName(textureDirectory), fileName);

					texture.ExportPath = relativeExportPath;
					texture.Metadata["Export.ImageFormat"] = "PNG";
					texture.Metadata["Export.ImagePath"] = relativeExportPath;
					exportedPathsByTextureId[GetTextureKey(texture)] = relativeExportPath;
				}
				catch (Exception ex)
				{
					texture.Metadata["Export.ImageError"] = ex.GetType().Name;
					texture.Metadata["Export.ImageErrorMessage"] = ex.Message;
				}
			}

			ApplyReferenceExportPaths(p_scene.Materials, exportedPathsByTextureId);
		}

		public static void ExportBitmapToPng(BMP p_bitmap, string p_outputPath)
		{
			if (p_bitmap == null)
				throw new ArgumentNullException(nameof(p_bitmap));
			if (string.IsNullOrWhiteSpace(p_outputPath))
				throw new ArgumentException("An output path is required.", nameof(p_outputPath));

			string directory = Path.GetDirectoryName(Path.GetFullPath(p_outputPath));
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);
			WritePng(p_bitmap, p_outputPath);
		}

		private static void ApplyReferenceExportPaths(IList<TrackMaterial> p_materials, Dictionary<string, string> p_exportedPathsByTextureId)
		{
			if (p_materials == null || p_exportedPathsByTextureId == null)
			{
				return;
			}

			for (int i = 0; i < p_materials.Count; i++)
			{
				TrackMaterial material = p_materials[i];
				if (material == null)
				{
					continue;
				}

				ApplyReferenceExportPath(material.TextureRef, p_exportedPathsByTextureId);
				ApplyReferenceExportPath(material.AlphaTextureRef, p_exportedPathsByTextureId);
			}
		}

		private static void ApplyReferenceExportPath(TrackTextureReference p_reference, Dictionary<string, string> p_exportedPathsByTextureId)
		{
			if (p_reference == null)
			{
				return;
			}

			string key = GetTextureReferenceKey(p_reference);
			string exportPath;
			if (!string.IsNullOrEmpty(key) && p_exportedPathsByTextureId.TryGetValue(key, out exportPath))
			{
				p_reference.ExportPath = exportPath;
			}
		}

		private static string GetTextureReferenceKey(TrackTextureReference p_reference)
		{
			if (p_reference == null)
			{
				return null;
			}

			if (!string.IsNullOrWhiteSpace(p_reference.TextureId))
			{
				return p_reference.TextureId.Trim();
			}

			if (!string.IsNullOrWhiteSpace(p_reference.SourcePath))
			{
				return p_reference.SourcePath.Trim();
			}

			return string.IsNullOrWhiteSpace(p_reference.Name) ? null : p_reference.Name.Trim();
		}

		private static string GetTextureKey(TrackTexture p_texture)
		{
			if (p_texture == null)
			{
				return null;
			}

			if (!string.IsNullOrWhiteSpace(p_texture.Id))
			{
				return p_texture.Id.Trim();
			}

			if (!string.IsNullOrWhiteSpace(p_texture.SourcePath))
			{
				return p_texture.SourcePath.Trim();
			}

			return string.IsNullOrWhiteSpace(p_texture.Name) ? null : p_texture.Name.Trim();
		}

		private static string GetUniqueTextureFileName(TrackTexture p_texture, HashSet<string> p_usedFileNames)
		{
			string stem = !string.IsNullOrWhiteSpace(p_texture.Name) ? p_texture.Name : Path.GetFileNameWithoutExtension(p_texture.SourcePath);
			stem = SanitizeFileName(stem);
			if (string.IsNullOrEmpty(stem))
			{
				stem = "Texture";
			}

			string candidate = stem + ".png";
			int suffix = 1;
			while (!p_usedFileNames.Add(candidate))
			{
				candidate = stem + "_" + suffix.ToString(CultureInfo.InvariantCulture) + ".png";
				suffix++;
			}

			return candidate;
		}

		private static string SanitizeFileName(string p_value)
		{
			if (string.IsNullOrWhiteSpace(p_value))
			{
				return string.Empty;
			}

			StringBuilder builder = new StringBuilder(p_value.Length);
			for (int i = 0; i < p_value.Length; i++)
			{
				char c = p_value[i];
				builder.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c);
			}

			return builder.ToString().Trim();
		}

		private static void WritePng(BMP p_bitmap, string p_outputPath)
		{
			using (FileStream stream = File.Create(p_outputPath))
			{
				stream.Write(ms_pngSignature, 0, ms_pngSignature.Length);
				WriteChunk(stream, "IHDR", CreateIHdrData(p_bitmap.Width, p_bitmap.Height));
				WriteChunk(stream, "IDAT", CreateImageData(p_bitmap));
				WriteChunk(stream, "IEND", Array.Empty<byte>());
			}
		}

		private static byte[] CreateIHdrData(int p_width, int p_height)
		{
			byte[] data = new byte[13];
			WriteInt32BigEndian(data, 0, p_width);
			WriteInt32BigEndian(data, 4, p_height);
			data[8] = 8;
			data[9] = 2;
			data[10] = 0;
			data[11] = 0;
			data[12] = 0;
			return data;
		}

		private static byte[] CreateImageData(BMP p_bitmap)
		{
			using (MemoryStream raw = new MemoryStream())
			{
				for (int y = 0; y < p_bitmap.Height; y++)
				{
					raw.WriteByte(0);
					for (int x = 0; x < p_bitmap.Width; x++)
					{
						BitmapColor color = p_bitmap.GetPixel(x, y);
						raw.WriteByte(color.r);
						raw.WriteByte(color.g);
						raw.WriteByte(color.b);
					}
				}

				raw.Position = 0;
				using (MemoryStream compressed = new MemoryStream())
				{
					using (ZLibStream zlib = new ZLibStream(compressed, CompressionLevel.Optimal, true))
					{
						raw.CopyTo(zlib);
					}

					return compressed.ToArray();
				}
			}
		}

		private static void WriteChunk(Stream p_stream, string p_type, byte[] p_data)
		{
			byte[] typeBytes = Encoding.ASCII.GetBytes(p_type);
			byte[] lengthBytes = new byte[4];
			WriteInt32BigEndian(lengthBytes, 0, p_data.Length);
			p_stream.Write(lengthBytes, 0, lengthBytes.Length);
			p_stream.Write(typeBytes, 0, typeBytes.Length);
			if (p_data.Length > 0)
			{
				p_stream.Write(p_data, 0, p_data.Length);
			}

			uint crc = ComputeCrc(typeBytes, p_data);
			byte[] crcBytes = new byte[4];
			WriteUInt32BigEndian(crcBytes, 0, crc);
			p_stream.Write(crcBytes, 0, crcBytes.Length);
		}

		private static uint ComputeCrc(byte[] p_typeBytes, byte[] p_data)
		{
			uint crc = 0xFFFFFFFFu;
			crc = UpdateCrc(crc, p_typeBytes, 0, p_typeBytes.Length);
			crc = UpdateCrc(crc, p_data, 0, p_data.Length);
			return crc ^ 0xFFFFFFFFu;
		}

		private static uint UpdateCrc(uint p_crc, byte[] p_buffer, int p_offset, int p_count)
		{
			uint crc = p_crc;
			for (int i = 0; i < p_count; i++)
			{
				crc ^= p_buffer[p_offset + i];
				for (int bit = 0; bit < 8; bit++)
				{
					crc = (crc & 1u) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
				}
			}

			return crc;
		}

		private static void WriteInt32BigEndian(byte[] p_buffer, int p_offset, int p_value)
		{
			p_buffer[p_offset + 0] = (byte)((p_value >> 24) & 0xFF);
			p_buffer[p_offset + 1] = (byte)((p_value >> 16) & 0xFF);
			p_buffer[p_offset + 2] = (byte)((p_value >> 8) & 0xFF);
			p_buffer[p_offset + 3] = (byte)(p_value & 0xFF);
		}

		private static void WriteUInt32BigEndian(byte[] p_buffer, int p_offset, uint p_value)
		{
			p_buffer[p_offset + 0] = (byte)((p_value >> 24) & 0xFF);
			p_buffer[p_offset + 1] = (byte)((p_value >> 16) & 0xFF);
			p_buffer[p_offset + 2] = (byte)((p_value >> 8) & 0xFF);
			p_buffer[p_offset + 3] = (byte)(p_value & 0xFF);
		}
	}
}
