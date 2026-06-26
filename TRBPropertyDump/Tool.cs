using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LibLR1;
using LibLR1.Exceptions;
using LibLR1.IO;
using LibLR1.Utils;

namespace LR1Tools.TRBPropertyDump
{
	public static class Tool
	{
		private const byte BlockUnknown27 = 0x27;
		private const byte Property29 = 0x29, Property2A = 0x2A, Property2B = 0x2B,
			Property2C = 0x2C, Property2D = 0x2D, Property2E = 0x2E, Property2F = 0x2F;

		public static TrbPropertyDump Load(string p_path)
		{
			if (p_path == null) throw new ArgumentNullException(nameof(p_path));
			TRB parsed = new TRB(p_path);
			List<TrbRecord> records = ReadInstrumented(Path.GetFullPath(p_path));
			int parsedCount = parsed.Unknown27 == null ? 0 : parsed.Unknown27.Length;
			if (parsedCount != records.Count)
				throw new InvalidDataException("Instrumented TRB record count disagrees with LibLR1.TRB for " + p_path + ".");
			return new TrbPropertyDump(new[] { Path.GetFullPath(p_path) }, records);
		}

		public static TrbPropertyDump LoadDirectory(string p_path)
		{
			if (p_path == null) throw new ArgumentNullException(nameof(p_path));
			List<string> files = Directory.EnumerateFiles(p_path, "*.TRB", SearchOption.AllDirectories)
				.Where(p_file => Path.GetExtension(p_file).Equals(".TRB", StringComparison.OrdinalIgnoreCase))
				.OrderBy(p_file => p_file, StringComparer.OrdinalIgnoreCase)
				.ToList();
			List<TrbRecord> records = new List<TrbRecord>();
			foreach (string file in files)
			{
				TrbPropertyDump dump = Load(file);
				records.AddRange(dump.Records);
			}
			return new TrbPropertyDump(files.Select(Path.GetFullPath).ToList(), records);
		}

		public static string FormatReport(TrbPropertyDump p_dump, string p_input)
		{
			if (p_dump == null) throw new ArgumentNullException(nameof(p_dump));
			StringBuilder output = new StringBuilder();
			List<string> environFiles = p_dump.Files.Where(p_file => IsFileNamed(p_file, "ENVIRON.TRB")).ToList();
			List<string> mainTrigFiles = p_dump.Files.Where(p_file => IsFileNamed(p_file, "MAINTRIG.TRB")).ToList();
			List<string> otherFiles = p_dump.Files.Where(p_file => !IsFileNamed(p_file, "ENVIRON.TRB") && !IsFileNamed(p_file, "MAINTRIG.TRB")).ToList();
			List<TrbRecord> environ = p_dump.Records.Where(p_record => IsFileNamed(p_record.FilePath, "ENVIRON.TRB")).ToList();
			List<TrbRecord> mainTrig = p_dump.Records.Where(p_record => IsFileNamed(p_record.FilePath, "MAINTRIG.TRB")).ToList();
			List<TrbRecord> other = p_dump.Records.Where(p_record => !IsFileNamed(p_record.FilePath, "ENVIRON.TRB") && !IsFileNamed(p_record.FilePath, "MAINTRIG.TRB")).ToList();

			output.AppendLine("=== TRB Property Dump ===");
			output.AppendLine("Input: " + p_input);
			output.AppendFormat(CultureInfo.InvariantCulture, "Files: {0} total ({1} ENVIRON, {2} MAINTRIG, {3} OTHER)\n", p_dump.Files.Count, environFiles.Count, mainTrigFiles.Count, otherFiles.Count);
			output.AppendFormat(CultureInfo.InvariantCulture, "Records: {0} total ({1} ENVIRON, {2} MAINTRIG, {3} OTHER)\n", p_dump.Records.Count, environ.Count, mainTrig.Count, other.Count);
			if (otherFiles.Count > 0)
				output.AppendLine("Other filenames: " + string.Join(", ", otherFiles.Select(Path.GetFileName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p_name => p_name, StringComparer.OrdinalIgnoreCase)));

			AppendBucket(output, "ALL", p_dump.Records);
			AppendBucket(output, "ENVIRON.TRB", environ);
			AppendBucket(output, "MAINTRIG.TRB", mainTrig);
			if (otherFiles.Count > 0) AppendBucket(output, "OTHER", other);
			return output.ToString();
		}

