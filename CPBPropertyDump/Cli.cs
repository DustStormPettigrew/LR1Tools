using System;
using System.IO;

namespace LR1Tools.CPBPropertyDump
{
	internal static class Cli
	{
		public static int Main(string[] p_args)
		{
			if (p_args.Length == 0 || p_args[0] == "--help" || p_args[0] == "-h") { PrintHelp(); return 0; }
			try
			{
				if (!p_args[0].Equals("dump", StringComparison.OrdinalIgnoreCase)) { PrintHelp(); return 1; }
				return Dump(p_args);
			}
			catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
		}

		private static int Dump(string[] p_args)
		{
			string inputDirectory = Value(p_args, "--input-dir");
			string inputFile = Value(p_args, "--input-file");
			if (string.IsNullOrWhiteSpace(inputDirectory) == string.IsNullOrWhiteSpace(inputFile))
				throw new ArgumentException("Specify exactly one of --input-dir or --input-file.");

			CpbPropertyDump dump;
			string input;
			if (!string.IsNullOrWhiteSpace(inputDirectory))
			{
				if (!Directory.Exists(inputDirectory)) throw new DirectoryNotFoundException("Input directory was not found: " + inputDirectory);
				dump = Tool.LoadDirectory(inputDirectory);
				input = Path.GetFullPath(inputDirectory);
			}
			else
			{
				if (!File.Exists(inputFile)) throw new FileNotFoundException("Input file was not found.", inputFile);
				dump = Tool.Load(inputFile);
				input = Path.GetFullPath(inputFile);
			}

			Console.Write(Tool.FormatReport(dump, input));
			string outputCsv = Value(p_args, "--output-csv");
			if (!string.IsNullOrWhiteSpace(outputCsv))
			{
				Tool.WriteCsv(dump, outputCsv);
				Console.WriteLine("CSV: " + Path.GetFullPath(outputCsv));
			}
			return 0;
		}

		private static string Value(string[] p_args, string p_name)
		{
			for (int i = 1; i + 1 < p_args.Length; i++)
				if (p_args[i].Equals(p_name, StringComparison.OrdinalIgnoreCase)) return p_args[i + 1];
			return null;
		}

		private static void PrintHelp()
		{
			Console.WriteLine("Usage:");
			Console.WriteLine("  LR1Tools.CPBPropertyDump dump --input-dir <dir> [--output-csv <file>]");
			Console.WriteLine("  LR1Tools.CPBPropertyDump dump --input-file <file> [--output-csv <file>]");
		}
	}
}
