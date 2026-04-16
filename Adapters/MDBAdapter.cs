using LR1Tools.Contracts;
using LibLR1;
using System.Collections.Generic;
using System.Globalization;

namespace LR1Tools.Adapters
{
	public static class MDBAdapter
	{
		public static List<TrackMaterial> ToMaterials(MDB p_source)
		{
			List<TrackMaterial> output = new List<TrackMaterial>();
			if (p_source == null || p_source.Materials == null)
			{
				return output;
			}

			foreach (KeyValuePair<string, MDB_Material> pair in p_source.Materials)
			{
				output.Add(ToMaterial(pair.Key, pair.Value));
			}

			return output;
		}

		public static TrackMaterial ToMaterial(string p_name, MDB_Material p_source)
		{
			TrackMaterial material = new TrackMaterial();
			material.Name = p_name ?? string.Empty;
			AdapterCommon.SetMaterialProvenance(material, "MDB", material.Name, material.Name, material.Name);

			if (p_source == null)
			{
				return material;
			}

			if (!string.IsNullOrEmpty(p_source.TextureName))
			{
				material.TextureName = p_source.TextureName;
			}

			if (p_source.DiffuseColor != null)
			{
				material.DiffuseColor = AdapterCommon.ToTrackColor(p_source.DiffuseColor);
				material.Metadata["DiffuseColor"] = AdapterCommon.FormatColor(p_source.DiffuseColor);
			}

			if (p_source.AmbientColor != null)
			{
				material.Metadata["AmbientColor"] = AdapterCommon.FormatColor(p_source.AmbientColor);
			}

			if (p_source.Alpha.HasValue)
			{
				material.Opacity = p_source.Alpha.Value / 255f;
				material.Metadata["Alpha"] = p_source.Alpha.Value.ToString(CultureInfo.InvariantCulture);
			}

			AddPropertyMetadata(material.Metadata, p_source);
			return material;
		}

		private static void AddPropertyMetadata(Dictionary<string, string> p_metadata, MDB_Material p_source)
		{
			AdapterCommon.AddMetadata(p_metadata, "Bool2A", p_source.Bool2A);
			AdapterCommon.AddMetadata(p_metadata, "Bool2B", p_source.Bool2B);
			AdapterCommon.AddMetadata(p_metadata, "Bool2D", p_source.Bool2D);
			AdapterCommon.AddMetadata(p_metadata, "Bool2E", p_source.Bool2E);
			AdapterCommon.AddMetadata(p_metadata, "Bool3A", p_source.Bool3A);
			AdapterCommon.AddMetadata(p_metadata, "Bool3F", p_source.Bool3F);
			AdapterCommon.AddMetadata(p_metadata, "Bool41", p_source.Bool41);
			AdapterCommon.AddMetadata(p_metadata, "Bool44", p_source.Bool44);
			AdapterCommon.AddMetadata(p_metadata, "Bool45", p_source.Bool45);
			AdapterCommon.AddMetadata(p_metadata, "Bool47", p_source.Bool47);
			AdapterCommon.AddMetadata(p_metadata, "Bool48", p_source.Bool48);
			AdapterCommon.AddMetadata(p_metadata, "Bool49", p_source.Bool49);
			AdapterCommon.AddMetadata(p_metadata, "Bool4A", p_source.Bool4A);
			AdapterCommon.AddMetadata(p_metadata, "Bool4B", p_source.Bool4B);
			AdapterCommon.AddMetadata(p_metadata, "Bool4C", p_source.Bool4C);

			if (p_source.Property2F != null)
			{
				AdapterCommon.AddMetadata(p_metadata, "Property2F.SubToken", string.Format(CultureInfo.InvariantCulture, "0x{0:X2}", p_source.Property2F.SubToken));
				if (p_source.Property2F.Value.HasValue)
				{
					AdapterCommon.AddMetadata(p_metadata, "Property2F.Value", p_source.Property2F.Value.Value);
				}
			}

			if (p_source.Property38 != null)
			{
				AdapterCommon.AddMetadata(p_metadata, "Property38.SubToken1", string.Format(CultureInfo.InvariantCulture, "0x{0:X2}", p_source.Property38.SubToken1));
				AdapterCommon.AddMetadata(p_metadata, "Property38.SubToken2", string.Format(CultureInfo.InvariantCulture, "0x{0:X2}", p_source.Property38.SubToken2));
			}

			if (p_source.Int4D.HasValue)
			{
				AdapterCommon.AddMetadata(p_metadata, "Int4D", p_source.Int4D.Value);
			}

			if (p_source.Int4E.HasValue)
			{
				AdapterCommon.AddMetadata(p_metadata, "Int4E", p_source.Int4E.Value);
			}

			if (p_source.Int4F.HasValue)
			{
				AdapterCommon.AddMetadata(p_metadata, "Int4F", p_source.Int4F.Value);
			}

			if (p_source.Int50.HasValue)
			{
				AdapterCommon.AddMetadata(p_metadata, "Int50", p_source.Int50.Value);
			}
		}
	}
}
