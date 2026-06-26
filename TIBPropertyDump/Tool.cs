using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LibLR1.IO;
using LibLR1.Utils;

namespace LR1Tools.TIBPropertyDump
{
	public static class Tool
	{
		private const byte BlockUnknown27 = 0x27;
		private const byte Property28 = 0x28;
		private const byte Property29 = 0x29;
		private const byte Property2A = 0x2A;
		private const byte Property2D = 0x2D;
		private const byte SubMarker2B = 0x2B;

		public static TibPropertyDump Load(string p_path)
		{
			if (p_path == null) throw new ArgumentNullException(nameof(p_path));
			string fullPath = Path.GetFullPath(p_path);
			List<TibRecord> records = new List<TibRecord>();
			List<string> anomalies = new List<string>();
			using (LRBinaryReader reader = BinaryFileHelper.Decompress(fullPath))
			{
				ReadFile(reader, fullPath, records, anomalies);
			}
			return new TibPropertyDump(new[] { fullPath }, records, anomalies);
		}

		public static TibPropertyDump LoadDirectory(string p_path)
		{
			if (p_path == null) throw new ArgumentNullException(nameof(p_path));
			List<string> files = Directory.EnumerateFiles(p_path, "*.TIB", SearchOption.AllDirectories)
				.Where(p_file => Path.GetExtension(p_file).Equals(".TIB", StringComparison.OrdinalIgnoreCase))
				.OrderBy(p_file => p_file, StringComparer.OrdinalIgnoreCase)
				.ToList();
			List<TibRecord> records = new List<TibRecord>();
			List<string> anomalies = new List<string>();
			foreach (string file in files)
			{
				TibPropertyDump dump = Load(file);
				records.AddRange(dump.Records);
				anomalies.AddRange(dump.Anomalies);
			}
			return new TibPropertyDump(files.Select(Path.GetFullPath).ToList(), records, anomalies);
		}

		private static void ReadFile(LRBinaryReader p_reader, string p_path, List<TibRecord> p_records, List<string> p_anomalies)
		{
			while (p_reader.BaseStream.Position < p_reader.BaseStream.Length)
			{
				long blockOffset = p_reader.BaseStream.Position;
				byte blockId = p_reader.ReadByte();
				if (blockId != BlockUnknown27)
				{
					p_anomalies.Add(string.Format(CultureInfo.InvariantCulture, "{0}@{1:X}: unexpected block 0x{2:X2}", p_path, blockOffset, blockId));
					return;
				}
				p_reader.Expect(Token.LeftBracket);
				int recordCount = p_reader.ReadIntWithHeader();
				p_reader.Expect(Token.RightBracket);
				p_reader.Expect(Token.LeftCurly);
				for (int i = 0; i < recordCount; i++)
				{
					long recordOffset = p_reader.BaseStream.Position;
					byte recordId = p_reader.ReadByte();
					if (recordId != BlockUnknown27)
					{
						p_anomalies.Add(string.Format(CultureInfo.InvariantCulture, "{0}@{1:X}: unexpected record marker 0x{2:X2} for record {3}", p_path, recordOffset, recordId, i));
						SkipToToken(p_reader, Token.RightCurly);
						continue;
					}
					ReadRecord(p_reader, p_path, i, p_records, p_anomalies);
				}
				p_reader.Expect(Token.RightCurly);
			}
		}

		private static void ReadRecord(LRBinaryReader p_reader, string p_path, int p_index, List<TibRecord> p_records, List<string> p_anomalies)
		{
			p_reader.Expect(Token.LeftCurly);
			TibRecord record = new TibRecord { FilePath = p_path, RecordIndex = p_index };
			bool terminatorConsumed = false;
			while (!p_reader.Next(Token.RightCurly))
			{
				long propOffset = p_reader.BaseStream.Position;
				byte propertyId = p_reader.ReadByte();
				switch (propertyId)
				{
					case Property28:
						ReadOptionalProperty(p_reader, ref record.HasInt28, ref record.Int28Explicit, ref record.HasInt28Value, ref record.Int28);
						break;
					case Property29:
						ReadOptionalProperty(p_reader, ref record.HasInt29, ref record.Int29Explicit, ref record.HasInt29Value, ref record.Int29);
						break;
					case Property2A:
						record.HasInt2A = true;
						record.Int2A = p_reader.ReadIntWithHeader();
						break;
					case Property2D:
						record.HasInt2D = true;
						record.Int2D = p_reader.ReadIntWithHeader();
						break;
					default:
						p_anomalies.Add(string.Format(CultureInfo.InvariantCulture, "{0}@{1:X}: unexpected property 0x{2:X2} in record {3}", p_path, propOffset, propertyId, p_index));
						SkipToToken(p_reader, Token.RightCurly);
						terminatorConsumed = true;
						break;
				}
				if (terminatorConsumed) break;
			}
			if (!terminatorConsumed) p_reader.Expect(Token.RightCurly);
			p_records.Add(record);
		}

