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

			Console.WriteLine("Usage:");
			Console.WriteLine("  export-json <input> <output.json>");
			Console.WriteLine("  export-json <input1> <input2> <output.json>");
		}
	}
}

