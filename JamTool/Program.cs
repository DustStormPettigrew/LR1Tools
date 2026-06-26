using System;
using System.IO;
using LibLR1;

namespace LR1Tools.JamTool
{
	internal static class Program
	{
		private static int Main(string[] p_args)
		{
			if (p_args == null || p_args.Length == 0 || IsHelp(p_args[0]))
			{
				PrintHelp();
				return 0;
			}
			try
			{
				switch (p_args[0].ToLowerInvariant())
				{
					case "list": return List(p_args);
					case "extract": return Extract(p_args);
					case "build": return Build(p_args);
					case "replace": return Replace(p_args);
					default:
						Console.Error.WriteLine("Unknown command: " + p_args[0]);
						PrintHelp();
						return 2;
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
				return 1;
			}
		}

		private static int List(string[] p_args)
		{
			RequireArgumentCount(p_args, 2, "list <archive.jam>");
			JAM archive = new JAM(p_args[1]);
			Console.WriteLine("Directories: {0}", archive.Directories.Count);
			Console.WriteLine("Files: {0}", archive.Files.Count);
			foreach (JAMFile file in archive.Files)
			{
				Console.WriteLine("{0,10}  {1}", file.Size, file.Path);
			}
			return 0;
		}

		private static int Extract(string[] p_args)
		{
			RequireArgumentCount(p_args, 3, "extract <archive.jam> <output-directory> [--force]");
			JAM archive = new JAM(p_args[1]);
			archive.Extract(p_args[2], HasForce(p_args));
			Console.WriteLine("Extracted {0} files to {1}", archive.Files.Count, Path.GetFullPath(p_args[2]));
			return 0;
		}

		private static int Build(string[] p_args)
		{
			RequireArgumentCount(p_args, 3, "build <source-directory> <output.jam> [--force]");
			EnsureOutputDoesNotExist(p_args[2], HasForce(p_args));
			JAM archive = JAM.FromDirectory(p_args[1]);
			archive.Write(p_args[2]);
			Console.WriteLine("Built {0} files at {1}", archive.Files.Count, Path.GetFullPath(p_args[2]));
			return 0;
		}

		private static int Replace(string[] p_args)
		{
			RequireArgumentCount(p_args, 5, "replace <archive.jam> <entry-path> <source-file> <output.jam> [--force]");
			EnsureOutputDoesNotExist(p_args[4], HasForce(p_args));
			JAM archive = new JAM(p_args[1]);
			archive.ReplaceFile(p_args[2], File.ReadAllBytes(p_args[3]));
			archive.Write(p_args[4]);
			Console.WriteLine("Replaced {0} and wrote {1}", p_args[2], Path.GetFullPath(p_args[4]));
			return 0;
		}

		private static bool IsHelp(string p_value)
		{
			return p_value == "--help" || p_value == "-h" || p_value == "/?";
		}

		private static bool HasForce(string[] p_args)
		{
			for (int i = 0; i < p_args.Length; i++)
			{
				if (string.Equals(p_args[i], "--force", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		private static void EnsureOutputDoesNotExist(string p_path, bool p_force)
		{
			if (File.Exists(p_path) && !p_force)
			{
				throw new IOException("Output file already exists. Use --force to overwrite it: " + p_path);
			}
		}

		private static void RequireArgumentCount(string[] p_args, int p_requiredCount, string p_usage)
		{
			if (p_args.Length < p_requiredCount)
			{
				throw new ArgumentException("Usage: LR1Tools.JamTool " + p_usage);
			}
		}

		private static void PrintHelp()
		{
			Console.WriteLine("LR1Tools.JamTool - LEGO Racers LJAM archive utility");
			Console.WriteLine("Usage:");
			Console.WriteLine("  LR1Tools.JamTool list <archive.jam>");
			Console.WriteLine("  LR1Tools.JamTool extract <archive.jam> <output-directory> [--force]");
			Console.WriteLine("  LR1Tools.JamTool build <source-directory> <output.jam> [--force]");
			Console.WriteLine("  LR1Tools.JamTool replace <archive.jam> <entry-path> <source-file> <output.jam> [--force]");
		}
	}
}
