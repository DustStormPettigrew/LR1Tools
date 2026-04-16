using LR1Tools.Contracts;
using LibLR1;
using System.Collections.Generic;
using System.Globalization;

namespace LR1Tools.Adapters
{
	public static class EMBAdapter
	{
		public static List<TrackObject> ToEmitters(EMB p_source, string p_namePrefix = null)
		{
			List<TrackObject> output = new List<TrackObject>();
			string namePrefix = string.IsNullOrEmpty(p_namePrefix) ? "Emitter" : p_namePrefix;
			if (p_source == null || p_source.Emitters == null)
			{
				return output;
			}

			foreach (KeyValuePair<string, EMB_Emitter> pair in p_source.Emitters)
			{
				EMB_Emitter emitter = pair.Value;
				if (emitter != null && emitter.Positions != null && emitter.Positions.Length > 0)
				{
					for (int i = 0; i < emitter.Positions.Length; i++)
					{
						TrackObject obj = CreateObject(string.Format(CultureInfo.InvariantCulture, "{0}.{1}[{2}]", namePrefix, pair.Key, i), emitter);
						AdapterCommon.SetObjectProvenance(obj, "EMB", obj.Name, pair.Key, i.ToString(CultureInfo.InvariantCulture), i);
						obj.Transform.Position = AdapterCommon.ToVector3(emitter.Positions[i]);
						obj.Metadata["EmitterName"] = pair.Key ?? string.Empty;
						obj.Metadata["EmitterPositionIndex"] = i.ToString(CultureInfo.InvariantCulture);
						output.Add(obj);
					}
					continue;
				}

				TrackObject fallback = CreateObject(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", namePrefix, pair.Key), emitter);
				AdapterCommon.SetObjectProvenance(fallback, "EMB", fallback.Name, pair.Key, pair.Key);
				fallback.Metadata["EmitterName"] = pair.Key ?? string.Empty;
				output.Add(fallback);
			}

			return output;
		}

		private static TrackObject CreateObject(string p_name, EMB_Emitter p_source)
		{
			TrackObject obj = new TrackObject();
			obj.Name = p_name ?? string.Empty;
			AdapterCommon.SetObjectProvenance(obj, "EMB", obj.Name, obj.Name, obj.Name);
			obj.Metadata["NativeType"] = "Emitter";

			if (p_source == null)
			{
				return obj;
			}

			obj.Metadata["Size"] = p_source.Size.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["Unknown29"] = p_source.Unknown29.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["Direction"] = AdapterCommon.FormatVector3(p_source.Direction);
			obj.Metadata["ScaleMin"] = p_source.ScaleMin.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["ScaleMax"] = p_source.ScaleMax.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["Lifetime"] = p_source.Lifetime.ToString(CultureInfo.InvariantCulture);
			obj.Metadata["Loop"] = p_source.Loop.ToString(CultureInfo.InvariantCulture);
			obj.Metadata["SpeedMin"] = p_source.SpeedMin.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["SpeedMax"] = p_source.SpeedMax.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["Range"] = p_source.Range.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["Texture"] = p_source.Texture ?? string.Empty;
			obj.Metadata["HasVariant"] = p_source.HasVariant ? "true" : "false";
			obj.Metadata["Variant"] = p_source.Variant.ToString(CultureInfo.InvariantCulture);
			obj.Metadata["HasColor"] = p_source.HasColor ? "true" : "false";
			obj.Metadata["Color"] = p_source.Color.ToString(CultureInfo.InvariantCulture);
			obj.Metadata["PositionCount"] = p_source.Positions != null ? p_source.Positions.Length.ToString(CultureInfo.InvariantCulture) : "0";
			return obj;
		}
	}
}