		public static void WriteCsv(TrbPropertyDump p_dump, string p_path)
		{
			if (p_dump == null) throw new ArgumentNullException(nameof(p_dump));
			if (p_path == null) throw new ArgumentNullException(nameof(p_path));
			string directory = Path.GetDirectoryName(Path.GetFullPath(p_path));
			if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
			using (StreamWriter writer = new StreamWriter(p_path, false, new UTF8Encoding(false)))
			{
				writer.WriteLine("FilePath,RecordIndex,HasPosition,PositionX,PositionY,PositionZ,HasRadius,Radius,HasInt2B,Int2B,HasName,Name,HasUnusedInt2E,UnusedInt2E,Bool2F");
				foreach (TrbRecord record in p_dump.Records)
				{
					string[] fields = new[]
					{
						record.FilePath, record.RecordIndex.ToString(CultureInfo.InvariantCulture), Bool(record.HasPosition),
						record.HasPosition ? Float(record.Position.X) : "", record.HasPosition ? Float(record.Position.Y) : "", record.HasPosition ? Float(record.Position.Z) : "",
						Bool(record.HasRadius), record.HasRadius ? Float(record.Radius) : "",
						Bool(record.HasInt2B), record.HasInt2B ? record.Int2B.ToString(CultureInfo.InvariantCulture) : "",
						Bool(record.HasName), record.HasName ? record.Name : "",
						Bool(record.HasUnusedInt2E), record.HasUnusedInt2E ? record.UnusedInt2E.ToString(CultureInfo.InvariantCulture) : "",
						record.Bool2F ? "true" : ""
					};
					writer.WriteLine(string.Join(",", fields.Select(Csv)));
				}
			}
		}

		private static List<TrbRecord> ReadInstrumented(string p_path)
		{
			List<TrbRecord> records = new List<TrbRecord>();
			using (LRBinaryReader reader = BinaryFileHelper.Decompress(p_path))
			{
				while (reader.BaseStream.Position < reader.BaseStream.Length)
				{
					byte blockId = reader.ReadByte();
					if (blockId != BlockUnknown27) throw new UnexpectedBlockException(blockId, reader.BaseStream.Position - 1);
					reader.Expect(Token.LeftBracket);
					int count = reader.ReadIntWithHeader();
					reader.Expect(Token.RightBracket);
					reader.Expect(Token.LeftCurly);
					for (int i = 0; i < count; i++)
					{
						reader.Expect((Token)BlockUnknown27);
						reader.Expect(Token.LeftCurly);
						records.Add(ReadRecord(reader, p_path, i));
						reader.Expect(Token.RightCurly);
					}
					reader.Expect(Token.RightCurly);
				}
			}
			return records;
		}

		private static TrbRecord ReadRecord(LRBinaryReader p_reader, string p_path, int p_recordIndex)
		{
			TrbRecord record = new TrbRecord { FilePath = p_path, RecordIndex = p_recordIndex };
			while (!p_reader.Next(Token.RightCurly))
			{
				byte propertyId = p_reader.ReadByte();
				switch (propertyId)
				{
					case Property29: record.HasPosition = true; record.Position = LRVector3.Read(p_reader); break;
					case Property2A: record.HasRadius = true; record.Radius = p_reader.ReadFloatWithHeader(); break;
					case Property2B: record.HasInt2B = true; record.Int2B = p_reader.ReadIntWithHeader(); break;
					case Property2C: break;
					case Property2D: record.HasName = true; record.Name = p_reader.ReadStringWithHeader(); break;
					case Property2E: record.HasUnusedInt2E = true; record.UnusedInt2E = p_reader.ReadIntWithHeader(); break;
					case Property2F: record.Bool2F = true; break;
					default: throw new UnexpectedPropertyException(propertyId, p_reader.BaseStream.Position - 1);
				}
			}
			return record;
		}

