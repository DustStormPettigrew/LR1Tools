using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LibLR1;
using LibLR1.Utils;

namespace LR1Tools.CPBPropertyDump
{
	public static class Tool
	{
		public static CpbPropertyDump Load(string p_path)
		{
			if (p_path == null) throw new ArgumentNullException(nameof(p_path));
			string fullPath = Path.GetFullPath(p_path);
			CPB cpb = new CPB(fullPath);
			List<CpbRecord> records = new List<CpbRecord>();
			CPB_Checkpoint[] checkpoints = cpb.Checkpoints ?? new CPB_Checkpoint[0];
			for (int i = 0; i < checkpoints.Length; i++)
			{
				CPB_Checkpoint checkpoint = checkpoints[i];
				CPB_Checkpoint_Direction direction = checkpoint.Direction ?? new CPB_Checkpoint_Direction();
				CPB_Checkpoint_NextLinks nextLinks = checkpoint.NextLinks ?? new CPB_Checkpoint_NextLinks();
				records.Add(new CpbRecord
				{
					FilePath = fullPath,
					RecordIndex = i,
					Normal = direction.Normal ?? new LRVector3(),
					PlaneOffset = direction.PlaneOffset,
					NextPrimary = nextLinks.NextPrimary,
					NextAlternate1 = nextLinks.NextAlternate1,
					NextAlternate2 = nextLinks.NextAlternate2,
					UnusedNextLink = nextLinks.UnusedNextLink,
					Location = checkpoint.Location ?? new LRVector3()
				});
			}
			return new CpbPropertyDump(new[] { fullPath }, records);
		}

		public static CpbPropertyDump LoadDirectory(string p_path)
		{
			if (p_path == null) throw new ArgumentNullException(nameof(p_path));
			List<string> files = Directory.EnumerateFiles(p_path, "*.CPB", SearchOption.AllDirectories)
				.Where(p_file => Path.GetExtension(p_file).Equals(".CPB", StringComparison.OrdinalIgnoreCase))
				.OrderBy(p_file => p_file, StringComparer.OrdinalIgnoreCase)
				.ToList();
			List<CpbRecord> records = new List<CpbRecord>();
			foreach (string file in files)
			{
				CpbPropertyDump dump = Load(file);
				records.AddRange(dump.Records);
			}
			return new CpbPropertyDump(files.Select(Path.GetFullPath).ToList(), records);
		}

		public static string FormatReport(CpbPropertyDump p_dump, string p_input)
		{
			if (p_dump == null) throw new ArgumentNullException(nameof(p_dump));
			StringBuilder output = new StringBuilder();
			output.AppendLine("=== CPB Property Dump ===");
			output.AppendLine("Input: " + p_input);
			output.AppendFormat(CultureInfo.InvariantCulture, "Files: {0} total\n", p_dump.Files.Count);
			output.AppendFormat(CultureInfo.InvariantCulture, "Records: {0} total\n", p_dump.Records.Count);

			AppendBucket(output, "ALL", p_dump.Records);
			AppendPerFileSummary(output, p_dump, p_input);
			return output.ToString();
		}

		public static void WriteCsv(CpbPropertyDump p_dump, string p_path)
		{
			if (p_dump == null) throw new ArgumentNullException(nameof(p_dump));
			if (p_path == null) throw new ArgumentNullException(nameof(p_path));
			string directory = Path.GetDirectoryName(Path.GetFullPath(p_path));
			if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
			using (StreamWriter writer = new StreamWriter(p_path, false, new UTF8Encoding(false)))
			{
				writer.WriteLine("FilePath,RecordIndex,NormalX,NormalY,NormalZ,PlaneOffset,NextPrimary,NextAlternate1,NextAlternate2,UnusedNextLink,LocationX,LocationY,LocationZ");
				foreach (CpbRecord record in p_dump.Records)
				{
					string[] fields = new[]
					{
						record.FilePath,
						record.RecordIndex.ToString(CultureInfo.InvariantCulture),
						Float(record.Normal.X),
						Float(record.Normal.Y),
						Float(record.Normal.Z),
						Float(record.PlaneOffset),
						record.NextPrimary.ToString(CultureInfo.InvariantCulture),
						record.NextAlternate1.ToString(CultureInfo.InvariantCulture),
						record.NextAlternate2.ToString(CultureInfo.InvariantCulture),
						record.UnusedNextLink.ToString(CultureInfo.InvariantCulture),
						Float(record.Location.X),
						Float(record.Location.Y),
						Float(record.Location.Z)
					};
					writer.WriteLine(string.Join(",", fields.Select(Csv)));
				}
			}
		}

