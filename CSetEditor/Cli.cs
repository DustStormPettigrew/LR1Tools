using System;
using System.Collections.Generic;
using System.IO;
using LibLR1;

namespace LR1Tools.CSetEditor
{
	internal static class Cli
	{
		public static int Main(string[] p_args)
		{
			if (p_args.Length == 0 || p_args[0] == "--help" || p_args[0] == "-h") { PrintHelp(); return 0; }
			try
			{
				if (p_args[0].Equals("add-color", StringComparison.OrdinalIgnoreCase)) return AddColor(p_args);
				if (p_args[0].Equals("add-all-colors", StringComparison.OrdinalIgnoreCase)) return AddAllColors(p_args);
				PrintHelp();
				return 1;
			}
			catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
		}

		private static int AddColor(string[] p_args)
		{
			string csetPath = Value(p_args, "--cset-file");
			string outputPath = Value(p_args, "--output") ?? csetPath;
			string piece = Value(p_args, "--piece");
			List<string> colors = Values(p_args, "--color");
			if (string.IsNullOrWhiteSpace(csetPath) || string.IsNullOrWhiteSpace(piece) || colors.Count == 0) throw new ArgumentException("--cset-file, --piece, and at least one --color are required.");
			if (!File.Exists(csetPath)) throw new FileNotFoundException("CSET file was not found.", csetPath);
			CSet cset = Tool.Load(csetPath);
			if (!cset.ValidColorsByPiece.ContainsKey(piece)) throw new InvalidDataException("Piece is not present in this CSET: " + piece);
			string palettePath = Value(p_args, "--palette-file") ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(csetPath)), "L_COLORS.LEB");
			if (!File.Exists(palettePath)) throw new FileNotFoundException("L_COLORS.LEB must be beside the CSET file.", palettePath);
			LColors palette = new LColors(palettePath);
			int added = 0;
			foreach (string color in colors)
			{
				if (palette.IndexOf(color) < 0) throw new InvalidDataException("Unknown L_COLORS name: " + color);
				if (cset.ValidColorsByPiece[piece].Contains(color)) continue;
				cset.AddEntry(piece, color);
				added++;
			}
			if (added == 0) { Console.WriteLine("No changes; requested colors are already present."); return 0; }
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
			if (!File.Exists(outputPath + ".bak")) File.Copy(File.Exists(outputPath) ? outputPath : csetPath, outputPath + ".bak", false);
			using (FileStream output = File.Create(outputPath)) cset.Save(output);
			Console.WriteLine("Added {0} color entries; output: {1}", added, outputPath);
			return 0;
		}

		private static int AddAllColors(string[] p_args)
		{
			List<string> csetPaths = Values(p_args, "--cset-file");
			if (csetPaths.Count == 0) throw new ArgumentException("At least one --cset-file is required.");
			foreach (string csetPath in csetPaths)
				if (!File.Exists(csetPath)) throw new FileNotFoundException("CSET file was not found.", csetPath);

			string outputDirectory = Value(p_args, "--output-dir");
			string palettePath = Value(p_args, "--palette-file") ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(csetPaths[0])), "L_COLORS.LEB");
			if (!File.Exists(palettePath)) throw new FileNotFoundException("L_COLORS.LEB must be beside the first CSET file.", palettePath);
			LColors palette = new LColors(palettePath);
			int totalAdded = 0;
			int totalPieces = 0;

			foreach (string csetPath in csetPaths)
			{
				CSet cset = Tool.Load(csetPath);
				int added = 0;
				int pieces = 0;
				foreach (string piece in new List<string>(cset.ValidColorsByPiece.Keys))
				{
					HashSet<string> existingColors = cset.ValidColorsByPiece[piece];
					int addedForPiece = 0;
					foreach (string color in palette.Names)
					{
						if (existingColors.Contains(color)) continue;
						cset.AddEntry(piece, color);
						added++;
						addedForPiece++;
					}
					if (addedForPiece > 0) pieces++;
				}

				string outputPath = string.IsNullOrWhiteSpace(outputDirectory) ? csetPath : Path.Combine(outputDirectory, Path.GetFileName(csetPath));
				if (added > 0)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
					if (!File.Exists(outputPath + ".bak")) File.Copy(File.Exists(outputPath) ? outputPath : csetPath, outputPath + ".bak", false);
					using (FileStream output = File.Create(outputPath)) cset.Save(output);
				}
				Console.WriteLine("{0}: +{1} entries across {2} pieces", outputPath, added, pieces);
				totalAdded += added;
				totalPieces += pieces;
			}

			Console.WriteLine("Total: +{0} entries across {1} pieces", totalAdded, totalPieces);
			return 0;
		}

		private static string Value(string[] p_args, string p_name) { for (int i = 1; i + 1 < p_args.Length; i++) if (p_args[i].Equals(p_name, StringComparison.OrdinalIgnoreCase)) return p_args[i + 1]; return null; }
		private static List<string> Values(string[] p_args, string p_name) { List<string> values = new List<string>(); for (int i = 1; i + 1 < p_args.Length; i++) if (p_args[i].Equals(p_name, StringComparison.OrdinalIgnoreCase)) values.Add(p_args[i + 1]); return values; }
		private static void PrintHelp()
		{
			Console.WriteLine("Usage:");
			Console.WriteLine("  LR1Tools.CSetEditor add-color --cset-file <file> --piece <name> --color <color> [--color <color> ...] [--output <file>] [--palette-file <L_COLORS.LEB>]");
			Console.WriteLine("  LR1Tools.CSetEditor add-all-colors --cset-file <file> [--cset-file <file> ...] [--output-dir <dir>] [--palette-file <L_COLORS.LEB>]");
		}
	}
}
