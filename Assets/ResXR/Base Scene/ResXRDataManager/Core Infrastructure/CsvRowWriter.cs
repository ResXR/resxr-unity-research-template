// CsvRowWriter.cs
// Writes a single CSV file: header once (from ColumnIndex), then rows.
// Each call to WriteRow appends one row. Flushes immediately so data
// is safe even if Unity crashes.

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;

namespace ResXRData
{
    public sealed class CsvRowWriter : IDisposable
    {
        // Static: aggregates across every CsvRowWriter instance (Continuous, Face, custom
        // tables, ResXRDataPerfSamples). Cheap enough to leave enabled permanently — a
        // ProfilerMarker is a few nanoseconds per Begin/End even with no profiler attached.
        private static readonly ProfilerMarker s_WriteRowMarker = new ProfilerMarker("ResXR.CsvRowWriter.WriteRow");
        private static readonly ProfilerMarker s_FlushMarker = new ProfilerMarker("ResXR.CsvRowWriter.Flush");

        private readonly TextWriter _writer;
        private readonly string _delimiter;
        private bool _headerWritten;

        /// <summary>
        /// Number of data rows successfully flushed to disk since this writer was created.
        /// The header row is not included. Incremented after each <see cref="WriteRow"/> call
        /// once <c>Flush()</c> completes. Intended for session-end reporting only — never
        /// written to any CSV file.
        /// </summary>
        public int RowCount { get; private set; }

        public CsvRowWriter(string filePath, string delimiter = ",", Encoding encoding = null, bool append = false)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath cannot be null or empty", nameof(filePath));

            encoding ??= new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            _writer = new StreamWriter(filePath, append, encoding);
            _delimiter = delimiter;
        }

        // Write a single row. Writes header first if not already written.
        public void WriteRow(ColumnIndex schema, object[] values, BitArray columnIsSet)
        {
            s_WriteRowMarker.Begin();

            if (!_headerWritten)
            {
                WriteHeader(schema);
                _headerWritten = true;
            }

            for (int i = 0; i < schema.Count; i++)
            {
                if (i > 0) _writer.Write(_delimiter);

                if (columnIsSet[i] && values[i] is not null)
                {
                    string formatted = Format(values[i]);
                    _writer.Write(Escape(formatted));
                }
                // else: empty cell
            }

            _writer.WriteLine();
            s_WriteRowMarker.End();

            // No buffering: every row is flushed immediately so data survives a Unity crash.
            // Isolated in its own marker since this synchronous per-row disk write is the
            // single biggest suspected latency cost at 100Hz — see the perf-test harness under
            // Assets/ResXR/Testing/PerfTest/.
            s_FlushMarker.Begin();
            _writer.Flush();
            s_FlushMarker.End();

            RowCount++;
        }

        private void WriteHeader(ColumnIndex schema)
        {
            for (int i = 0; i < schema.Count; i++)
            {
                if (i > 0) _writer.Write(_delimiter);
                _writer.Write(Escape(schema[i]));
            }
            _writer.WriteLine();
        }

        // Convert supported types into CSV-friendly strings
        private static string Format(object value)
        {
            return value switch
            {
                float f => f.ToString(CultureInfo.InvariantCulture),
                double d => d.ToString(CultureInfo.InvariantCulture),
                int i => i.ToString(CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture), // ISO 8601
                _ => value.ToString()
            };
        }

        // Escape quotes/commas/newlines if necessary
        private static string Escape(string s)
        {
            if (s is null) return string.Empty;

            bool needsQuotes = s.Contains('\"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r');
            if (needsQuotes)
            {
                s = s.Replace("\"", "\"\"");
                return $"\"{s}\"";
            }
            return s;
        }

        public void Dispose()
        {
            _writer?.Flush();
            _writer?.Dispose();
        }
    }
}