		private static void AppendBucket(StringBuilder p_output, string p_name, IEnumerable<TrbRecord> p_source)
		{
			List<TrbRecord> records = p_source.ToList();
			p_output.AppendLine();
			p_output.AppendFormat(CultureInfo.InvariantCulture, "=== Bucket: {0} ({1} records) ===\n\n", p_name, records.Count);
			AppendVector(p_output, records);
			AppendFloat(p_output, records);
			AppendInteger(p_output, "Int2B", records.Where(p_record => p_record.HasInt2B).Select(p_record => p_record.Int2B), records.Count);
			AppendString(p_output, records);
			AppendInteger(p_output, "UnusedInt2E", records.Where(p_record => p_record.HasUnusedInt2E).Select(p_record => p_record.UnusedInt2E), records.Count);
			p_output.AppendLine("Bool2F:");
			p_output.AppendFormat(CultureInfo.InvariantCulture, "  Presence: {0} / {1} records ({2}%)\n", records.Count(p_record => p_record.Bool2F), records.Count, Percent(records.Count(p_record => p_record.Bool2F), records.Count));
		}

		private static void AppendVector(StringBuilder p_output, List<TrbRecord> p_records)
		{
			List<LRVector3> values = p_records.Where(p_record => p_record.HasPosition).Select(p_record => p_record.Position).ToList();
			p_output.AppendLine("Position (HasPosition):");
			AppendPresence(p_output, values.Count, p_records.Count);
			if (values.Count == 0) { p_output.AppendLine("  No values present."); p_output.AppendLine(); return; }
			AppendAxis(p_output, "X", values.Select(p_value => p_value.X));
			AppendAxis(p_output, "Y", values.Select(p_value => p_value.Y));
			AppendAxis(p_output, "Z", values.Select(p_value => p_value.Z));
			List<double> magnitudes = values.Select(p_value => Math.Sqrt(p_value.X * p_value.X + p_value.Y * p_value.Y + p_value.Z * p_value.Z)).ToList();
			p_output.AppendFormat(CultureInfo.InvariantCulture, "  Magnitude: min={0}, max={1}\n", Float(magnitudes.Min()), Float(magnitudes.Max()));
			p_output.AppendFormat(CultureInfo.InvariantCulture, "  Distinct: {0}\n\n", values.Select(p_value => new VectorKey(p_value)).Distinct().Count());
		}

		private static void AppendFloat(StringBuilder p_output, List<TrbRecord> p_records)
		{
			List<float> values = p_records.Where(p_record => p_record.HasRadius).Select(p_record => p_record.Radius).ToList();
			p_output.AppendLine("Radius:");
			AppendPresence(p_output, values.Count, p_records.Count);
			if (values.Count == 0) { p_output.AppendLine("  No values present."); p_output.AppendLine(); return; }
			p_output.AppendFormat(CultureInfo.InvariantCulture, "  min={0}, max={1}, mean={2}\n", Float(values.Min()), Float(values.Max()), Float(values.Average(p_value => (double)p_value)));
			Dictionary<float, int> histogram = Histogram(values);
			p_output.AppendFormat(CultureInfo.InvariantCulture, "  Distinct: {0}\n", histogram.Count);
			if (histogram.Count <= 12) AppendHistogram(p_output, histogram.OrderByDescending(p_pair => p_pair.Value).ThenBy(p_pair => p_pair.Key), p_pair => Float(p_pair.Key), "  Histogram (by value):");
			p_output.AppendLine();
		}

