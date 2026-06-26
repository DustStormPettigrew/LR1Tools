using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LR1Tools.Shared
{
	public static class ConfigLoader
	{
		public static IReadOnlyDictionary<string, string> Load(string p_path)
		{
			if (p_path == null) throw new ArgumentNullException(nameof(p_path));
			string text = File.ReadAllText(p_path);
			if (Path.GetExtension(p_path).Equals(".json", StringComparison.OrdinalIgnoreCase))
			{
				Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				using (JsonDocument document = JsonDocument.Parse(text))
					foreach (JsonProperty property in document.RootElement.EnumerateObject()) result[property.Name] = property.Value.ToString();
				return result;
			}
			Dictionary<string, string> yaml = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (string line in File.ReadLines(p_path))
			{
				string trimmed = line.Trim(); int separator = trimmed.IndexOf(':');
				if (separator > 0 && !trimmed.StartsWith("#", StringComparison.Ordinal)) yaml[trimmed.Substring(0, separator).Trim()] = trimmed.Substring(separator + 1).Trim().Trim('"', '\'');
			}
			return yaml;
		}
	}
}
