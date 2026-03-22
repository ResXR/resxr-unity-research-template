// RowBuffer.cs
// Reusable row scratchpad: set values by name or index, track which cells are set, then clear for reuse.

using System;
using System.Collections;

namespace ResXRData
{
    public sealed class RowBuffer
    {
        private readonly ColumnIndex _schema;
        private readonly object[] _valuesForColumns;
        private readonly BitArray _columnIsSet;

        public RowBuffer(ColumnIndex schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            _schema = schema;
            _valuesForColumns = new object[schema.Count];
            _columnIsSet = new BitArray(schema.Count);
        }

        // Number of columns in this row (matches the schema)
        public int ColumnCount => _valuesForColumns.Length;

        // Set by index (fast path). Throws if index is out of range.
        public void Set(int columnIndex, object value)
        {
            if ((uint)columnIndex >= (uint)_valuesForColumns.Length)
                throw new IndexOutOfRangeException($"Column index {columnIndex} is out of range [0..{_valuesForColumns.Length - 1}]");

            _valuesForColumns[columnIndex] = value;
            _columnIsSet[columnIndex] = true;
        }

        // Set by name (friendly path). Throws if name is not found in the schema.
        public void Set(string columnName, object value)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

            if (!_schema.TryGetIndex(columnName, out int columnIndex))
                throw new ArgumentException($"Column not found in schema: {columnName}", nameof(columnName));

            Set(columnIndex, value);
        }

        // Try-set by name: returns false if the column doesn't exist; never throws.
        public bool TrySet(string columnName, object value)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            if (!_schema.TryGetIndex(columnName, out int columnIndex))
                return false;

            _valuesForColumns[columnIndex] = value;
            _columnIsSet[columnIndex] = true;
            return true;
        }

        // Get whether a specific column has been set for this row.
        public bool IsSet(int columnIndex)
        {
            if ((uint)columnIndex >= (uint)_valuesForColumns.Length)
                throw new IndexOutOfRangeException($"Column index {columnIndex} is out of range [0..{_valuesForColumns.Length - 1}]");

            return _columnIsSet[columnIndex];
        }

        // Clear the buffer so it can be reused for the next row.
        public void Clear()
        {
            Array.Clear(_valuesForColumns, 0, _valuesForColumns.Length);
            _columnIsSet.SetAll(false);
        }

        // Internal accessors used by writers/managers (CsvRowWriter, CsvFileManager).
        internal object[] ValuesArray => _valuesForColumns;
        internal BitArray ColumnIsSetMask => _columnIsSet;

        // Optional convenience: set a bunch of values at once by (name, value) pairs.
        // Any name not in the schema throws to reveal mistakes early.
        public void SetMany(params (string name, object value)[] assignments)
        {
            if (assignments == null) return;
            for (int i = 0; i < assignments.Length; i++)
            {
                (string name, object value) pair = assignments[i];
                Set(pair.name, pair.value);
            }
        }
    }
}
