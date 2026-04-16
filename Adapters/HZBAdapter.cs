using LR1Tools.Contracts;
using LibLR1;
using LibLR1.Utils;
using System.Collections.Generic;
using System.Globalization;

namespace LR1Tools.Adapters
{
	public static class HZBAdapter
	{
		public static List<TrackObject> ToHazards(HZB p_source, string p_namePrefix = null)
		{
			List<TrackObject> output = new List<TrackObject>();
			string namePrefix = string.IsNullOrEmpty(p_namePrefix) ? "Hazard" : p_namePrefix;
			List<HZB_Entry> entries = p_source != null && p_source.Entries != null ? p_source.Entries : new List<HZB_Entry>();

			for (int i = 0; i < entries.Count; i++)
			{
				HZB_Entry entry = entries[i];
				TrackObject obj = new TrackObject();
				obj.Name = string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", namePrefix, i);
				AdapterCommon.SetObjectProvenance(obj, "HZB", obj.Name, obj.Name, i.ToString(CultureInfo.InvariantCulture), i);
				obj.Metadata["NativeType"] = "Hazard";
				obj.Metadata["HazardType"] = string.Format(CultureInfo.InvariantCulture, "0x{0:X2}", entry != null ? entry.Type : (byte)0);

				ApplyRepresentativeTransform(obj, entry);
				AddHazardMetadata(obj.Metadata, entry);
				output.Add(obj);
			}

			return output;
		}

		private static void ApplyRepresentativeTransform(TrackObject p_object, HZB_Entry p_entry)
		{
			if (p_entry == null)
			{
				return;
			}

			if (p_entry.PathData != null)
			{
				p_object.Transform.Position = AdapterCommon.ToVector3(p_entry.PathData.Position1);
				return;
			}

			if (p_entry.WaterZoneData != null && p_entry.WaterZoneData.Path != null)
			{
				p_object.Transform.Position = AdapterCommon.ToVector3(p_entry.WaterZoneData.Path.Position1);
				return;
			}

			if (p_entry.SpinningData != null && p_entry.SpinningData.HasPosition)
			{
				p_object.Transform.Position = AdapterCommon.ToVector3(p_entry.SpinningData.Position);
				return;
			}
		}

