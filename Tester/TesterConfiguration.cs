using System;
using System.IO;
using System.Text.Json;

namespace LR1Tools.Tester
{
	internal static class TesterConfiguration
	{
		private static readonly string[] ms_installationPathEnvVars = new string[]
		{
			"LR1_INSTALLATION_PATH",
			"LEGO_RACERS_INSTALLATION_PATH"
		};

		public static bool TryGetInstallationPath(out string p_installationPath, out string p_error)
		{
			foreach (string envVarName in ms_installationPathEnvVars)
			{
				string envValue = Environment.GetEnvironmentVariable(envVarName);
				if (TryNormalizeExistingDirectory(envValue, out p_installationPath))
				{
					p_error = null;
					return true;
				}
			}

			LocalSettings settings = LoadJson<LocalSettings>("locals.json");
			if (settings != null && TryNormalizeExistingDirectory(settings.InstallationPath, out p_installationPath))
			{
				p_error = null;
				return true;
			}

			p_installationPath = null;
			p_error =
				"Game installation path is not configured. " +
				"Set LR1_INSTALLATION_PATH or LEGO_RACERS_INSTALLATION_PATH, " +
				"or create locals.json with { \"InstallationPath\": \"C:\\\\Path\\\\To\\\\Game\" }.";
			return false;
		}

		public static T LoadJson<T>(string p_fileName) where T : class
		{
			string path = ResolveTesterFile(p_fileName);
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				return null;
			}

			return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
		}

		private static bool TryNormalizeExistingDirectory(string p_path, out string p_normalizedPath)
		{
			p_normalizedPath = null;
			if (string.IsNullOrWhiteSpace(p_path))
			{
				return false;
			}

			string fullPath = Path.GetFullPath(p_path);
			if (!Directory.Exists(fullPath))
			{
				return false;
			}

			p_normalizedPath = fullPath;
			return true;
		}

		private static string ResolveTesterFile(string p_relativePath)
		{
			string[] candidates = new string[]
			{
				Path.Combine(Directory.GetCurrentDirectory(), p_relativePath),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p_relativePath),
				Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\", p_relativePath)),
				Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\", p_relativePath))
			};

			for (int i = 0; i < candidates.Length; i++)
			{
				if (File.Exists(candidates[i]))
				{
					return candidates[i];
				}
			}

			return candidates[0];
		}

		private sealed class LocalSettings
		{
			public string InstallationPath { get; set; }
		}
	}
}
