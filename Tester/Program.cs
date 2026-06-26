using System;

namespace LR1Tools.Tester
{
	public class Program
	{
		public static void Main(string[] p_args)
		{
			if (p_args != null && p_args.Length > 0 && string.Equals(p_args[0], "export-json", StringComparison.OrdinalIgnoreCase))
			{
				ExportRunner.Run(p_args);
				return;
			}

			if (p_args != null && p_args.Length > 0 && string.Equals(p_args[0], "export-animation", StringComparison.OrdinalIgnoreCase))
			{
				AnimationExportRunner.Run(p_args);
				return;
			}

			if (p_args != null && p_args.Length == 3 && string.Equals(p_args[0], "extract-racer-assets", StringComparison.OrdinalIgnoreCase))
			{
				RacerAssets.RacerAssetManifest manifest = RacerAssets.RacerAssetExtractor.Extract(p_args[1], p_args[2]);
				Console.WriteLine("Extracted {0} assets, {1} CSET brick entries, and {2} palette names.", manifest.Assets.Count, manifest.LogicalBricks.Count, manifest.PaletteNames.Count);
				return;
			}

			Console.WriteLine("Usage:");
			Console.WriteLine("  export-json <input> <output.json> [--export-textures]");
			Console.WriteLine("  export-json <input1> <input2> ... <output.json> [--export-textures]");
			Console.WriteLine("  export-animation <input.MAB|input.ADB> <output.json>");
			Console.WriteLine("  extract-racer-assets <game-install> <output-directory>");
		}
	}
}