		private static void ReadOptionalProperty(LRBinaryReader p_reader, ref bool p_hasField, ref bool p_explicit, ref bool p_hasValue, ref int p_value)
		{
			p_hasField = true;
			long peekOffset = p_reader.BaseStream.Position;
			byte next = p_reader.ReadByte();
			if (next == SubMarker2B)
			{
				p_explicit = true;
				p_hasValue = true;
				p_value = p_reader.ReadIntWithHeader();
			}
			else if ((Token)next == Token.Int32)
			{
				p_hasValue = true;
				p_value = p_reader.ReadInt();
			}
			else
			{
				p_reader.BaseStream.Position = peekOffset;
			}
		}

		private static void SkipToToken(LRBinaryReader p_reader, Token p_token)
		{
			while (p_reader.BaseStream.Position < p_reader.BaseStream.Length)
			{
				if ((Token)p_reader.ReadByte() == p_token) return;
			}
		}

		public static string FormatReport(TibPropertyDump p_dump, string p_input)
		{
			if (p_dump == null) throw new ArgumentNullException(nameof(p_dump));
			StringBuilder output = new StringBuilder();
			output.AppendLine("=== TIB Property Dump ===");
			output.AppendLine("Input: " + p_input);
			output.AppendFormat(CultureInfo.InvariantCulture, "Files: {0} total\n", p_dump.Files.Count);
			output.AppendFormat(CultureInfo.InvariantCulture, "Records: {0} total\n", p_dump.Records.Count);
			output.AppendFormat(CultureInfo.InvariantCulture, "Anomalies: {0}\n", p_dump.Anomalies.Count);

			AppendPresence(output, "ALL", p_dump.Records);
			AppendPerFileSummary(output, p_dump, p_input);
			AppendAnomalies(output, p_dump.Anomalies);
			return output.ToString();
		}

		public static void WriteCsv(TibPropertyDump p_dump, string p_path)
		{
			if (p_dump == null) throw new ArgumentNullException(nameof(p_dump));
			if (p_path == null) throw new ArgumentNullException(nameof(p_path));
			string directory = Path.GetDirectoryName(Path.GetFullPath(p_path));
			if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
			using (StreamWriter writer = new StreamWriter(p_path, false, new UTF8Encoding(false)))
			{
				writer.WriteLine("FilePath,RecordIndex,HasInt28,Int28Explicit,Int28,HasInt29,Int29Explicit,Int29,HasInt2A,Int2A,HasInt2D,Int2D");
				foreach (TibRecord record in p_dump.Records)
				{
					string[] fields = new[]
					{
						record.FilePath,
						record.RecordIndex.ToString(CultureInfo.InvariantCulture),
						record.HasInt28 ? "true" : "false",
						record.Int28Explicit ? "true" : "false",
						record.Int28.ToString(CultureInfo.InvariantCulture),
						record.HasInt29 ? "true" : "false",
						record.Int29Explicit ? "true" : "false",
						record.Int29.ToString(CultureInfo.InvariantCulture),
						record.HasInt2A ? "true" : "false",
						record.Int2A.ToString(CultureInfo.InvariantCulture),
						record.HasInt2D ? "true" : "false",
						record.Int2D.ToString(CultureInfo.InvariantCulture)
					};
					writer.WriteLine(string.Join(",", fields.Select(Csv)));
				}
			}
		}

