using System;

namespace LR1Tools.BrickManifest
{
	internal static class Tool
	{
		public static int Main(string[] p_args)
		{
			if (p_args.Length == 0 || p_args[0] == "--help" || p_args[0] == "-h") { PrintHelp(); return 0; }
			if (!string.Equals(p_args[0], "generate", StringComparison.OrdinalIgnoreCase)) { PrintHelp(); return 1; }
			try { BrickManifestDocument manifest = Generator.Generate(new GeneratorOptions { GamePath = Value(p_args, "--game-path"), ExistingManifestPath = Value(p_args, "--existing-manifest"), OutputPath = Value(p_args, "--output") }); Console.WriteLine("Wrote {0} pieces and {1} sets.", manifest.Pieces.Count, manifest.Sets.Count); return 0; }
			catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
		}
		private static string Value(string[] p_args, string p_name) { for (int i = 1; i + 1 < p_args.Length; i++) if (p_args[i].Equals(p_name, StringComparison.OrdinalIgnoreCase)) return p_args[i + 1]; return null; }
		private static void PrintHelp() { Console.WriteLine("Usage: LR1Tools.BrickManifest generate --game-path <LR1 install> --output <brick_manifest.yaml> [--existing-manifest <path>]"); }
	}
}
