using LR1Tools.Contracts;
using LibLR1;
using System.Collections.Generic;
using System.Globalization;

namespace LR1Tools.Adapters
{
	public static class PWBAdapter
	{
		public static List<TrackObject> ToPowerups(PWB p_source, string p_namePrefix = null)
		{
			List<TrackObject> output = new List<TrackObject>();
			string namePrefix = string.IsNullOrEmpty(p_namePrefix) ? "Powerup" : p_namePrefix;
			if (p_source == null)
			{
				return output;
			}

			List<PWB_ColorBrick> colorBricks = p_source.ColorBricks ?? new List<PWB_ColorBrick>();
			for (int i = 0; i < colorBricks.Count; i++)
			{
				TrackObject obj = CreateObject(string.Format(CultureInfo.InvariantCulture, "{0}.Color[{1}]", namePrefix, i));
				AdapterCommon.SetObjectProvenance(obj, "PWB", obj.Name, obj.Name, i.ToString(CultureInfo.InvariantCulture), i);
				obj.Transform.Position = AdapterCommon.ToVector3(colorBricks[i] != null ? colorBricks[i].Position : null);
				obj.Metadata["PowerupType"] = "ColorBrick";
				obj.Metadata["BrickColor"] = GetColorName(colorBricks[i] != null ? colorBricks[i].Color : (byte)0);
				obj.Metadata["BrickColorId"] = (colorBricks[i] != null ? colorBricks[i].Color : (byte)0).ToString(CultureInfo.InvariantCulture);
				output.Add(obj);
			}

			List<PWB_WhiteBrick> whiteBricks = p_source.WhiteBricks ?? new List<PWB_WhiteBrick>();
			for (int i = 0; i < whiteBricks.Count; i++)
			{
				TrackObject obj = CreateObject(string.Format(CultureInfo.InvariantCulture, "{0}.White[{1}]", namePrefix, i));
				AdapterCommon.SetObjectProvenance(obj, "PWB", obj.Name, obj.Name, i.ToString(CultureInfo.InvariantCulture), i);
				obj.Transform.Position = AdapterCommon.ToVector3(whiteBricks[i] != null ? whiteBricks[i].Position : null);
				obj.Metadata["PowerupType"] = "WhiteBrick";
				output.Add(obj);
			}

			return output;
		}

		private static TrackObject CreateObject(string p_name)
		{
			TrackObject obj = new TrackObject();
			obj.Name = p_name ?? string.Empty;
			AdapterCommon.SetObjectProvenance(obj, "PWB", obj.Name, obj.Name, obj.Name);
			obj.Metadata["NativeType"] = "Powerup";
			return obj;
		}

		private static string GetColorName(byte p_color)
		{
			switch (p_color)
			{
				case PWB.COLOR_RED:
					return "Red";
				case PWB.COLOR_YELLOW:
					return "Yellow";
				case PWB.COLOR_BLUE:
					return "Blue";
				case PWB.COLOR_GREEN:
					return "Green";
				default:
					return "Unknown";
			}
		}
	}
}
