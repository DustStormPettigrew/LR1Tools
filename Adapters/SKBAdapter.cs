using LR1Tools.Contracts;
using LibLR1;
using LibLR1.Utils;
using System.Collections.Generic;
using System.Globalization;

namespace LR1Tools.Adapters
{
	public static class SKBAdapter
	{
		public static TrackScene ToScene(SKB p_source, string p_sceneName = null)
		{
			TrackScene scene = new TrackScene();
			scene.ExportType = TrackSceneExportTypes.MaterialSet;
			scene.Name = string.IsNullOrEmpty(p_sceneName) ? "SKBScene" : p_sceneName;
			AdapterCommon.SetSceneProvenance(scene, "SKB", scene.Name);

			if (p_source == null)
			{
				return scene;
			}

			if (!string.IsNullOrEmpty(p_source.PreferredSet))
			{
				scene.Metadata["PreferredSet"] = p_source.PreferredSet;
			}

			if (p_source.UnknownFloat.HasValue)
			{
				scene.Metadata["UnknownFloat"] = p_source.UnknownFloat.Value.ToString("R", CultureInfo.InvariantCulture);
			}

			Dictionary<string, SKB_Gradient>[] gradientSets = p_source.Gradients ?? new Dictionary<string, SKB_Gradient>[0];
			for (int setIndex = 0; setIndex < gradientSets.Length; setIndex++)
			{
				Dictionary<string, SKB_Gradient> gradientSet = gradientSets[setIndex];
				if (gradientSet == null)
				{
					continue;
				}

				foreach (KeyValuePair<string, SKB_Gradient> pair in gradientSet)
				{
					string gradientName = string.Format(CultureInfo.InvariantCulture, "{0}@Set{1}", pair.Key, setIndex);
					TrackGradient gradient = ToGradient(pair.Value, gradientName, setIndex, pair.Key);
					TrackMaterial material = ToMaterial(pair.Value, gradientName, setIndex, pair.Key, p_source.PreferredSet);

					material.Gradients.Add(gradient);
					scene.Gradients.Add(gradient);
					scene.Materials.Add(material);
				}
			}

			scene.Metadata["GradientSetCount"] = gradientSets.Length.ToString(CultureInfo.InvariantCulture);
			return scene;
		}

		public static TrackMaterial ToMaterial(SKB_Gradient p_source, string p_materialName, int p_setIndex, string p_sourceKey, string p_preferredSet = null)
		{
			TrackMaterial material = new TrackMaterial();
			material.Name = p_materialName ?? string.Empty;
			material.DiffuseColor = SelectRepresentativeColor(p_source);
			AdapterCommon.SetMaterialProvenance(
				material,
				"SKB",
				material.Name,
				p_sourceKey,
				string.Format(CultureInfo.InvariantCulture, "{0}:{1}", p_setIndex, p_sourceKey ?? string.Empty),
				p_setIndex);
			material.Metadata["GradientSetIndex"] = p_setIndex.ToString(CultureInfo.InvariantCulture);
			material.Metadata["GradientKey"] = p_sourceKey ?? string.Empty;
			material.Metadata["UsesPreferredSetKey"] = string.Equals(p_sourceKey, p_preferredSet) ? "true" : "false";

			if (p_source != null && p_source.UnknownInt.HasValue)
			{
				material.Metadata["UnknownInt"] = p_source.UnknownInt.Value.ToString(CultureInfo.InvariantCulture);
			}

			return material;
		}

		public static TrackGradient ToGradient(SKB_Gradient p_source, string p_gradientName, int p_setIndex, string p_sourceKey)
		{
			TrackGradient gradient = new TrackGradient();
			gradient.Name = p_gradientName ?? string.Empty;
			AdapterCommon.SetGradientProvenance(
				gradient,
				"SKB",
				gradient.Name,
				p_sourceKey,
				string.Format(CultureInfo.InvariantCulture, "{0}:{1}", p_setIndex, p_sourceKey ?? string.Empty),
				p_setIndex);
			gradient.Metadata["GradientSetIndex"] = p_setIndex.ToString(CultureInfo.InvariantCulture);
			gradient.Metadata["GradientKey"] = p_sourceKey ?? string.Empty;

			if (p_source != null && p_source.UnknownInt.HasValue)
			{
				gradient.Metadata["UnknownInt"] = p_source.UnknownInt.Value.ToString(CultureInfo.InvariantCulture);
			}

			// SKB stores three ordered color slots but no explicit stop positions.
			AddStop(gradient, 0f, p_source != null ? p_source.Color1 : null, "Color1");
			AddStop(gradient, 0.5f, p_source != null ? p_source.Color2 : null, "Color2");
			AddStop(gradient, 1f, p_source != null ? p_source.Color3 : null, "Color3");
			return gradient;
		}

		private static void AddStop(TrackGradient p_gradient, float p_position, LRColor p_color, string p_slotName)
		{
			if (p_color == null)
			{
				return;
			}

			TrackGradientStop stop = new TrackGradientStop();
			stop.Position = p_position;
			stop.Color = AdapterCommon.ToTrackColor(p_color, false);
			stop.Metadata["SourceSlot"] = p_slotName;
			p_gradient.Stops.Add(stop);
		}

		private static TrackColor SelectRepresentativeColor(SKB_Gradient p_source)
		{
			if (p_source == null)
			{
				return new TrackColor(1f, 1f, 1f, 1f);
			}

			if (p_source.Color2 != null)
			{
				return AdapterCommon.ToTrackColor(p_source.Color2, false);
			}

			if (p_source.Color1 != null)
			{
				return AdapterCommon.ToTrackColor(p_source.Color1, false);
			}

			if (p_source.Color3 != null)
			{
				return AdapterCommon.ToTrackColor(p_source.Color3, false);
			}

			return new TrackColor(1f, 1f, 1f, 1f);
		}
	}
}


