using System;
using System.IO;

namespace LR1Tools.Shared
{
	public static class GamePathResolver
	{
		public static bool TryResolve(out string p_path)
		{
			p_path = Environment.GetEnvironmentVariable("LR1_INSTALLATION_PATH") ?? Environment.GetEnvironmentVariable("LEGO_RACERS_INSTALLATION_PATH");
			if (!string.IsNullOrWhiteSpace(p_path) && Directory.Exists(p_path)) return true;
			p_path = null;
			return false;
		}
	}
}
