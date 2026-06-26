using LR1Tools.RacerAssets;
using System;

namespace LR1RacerAssetExtractor
{
	internal static class Program
	{
		private static int Main(string[] p_args)
		{
			if (p_args == null || p_args.Length != 2)
			{
				Console.Error.WriteLine("Usage: LR1RacerAssetExtractor <game-install> <output-directory>");
				return 2;
			}

			try
			{
				RacerAssetManifest manifest = RacerAssetExtractor.Extract(p_args[0], p_args[1]);
				Console.WriteLine("Extracted {0} assets, {1} CSET brick entries, and {2} palette names.", manifest.Assets.Count, manifest.LogicalBricks.Count, manifest.PaletteNames.Count);
				return 0;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
				return 1;
			}
		}
	}
}