		private static void AddHazardMetadata(Dictionary<string, string> p_metadata, HZB_Entry p_entry)
		{
			if (p_entry == null)
			{
				return;
			}

			AdapterCommon.AddMetadata(p_metadata, "SurfaceModel", p_entry.SurfaceModel);
			AdapterCommon.AddMetadata(p_metadata, "SurfaceParam1", p_entry.SurfaceParam1);
			AdapterCommon.AddMetadata(p_metadata, "SurfaceParam2", p_entry.SurfaceParam2);
			AdapterCommon.AddMetadata(p_metadata, "RotatingModel", p_entry.RotatingModel);
			AdapterCommon.AddMetadata(p_metadata, "RotatingCheckpoint", p_entry.RotatingCheckpoint);
			AdapterCommon.AddMetadata(p_metadata, "RotatingParam1", p_entry.RotatingParam1);
			AdapterCommon.AddMetadata(p_metadata, "RotatingParam2", p_entry.RotatingParam2);
			AdapterCommon.AddMetadata(p_metadata, "RotatingParam3", p_entry.RotatingParam3);
			AdapterCommon.AddMetadata(p_metadata, "RotatingParam4", p_entry.RotatingParam4);
			AdapterCommon.AddMetadata(p_metadata, "DestructibleCollision", p_entry.DestructibleCollision);
			AdapterCommon.AddMetadata(p_metadata, "DestructibleCheckpoint", p_entry.DestructibleCheckpoint);
			AdapterCommon.AddMetadata(p_metadata, "AnimatedModel", p_entry.AnimatedModel);
			AdapterCommon.AddMetadata(p_metadata, "AnimatedCheckpoint", p_entry.AnimatedCheckpoint);
			AdapterCommon.AddMetadata(p_metadata, "AnimatedDuration", p_entry.AnimatedDuration);
			AdapterCommon.AddMetadata(p_metadata, "FlyingCheckpoint", p_entry.FlyingCheckpoint);
			AdapterCommon.AddMetadata(p_metadata, "FlyingModel", p_entry.FlyingModel);
			AdapterCommon.AddMetadata(p_metadata, "FlyingParam1", p_entry.FlyingParam1);
			AdapterCommon.AddMetadata(p_metadata, "FlyingParam2", p_entry.FlyingParam2);
			AdapterCommon.AddMetadata(p_metadata, "FlyingParam3", p_entry.FlyingParam3);

			if (p_entry.DestructibleModels != null)
			{
				p_metadata["DestructibleModel.Count"] = p_entry.DestructibleModels.Count.ToString(CultureInfo.InvariantCulture);
				for (int i = 0; i < p_entry.DestructibleModels.Count; i++)
				{
					AdapterCommon.AddMetadata(p_metadata, string.Format(CultureInfo.InvariantCulture, "DestructibleModel[{0}]", i), p_entry.DestructibleModels[i]);
				}
			}

			AddPathMetadata(p_metadata, "Path", p_entry.PathData);
			if (p_entry.WaterZoneData != null)
			{
				AddPathMetadata(p_metadata, "WaterZone.Path", p_entry.WaterZoneData.Path);
				AddWaterZoneVerticesMetadata(p_metadata, "WaterZone.Vertices1", p_entry.WaterZoneData.Vertices1);
				AddWaterZoneVerticesMetadata(p_metadata, "WaterZone.Vertices2", p_entry.WaterZoneData.Vertices2);
			}

			if (p_entry.SpinningData != null)
			{
				AdapterCommon.AddMetadata(p_metadata, "SpinningCheckpoint", p_entry.SpinningData.Checkpoint);
				AdapterCommon.AddMetadata(p_metadata, "SpinningHasPosition", p_entry.SpinningData.HasPosition);
				AdapterCommon.AddMetadata(p_metadata, "SpinningPosition", AdapterCommon.FormatVector3(p_entry.SpinningData.Position));
				AdapterCommon.AddMetadata(p_metadata, "SpinningModel", p_entry.SpinningData.Model);
				AdapterCommon.AddMetadata(p_metadata, "SpinningDuration", p_entry.SpinningData.Duration);
				AdapterCommon.AddMetadata(p_metadata, "SpinningRotation", AdapterCommon.FormatVector3(p_entry.SpinningData.Rotation));
			}
		}

		private static void AddPathMetadata(Dictionary<string, string> p_metadata, string p_prefix, HZB_PathHazard p_path)
		{
			if (p_path == null)
			{
				return;
			}

			AdapterCommon.AddMetadata(p_metadata, p_prefix + ".Position1", AdapterCommon.FormatVector3(p_path.Position1));
			AdapterCommon.AddMetadata(p_metadata, p_prefix + ".HasPosition2", p_path.HasPosition2);
			AdapterCommon.AddMetadata(p_metadata, p_prefix + ".Position2", AdapterCommon.FormatVector3(p_path.Position2));
			AdapterCommon.AddMetadata(p_metadata, p_prefix + ".HasPosition3", p_path.HasPosition3);
			AdapterCommon.AddMetadata(p_metadata, p_prefix + ".Position3", AdapterCommon.FormatVector3(p_path.Position3));
			AdapterCommon.AddMetadata(p_metadata, p_prefix + ".Radius", p_path.Radius);
			AdapterCommon.AddMetadata(p_metadata, p_prefix + ".Checkpoint", p_path.Checkpoint);
		}

		private static void AddWaterZoneVerticesMetadata(Dictionary<string, string> p_metadata, string p_prefix, HZB_Vertex[] p_vertices)
		{
			HZB_Vertex[] vertices = p_vertices ?? new HZB_Vertex[0];
			p_metadata[p_prefix + ".Count"] = vertices.Length.ToString(CultureInfo.InvariantCulture);
			for (int i = 0; i < vertices.Length; i++)
			{
				p_metadata[string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", p_prefix, i)] = string.Format(
					CultureInfo.InvariantCulture,
					"{0:R},{1:R},{2:R},{3}",
					vertices[i].X,
					vertices[i].Y,
					vertices[i].Z,
					vertices[i].Index
				);
			}
		}
	}
}
