// CustomCsvFromDataClass.cs
// Writes a custom "data class" (that implements CustomDataClass) directly to its own CSV.
//
// The CSV filename is derived from the C# class name — for example, a class named
// ChoiceEvents produces {sessionTime}_ChoiceEvents.csv. Name your class to describe
// what it records; no TableName property is needed.
//
// Every CSV automatically starts with "onset" and "duration" columns (required by the
// CustomDataClass interface), followed by all public fields declared in your class.
//
// Assumptions:
//  - The class implements `ResXRData.CustomDataClass` (onset and duration properties).
//  - All PUBLIC FIELDS (not properties) become the remaining columns (properties are ignored).
//  - Researchers typically add fields like Task, Trial, etc.
//
// Usage (once on startup):
//   CustomCsvFromDataClass.Initialize(outputDirectory, ",");
//
// Usage (per event):
//   ChoiceEvents e = new ChoiceEvents(...);   // implements CustomDataClass
//   CustomCsvFromDataClass.Write(e);
//
// Shutdown (optional):
//   CustomCsvFromDataClass.CloseAll();

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ResXRData
{
    // Marker interface required for custom event rows.
    // Implement onset and duration in every custom data class; the framework
    // always writes them as the first two columns of every custom CSV.
    public interface CustomDataClass
    {
        float onset    { get; }   // Time.realtimeSinceStartup when the row was logged
        float duration { get; }   // 0f for instantaneous events; seconds for windowed events
    }

    // Column-level metadata harvested at session end for the custom_tables sidecar.
    // Populated from [ColumnInfo] attributes; all fields except name are null/NaN for unannotated fields.
    public struct CustomColumnMeta
    {
        public string   name;
        public bool     hasAnnotation; // true = [ColumnInfo] present; false = unannotated field
        public string   description;   // null when unannotated; "" when [ColumnInfo("")]; BuildCustomTablesJson falls back to PrettifyFieldName(name) in both cases
        public string   units;         // null when not set
        public string[] levels;        // null for non-categorical columns; each element is "value:description" or "value"
        public string   format;        // null when not set; validated against BIDS allowed values at session end
        public double   minimum;       // double.NaN when not set
        public double   maximum;       // double.NaN when not set
    }

    // One entry per registered custom table, returned by GetTableSummaries().
    // Consumed by ResXRDataManager to write the custom_tables_sidecar.json file.
    public struct CustomTableSummary
    {
        public string tableName;           // C# class name (e.g. "ChoiceEvents")
        public string fileName;            // CSV filename with session prefix (e.g. "2025.09.14_15-08_ChoiceEvents.csv")
        public int    rowCount;            // data rows written this session (header not counted)
        public CustomColumnMeta[] columns; // onset + duration first, then payload fields in declaration order
    }

    public static class CustomCsvFromDataClass
    {
        private static string _baseDirectory = ".";
        private static string _delimiter = ",";
        private static string _filePrefix = null; // e.g., "2025.09.14_15-08", sessionTime from DataManager

        // One writer per table name.
        private static readonly Dictionary<string, CsvRowWriter> _writerByTable = new Dictionary<string, CsvRowWriter>(StringComparer.Ordinal);

        // One schema (header) per table name, plus cached field order for that table.
        private static readonly Dictionary<string, ColumnIndex> _schemaByTable = new Dictionary<string, ColumnIndex>(StringComparer.Ordinal);
        private static readonly Dictionary<string, FieldInfo[]> _fieldsByTable = new Dictionary<string, FieldInfo[]>(StringComparer.Ordinal);

        // Optional: remember which Type first defined a table schema (to detect conflicts).
        private static readonly Dictionary<string, Type> _definingTypeByTable = new Dictionary<string, Type>(StringComparer.Ordinal);

        // Set output directory and delimiter (call once from your DataManager)
        public static void Initialize(string baseDirectory, string delimiter = ",", string filePrefix = null)
        {
            if (!string.IsNullOrWhiteSpace(baseDirectory))
                _baseDirectory = baseDirectory;

            if (!string.IsNullOrEmpty(delimiter))
                _delimiter = delimiter;

            // Optional prefix for all custom CSVs (e.g., sessionTime)
            if (!string.IsNullOrWhiteSpace(filePrefix))
            {
                _filePrefix = SanitizeFileName(filePrefix);
            }

        }

        // Write one row for the given data instance.
        public static void Write(CustomDataClass dataInstance)
        {
            if (dataInstance == null)
                throw new ArgumentNullException(nameof(dataInstance));

            Type dataType = dataInstance.GetType();
            string tableName = dataType.Name;  // CSV filename = class name (e.g. ChoiceEvents → ChoiceEvents.csv)

            // Ensure table is initialized (writer + schema + field order)
            EnsureTableInitialized(tableName, dataType);

            // Build a row: onset + duration first (from interface), then payload fields
            FieldInfo[] fields = _fieldsByTable[tableName];
            object[] values = new object[fields.Length + 2];
            values[0] = dataInstance.onset;
            values[1] = dataInstance.duration;
            for (int i = 0; i < fields.Length; i++)
            {
                values[i + 2] = fields[i].GetValue(dataInstance);
            }

            BitArray columnIsSetMask = new BitArray(values.Length, true);
            _writerByTable[tableName].WriteRow(_schemaByTable[tableName], values, columnIsSetMask);
        }

        // Close a specific table (flush + dispose)
        public static void Close(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) return;

            if (_writerByTable.TryGetValue(tableName, out CsvRowWriter writer))
            {
                writer.Dispose();
                _writerByTable.Remove(tableName);
            }

            _schemaByTable.Remove(tableName);
            _fieldsByTable.Remove(tableName);
            _definingTypeByTable.Remove(tableName);
        }

        // Close all open writers (call on shutdown)
        public static void CloseAll()
        {
            foreach (CsvRowWriter writer in _writerByTable.Values)
            {
                writer.Dispose();
            }
            _writerByTable.Clear();
            _schemaByTable.Clear();
            _fieldsByTable.Clear();
            _definingTypeByTable.Clear();
        }

        /// <summary>
        /// Returns a snapshot of every custom table registered in this session.
        /// Includes column names, row counts, and any <see cref="ColumnInfoAttribute"/> metadata.
        /// <para>
        /// Call this <b>before</b> <see cref="CloseAll"/> — writers must still be open to read
        /// <see cref="CsvRowWriter.RowCount"/>.
        /// </para>
        /// Consumed by ResXRDataManager to write <c>{sessionTime}_custom_tables_sidecar.json</c>.
        /// </summary>
        public static CustomTableSummary[] GetTableSummaries()
        {
            var result = new CustomTableSummary[_writerByTable.Count];
            int idx = 0;
            foreach (KeyValuePair<string, CsvRowWriter> kv in _writerByTable)
            {
                string tableName = kv.Key;
                CsvRowWriter writer = kv.Value;
                FieldInfo[] fields = _fieldsByTable[tableName];

                // Reconstruct the filename exactly as EnsureTableInitialized built it.
                string safeTable = SanitizeFileName(tableName);
                string fileName = string.IsNullOrEmpty(_filePrefix)
                    ? $"{safeTable}.csv"
                    : $"{_filePrefix}_{safeTable}.csv";

                // onset and duration: universal interface columns — metadata hardcoded here
                // because there is no [ColumnInfo] on interface properties.
                var columns = new CustomColumnMeta[fields.Length + 2];
                columns[0] = new CustomColumnMeta { name = "onset",    hasAnnotation = false, description = "Time since app start when the event was logged", units = "s", format = "number", minimum = 0.0, maximum = double.NaN, levels = null };
                columns[1] = new CustomColumnMeta { name = "duration", hasAnnotation = false, description = "Event duration in seconds; 0 for point events",  units = "s", format = "number", minimum = 0.0, maximum = double.NaN, levels = null };

                // Payload fields: read [ColumnInfo] attribute if present (null = unannotated).
                for (int i = 0; i < fields.Length; i++)
                {
                    ColumnInfoAttribute attr = fields[i].GetCustomAttribute<ColumnInfoAttribute>();
                    columns[i + 2] = new CustomColumnMeta
                    {
                        name          = fields[i].Name,
                        hasAnnotation = attr != null,
                        description   = attr?.Description,
                        units         = attr?.Units,
                        levels        = attr?.Levels,
                        format        = attr?.Format,
                        minimum       = attr != null ? attr.Minimum : double.NaN,
                        maximum       = attr != null ? attr.Maximum : double.NaN,
                    };
                }

                result[idx++] = new CustomTableSummary
                {
                    tableName = tableName,
                    fileName  = fileName,
                    rowCount  = writer.RowCount,
                    columns   = columns
                };
            }
            return result;
        }

        // Create writer + schema for <base>/(<prefix>_)?<ClassName>.csv if not yet created.
        private static void EnsureTableInitialized(string tableName, Type dataType)
        {
            if (_writerByTable.ContainsKey(tableName))
            {
                // If table already exists, ensure the field layout matches original defining type
                FieldInfo[] expected = _fieldsByTable[tableName];
                FieldInfo[] current = GetPayloadFields(dataType);
                if (!SameFieldLayout(expected, current))
                {
                    string definedBy = _definingTypeByTable.TryGetValue(tableName, out Type t) ? t.FullName : "unknown";
                    throw new InvalidOperationException(
                        $"Table '{tableName}' schema mismatch. First defined by type '{definedBy}', " +
                        $"but current type '{dataType.FullName}' has a different set/order of public fields.");
                }
                return;
            }

            // Build schema: onset and duration always come first, then payload fields.
            FieldInfo[] payloadFields = GetPayloadFields(dataType);

            ColumnIndex schema = new ColumnIndex();
            schema.Add("onset");
            schema.Add("duration");
            for (int i = 0; i < payloadFields.Length; i++)
            {
                string fieldName = payloadFields[i].Name;
                if (string.IsNullOrWhiteSpace(fieldName))
                    throw new ArgumentException($"Field name at index {i} is empty in '{dataType.Name}'.");
                schema.Add(fieldName);
            }

            string safeTable = SanitizeFileName(tableName);
            string fileName = string.IsNullOrEmpty(_filePrefix) ? $"{safeTable}.csv" : $"{_filePrefix}_{safeTable}.csv";
            string path = Path.Combine(_baseDirectory, fileName);

            CsvRowWriter writer = new CsvRowWriter(path, _delimiter);

            _writerByTable[tableName] = writer;
            _schemaByTable[tableName] = schema;
            _fieldsByTable[tableName] = payloadFields;
            _definingTypeByTable[tableName] = dataType;
        }

        // Returns all public instance fields in declaration order (MetadataToken).
        // onset and duration come from the interface and are written separately — not included here.
        private static FieldInfo[] GetPayloadFields(Type dataType)
        {
            return dataType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(f => f.MetadataToken) // stable declaration order in Unity/Mono
                .ToArray();
        }

        // Compare two field arrays by name sequence
        private static bool SameFieldLayout(FieldInfo[] a, FieldInfo[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (!string.Equals(a[i].Name, b[i].Name, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        // Minimal filename sanitizer for table names (prevents invalid characters on Windows/macOS)
        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
            return sb.ToString();
        }
    }
}