		private static void AppendInteger(StringBuilder p_output, string p_name, IEnumerable<int> p_values, int p_total)
		{
			List<int> values = p_values.ToList();
			p_output.AppendLine(p_name + ":");
			AppendPresence(p_output, values.Count, p_total);
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

		private static void AppendString(StringBuilder p_output, List<TrbRecord> p_records)
		{
			List<string> values = p_records.Where(p_record => p_record.HasName).Select(p_record => p_record.Name).ToList();
			p_output.AppendLine("Name:");
			AppendPresence(p_output, values.Count, p_records.Count);
			if (values.Count == 0) { p_output.AppendLine("  No values present."); p_output.AppendLine(); return; }
			AppendHistogram(p_output, Histogram(values).OrderByDescending(p_pair => p_pair.Value).ThenBy(p_pair => p_pair.Key, StringComparer.Ordinal), p_pair => Quote(p_pair.Key), "  Histogram (by value):");
			p_output.AppendLine();
		}

		private static void AppendAxis(StringBuilder p_output, string p_name, IEnumerable<float> p_values)
		{
			List<float> values = p_values.ToList();
			p_output.AppendFormat(CultureInfo.InvariantCulture, "  {0}: min={1}, max={2}, mean={3}\n", p_name, Float(values.Min()), Float(values.Max()), Float(values.Average(p_value => (double)p_value)));
		}

		private static void AppendPresence(StringBuilder p_output, int p_present, int p_total)
		{
			p_output.AppendFormat(CultureInfo.InvariantCulture, "  Presence: {0} / {1} records ({2}%)\n", p_present, p_total, Percent(p_present, p_total));
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

		private static bool IsFileNamed(string p_path, string p_name) { return Path.GetFileName(p_path).Equals(p_name, StringComparison.OrdinalIgnoreCase); }
		private static string Percent(int p_present, int p_total) { return (p_total == 0 ? 0d : 100d * p_present / p_total).ToString("0.0", CultureInfo.InvariantCulture); }
		private static string Float(float p_value) { return p_value.ToString("0.###", CultureInfo.InvariantCulture); }
		private static string Float(double p_value) { return p_value.ToString("0.###", CultureInfo.InvariantCulture); }
		private static string Bool(bool p_value) { return p_value ? "true" : "false"; }
		private static string Csv(string p_value) { return "\"" + (p_value ?? "").Replace("\"", "\"\"") + "\""; }
		private static string Quote(string p_value) { return "\"" + (p_value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""; }

		private struct VectorKey : IEquatable<VectorKey>
		{
			private readonly int m_x, m_y, m_z;
			public VectorKey(LRVector3 p_value) { m_x = BitConverter.SingleToInt32Bits(p_value.X); m_y = BitConverter.SingleToInt32Bits(p_value.Y); m_z = BitConverter.SingleToInt32Bits(p_value.Z); }
			public bool Equals(VectorKey p_other) { return m_x == p_other.m_x && m_y == p_other.m_y && m_z == p_other.m_z; }
			public override bool Equals(object p_object) { return p_object is VectorKey && Equals((VectorKey)p_object); }
			public override int GetHashCode() { return HashCode.Combine(m_x, m_y, m_z); }
		}
	}

	public sealed class TrbPropertyDump
	{
		public IReadOnlyList<string> Files { get; private set; }
		public IReadOnlyList<TrbRecord> Records { get; private set; }
		internal TrbPropertyDump(IReadOnlyList<string> p_files, IReadOnlyList<TrbRecord> p_records) { Files = p_files; Records = p_records; }
	}

	public sealed class TrbRecord
	{
		public string FilePath;
		public int RecordIndex;
		public bool HasPosition;
		public LRVector3 Position;
		public bool HasRadius;
		public float Radius;
		public bool HasInt2B;
		public int Int2B;
		public bool HasName;
		public string Name;
		public bool HasUnusedInt2E;
		public int UnusedInt2E;
		public bool Bool2F;
	}
}