		private static void AppendBucket(StringBuilder p_output, string p_name, IEnumerable<CpbRecord> p_source)
		{
			List<CpbRecord> records = p_source.ToList();
			p_output.AppendLine();
			p_output.AppendFormat(CultureInfo.InvariantCulture, "=== Bucket: {0} ({1} records) ===\n\n", p_name, records.Count);
			AppendVector(p_output, "Normal", records.Select(p_record => p_record.Normal));
			AppendPlaneOffset(p_output, records.Select(p_record => p_record.PlaneOffset));
			AppendInteger(p_output, "NextPrimary", records.Select(p_record => p_record.NextPrimary));
			AppendInteger(p_output, "NextAlternate1", records.Select(p_record => p_record.NextAlternate1));
			AppendInteger(p_output, "NextAlternate2", records.Select(p_record => p_record.NextAlternate2));
			AppendInteger(p_output, "UnusedNextLink", records.Select(p_record => p_record.UnusedNextLink));
			AppendVector(p_output, "Location", records.Select(p_record => p_record.Location));
		}

		private static void AppendPerFileSummary(StringBuilder p_output, CpbPropertyDump p_dump, string p_input)
		{
			p_output.AppendLine("=== Per-file Summary ===");
			foreach (string file in p_dump.Files)
			{
				List<CpbRecord> records = p_dump.Records.Where(p_record => p_record.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase)).ToList();
				List<int> values = records.Select(p_record => p_record.NextPrimary).Distinct().OrderBy(p_value => p_value).ToList();
				p_output.AppendFormat(
					CultureInfo.InvariantCulture,
					"{0} ({1} records): NextPrimary values = {{{2}}}\n",
					DisplayPath(file, p_input),
					records.Count,
					string.Join(", ", values.Select(p_value => p_value.ToString(CultureInfo.InvariantCulture))));
			}
		}

		private static void AppendVector(StringBuilder p_output, string p_name, IEnumerable<LRVector3> p_values)
		{
			List<LRVector3> values = p_values.ToList();
			p_output.AppendLine(p_name + ":");
			if (values.Count == 0) { p_output.AppendLine("  No values present."); p_output.AppendLine(); return; }
			AppendAxis(p_output, "X", values.Select(p_value => p_value.X));
			AppendAxis(p_output, "Y", values.Select(p_value => p_value.Y));
			AppendAxis(p_output, "Z", values.Select(p_value => p_value.Z));
			List<double> magnitudes = values.Select(p_value => Math.Sqrt(p_value.X * p_value.X + p_value.Y * p_value.Y + p_value.Z * p_value.Z)).ToList();
			p_output.AppendFormat(CultureInfo.InvariantCulture, "  Magnitude: min={0}, max={1}\n", Float(magnitudes.Min()), Float(magnitudes.Max()));
			p_output.AppendFormat(CultureInfo.InvariantCulture, "  Distinct: {0}\n\n", values.Select(p_value => new VectorKey(p_value)).Distinct().Count());
		}

		private static void AppendPlaneOffset(StringBuilder p_output, IEnumerable<float> p_values)
		{
			List<float> values = p_values.ToList();
			p_output.AppendLine("PlaneOffset:");
			if (values.Count == 0) { p_output.AppendLine("  No values present."); p_output.AppendLine(); return; }
			Dictionary<float, int> histogram = Histogram(values);
			p_output.AppendFormat(CultureInfo.InvariantCulture, "  min={0}, max={1}, mean={2}, distinct={3}\n", Float(values.Min()), Float(values.Max()), Float(values.Average(p_value => (double)p_value)), histogram.Count);
			if (histogram.Count <= 12)
				AppendHistogram(p_output, histogram.OrderBy(p_pair => p_pair.Key), p_pair => Float(p_pair.Key), "  Histogram (by value):");
			p_output.AppendLine();
		}