		private static void AppendPresence(StringBuilder p_output, string p_name, IEnumerable<TibRecord> p_source)
		{
			List<TibRecord> records = p_source.ToList();
			p_output.AppendLine();
			p_output.AppendFormat(CultureInfo.InvariantCulture, "=== Bucket: {0} ({1} records) ===\n\n", p_name, records.Count);
			AppendFieldStats(p_output, "Int28", records, p_record => p_record.HasInt28, p_record => p_record.Int28Explicit, p_record => p_record.HasInt28Value, p_record => p_record.Int28);
			AppendFieldStats(p_output, "Int29", records, p_record => p_record.HasInt29, p_record => p_record.Int29Explicit, p_record => p_record.HasInt29Value, p_record => p_record.Int29);
			AppendFieldStats(p_output, "Int2A", records, p_record => p_record.HasInt2A, p_record => true, p_record => p_record.HasInt2A, p_record => p_record.Int2A);
			AppendFieldStats(p_output, "Int2D", records, p_record => p_record.HasInt2D, p_record => true, p_record => p_record.HasInt2D, p_record => p_record.Int2D);
		}

		private static void AppendFieldStats(StringBuilder p_output, string p_name, IList<TibRecord> p_records, Func<TibRecord, bool> p_has, Func<TibRecord, bool> p_explicit, Func<TibRecord, bool> p_hasValue, Func<TibRecord, int> p_value)
		{
			int present = p_records.Count(p_has);
			int explicitCount = p_records.Where(p_has).Count(p_explicit);
			int valueCount = p_records.Count(p_record => p_has(p_record) && p_hasValue(p_record));
			p_output.AppendFormat(CultureInfo.InvariantCulture, "{0}: present={1}/{2} explicit={3} values={4}\n", p_name, present, p_records.Count, explicitCount, valueCount);
			if (valueCount > 0)
			{
				List<int> values = p_records.Where(p_record => p_has(p_record) && p_hasValue(p_record)).Select(p_value).ToList();
				p_output.AppendFormat(CultureInfo.InvariantCulture, "  values: min={0}, max={1}, distinct={2}\n", values.Min(), values.Max(), values.Distinct().Count());
				if (values.Distinct().Count() <= 16)
				{
					p_output.Append("  histogram: ");
					p_output.AppendLine(string.Join(", ", values.GroupBy(p_value2 => p_value2).OrderBy(p_group => p_group.Key).Select(p_group => string.Format(CultureInfo.InvariantCulture, "{0}={1}", p_group.Key, p_group.Count()))));
				}
			}
			p_output.AppendLine();
		}

		private static void AppendPerFileSummary(StringBuilder p_output, TibPropertyDump p_dump, string p_input)
		{
			p_output.AppendLine("=== Per-file Summary ===");
			foreach (string file in p_dump.Files)
			{
				List<TibRecord> records = p_dump.Records.Where(p_record => p_record.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase)).ToList();
				p_output.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1} records\n", DisplayPath(file, p_input), records.Count);
			}
			p_output.AppendLine();
		}

		private static void AppendAnomalies(StringBuilder p_output, IReadOnlyList<string> p_anomalies)
		{
			if (p_anomalies.Count == 0) return;
			p_output.AppendLine("=== Anomalies ===");
			foreach (string line in p_anomalies) p_output.AppendLine(line);
		}

		private static string DisplayPath(string p_file, string p_input)
		{
			if (!string.IsNullOrEmpty(p_input) && Directory.Exists(p_input))
				return Path.GetRelativePath(p_input, p_file);
			if (!string.IsNullOrEmpty(p_input) && File.Exists(p_input) && Path.GetFullPath(p_input).Equals(Path.GetFullPath(p_file), StringComparison.OrdinalIgnoreCase))
				return Path.GetFileName(p_file);
			return p_file;
		}

		private static string Csv(string p_value)
		{
			if (p_value == null) return string.Empty;
			if (p_value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return p_value;
			return "\"" + p_value.Replace("\"", "\"\"") + "\"";
		}
	}

	public class TibPropertyDump
	{
		public TibPropertyDump(IReadOnlyList<string> p_files, IReadOnlyList<TibRecord> p_records, IReadOnlyList<string> p_anomalies)
		{
			Files = p_files;
			Records = p_records;
			Anomalies = p_anomalies;
		}
		public IReadOnlyList<string> Files { get; private set; }
		public IReadOnlyList<TibRecord> Records { get; private set; }
		public IReadOnlyList<string> Anomalies { get; private set; }
	}

	public class TibRecord
	{
		public string FilePath;
		public int RecordIndex;
		public bool HasInt28;
		public bool Int28Explicit;
		public bool HasInt28Value;
		public int Int28;
		public bool HasInt29;
		public bool Int29Explicit;
		public bool HasInt29Value;
		public int Int29;
		public bool HasInt2A;
		public int Int2A;
		public bool HasInt2D;
		public int Int2D;
	}
}
