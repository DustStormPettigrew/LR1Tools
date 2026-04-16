using LR1Tools.Contracts;
using LibLR1;
using LibLR1.Utils;
using System.Collections.Generic;
using System.Globalization;

namespace LR1Tools.Adapters
{
	public static class CPBAdapter
	{
		public static List<TrackObject> ToCheckpoints(CPB p_source, string p_namePrefix = null)
		{
			List<TrackObject> output = new List<TrackObject>();
			string namePrefix = string.IsNullOrEmpty(p_namePrefix) ? "Checkpoint" : p_namePrefix;
			CPB_Checkpoint[] checkpoints = p_source != null && p_source.Checkpoints != null ? p_source.Checkpoints : new CPB_Checkpoint[0];

			for (int i = 0; i < checkpoints.Length; i++)
			{
				CPB_Checkpoint checkpoint = checkpoints[i];
				TrackObject obj = new TrackObject();
				obj.Name = string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", namePrefix, i);
				AdapterCommon.SetObjectProvenance(obj, "CPB", obj.Name, obj.Name, i.ToString(CultureInfo.InvariantCulture), i);
				obj.Transform.Position = AdapterCommon.ToVector3(checkpoint != null ? checkpoint.Location : null);
				obj.Metadata["NativeType"] = "Checkpoint";

				if (checkpoint != null && checkpoint.Direction != null)
				{
					obj.Metadata["DirectionNormal"] = AdapterCommon.FormatVector3(checkpoint.Direction.Normal);
					obj.Metadata["DirectionUnknown"] = checkpoint.Direction.Unknown.ToString("R", CultureInfo.InvariantCulture);
				}

				if (checkpoint != null && checkpoint.Timing != null)
				{
					obj.Metadata["Timing.Unknown1"] = checkpoint.Timing.Unknown1.ToString(CultureInfo.InvariantCulture);
					obj.Metadata["Timing.Unknown2"] = checkpoint.Timing.Unknown2.ToString(CultureInfo.InvariantCulture);
					obj.Metadata["Timing.Unknown3"] = checkpoint.Timing.Unknown3.ToString(CultureInfo.InvariantCulture);
					obj.Metadata["Timing.Unknown4"] = checkpoint.Timing.Unknown4.ToString(CultureInfo.InvariantCulture);
				}

				output.Add(obj);
			}

			return output;
		}
	}
}
