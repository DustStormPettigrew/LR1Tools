using LR1Tools.Contracts;
using LibLR1;
using LibLR1.Utils;
using System.Collections.Generic;
using System.Globalization;

namespace LR1Tools.Adapters
{
	public static class SPBAdapter
	{
		public static List<TrackObject> ToStartPositions(SPB p_source, string p_namePrefix = null)
		{
			List<TrackObject> output = new List<TrackObject>();
			string namePrefix = string.IsNullOrEmpty(p_namePrefix) ? "StartPosition" : p_namePrefix;
			if (p_source == null || p_source.StartPositions == null)
			{
				return output;
			}

			foreach (KeyValuePair<int, SPB_StartPosition> pair in p_source.StartPositions)
			{
				TrackObject obj = CreateObject(string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", namePrefix, pair.Key));
				AdapterCommon.SetObjectProvenance(obj, "SPB", obj.Name, obj.Name, pair.Key.ToString(CultureInfo.InvariantCulture), pair.Key);
				obj.Transform.Position = AdapterCommon.ToVector3(pair.Value != null ? pair.Value.Position : null);

				if (pair.Value != null && pair.Value.Orientation != null && pair.Value.Orientation.Length >= 6)
				{
					LRVector3 forward = new LRVector3(pair.Value.Orientation[0], pair.Value.Orientation[1], pair.Value.Orientation[2]);
					LRVector3 up = new LRVector3(pair.Value.Orientation[3], pair.Value.Orientation[4], pair.Value.Orientation[5]);
					obj.Transform.Rotation = AdapterCommon.CreateRotationFromForwardUp(forward, up);
					obj.Metadata["OrientationForward"] = AdapterCommon.FormatVector3(forward);
					obj.Metadata["OrientationUp"] = AdapterCommon.FormatVector3(up);
				}

				obj.Metadata["StartIndex"] = pair.Key.ToString(CultureInfo.InvariantCulture);
				output.Add(obj);
			}

			return output;
		}

		private static TrackObject CreateObject(string p_name)
		{
			TrackObject obj = new TrackObject();
			obj.Name = p_name ?? string.Empty;
			AdapterCommon.SetObjectProvenance(obj, "SPB", obj.Name, obj.Name, obj.Name);
			obj.Metadata["NativeType"] = "StartPosition";
			return obj;
		}
	}
}