		private static void AppendInteger(StringBuilder p_output, string p_name, IEnumerable<int> p_values)
		{
			List<int> values = p_values.ToList();
			p_output.AppendLine(p_name + ":");
			if (values.Count == 0) { p_output.AppendLine("  No values present."); p_output.AppendLine(); return; }
			Dictionary<int, int> histogram = Histogram(values);
			if (histogram.Count > 30)
			{
				AppendHistogram(p_output, histogram.OrderByDescending(p_pair => p_pair.Value).ThenBy(p_pair => p_pair.Key).Take(20), p_pair => p_pair.Key.ToString(CultureInfo.InvariantCulture), "  Histogram (top 20 by count):");
				p_output.AppendFormat(CultureInfo.InvariantCulture, "  [+{0} more distinct values]\n", histogram.Count - 20);
			}
			else AppendHistogram(p_output, histogram.OrderBy(p_pair => p_pair.Key), p_pair => p_pair.Key.ToString(CultureInfo.InvariantCulture), "  Histogram (by value):");
			p_output.AppendLine();
		}

		private static void AppendAxis(StringBuilder p_output, string p_name, IEnumerable<float> p_values)
		{
			List<float> values = p_values.ToList();
			p_output.AppendFormat(CultureInfo.InvariantCulture, "  {0}: min={1}, max={2}, mean={3}\n", p_name, Float(values.Min()), Float(values.Max()), Float(values.Average(p_value => (double)p_value)));
		}

		private static void AppendHistogram<T>(StringBuilder p_output, IEnumerable<KeyValuePair<T, int>> p_entries, Func<KeyValuePair<T, int>, string> p_formatKey, string p_header)
		{
			p_output.AppendLine(p_header);
			foreach (KeyValuePair<T, int> entry in p_entries)
				p_output.AppendLine("    " + p_formatKey(entry) + ": " + entry.Value.ToString(CultureInfo.InvariantCulture));
		}

		private static Dictionary<T, int> Histogram<T>(IEnumerable<T> p_values)
		{
			Dictionary<T, int> output = new Dictionary<T, int>();
			foreach (T value in p_values)
			{
				int count;
				output.TryGetValue(value, out count);
				output[value] = count + 1;
			}
			return output;
		}

		private static string DisplayPath(string p_file, string p_input)
		{
			if (!string.IsNullOrEmpty(p_input) && Directory.Exists(p_input))
				return Path.GetRelativePath(p_input, p_file);
			if (!string.IsNullOrEmpty(p_input) && File.Exists(p_input) && Path.GetFullPath(p_input).Equals(Path.GetFullPath(p_file), StringComparison.OrdinalIgnoreCase))
				return Path.GetFileName(p_file);
			return p_file;
		}

		private static string Float(float p_value) { return p_value.ToString("0.###", CultureInfo.InvariantCulture); }
		private static string Float(double p_value) { return p_value.ToString("0.###", CultureInfo.InvariantCulture); }
		private static string Csv(string p_value) { return "\"" + (p_value ?? "").Replace("\"", "\"\"") + "\""; }

		private struct VectorKey : IEquatable<VectorKey>
		{
			private readonly int m_x, m_y, m_z;
			public VectorKey(LRVector3 p_value) { m_x = BitConverter.SingleToInt32Bits(p_value.X); m_y = BitConverter.SingleToInt32Bits(p_value.Y); m_z = BitConverter.SingleToInt32Bits(p_value.Z); }
			public bool Equals(VectorKey p_other) { return m_x == p_other.m_x && m_y == p_other.m_y && m_z == p_other.m_z; }
			public override bool Equals(object p_object) { return p_object is VectorKey && Equals((VectorKey)p_object); }
			public override int GetHashCode() { return HashCode.Combine(m_x, m_y, m_z); }
		}
	}

	public sealed class CpbPropertyDump
	{
		public IReadOnlyList<string> Files { get; private set; }
		public IReadOnlyList<CpbRecord> Records { get; private set; }
		internal CpbPropertyDump(IReadOnlyList<string> p_files, IReadOnlyList<CpbRecord> p_records) { Files = p_files; Records = p_records; }
	}

	public sealed class CpbRecord
	{
		public string FilePath;
		public int RecordIndex;
		public LRVector3 Normal;
		public float PlaneOffset;
		public int NextPrimary;
		public int NextAlternate1;
		public int NextAlternate2;
		public int UnusedNextLink;
		public LRVector3 Location;
	}
}
