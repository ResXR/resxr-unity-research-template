// ColumnIndex.cs
// Keeps the list of column names for a schema and lets you look them up by index or by name.

using System;
using System.Collections.Generic;

namespace ResXRData
{
    public sealed class ColumnIndex
    {
        private readonly List<string> _names = new();
        private readonly Dictionary<string, int> _indexOf = new(StringComparer.Ordinal);

        public int Count => _names.Count; // Number of columns in this schema

        public string this[int index] => _names[index]; // Get the column name by its index

        public int this[string name] => _indexOf[name]; // Get the index of a column by its name

        internal void Add(string name)
        {
            if (_indexOf.ContainsKey(name))
                throw new ArgumentException($"Duplicate column name: {name}");
            _indexOf[name] = _names.Count;
            _names.Add(name);
        }

        // Try to get the index of a column (returns true if found)
        public bool TryGetIndex(string name, out int idx) => _indexOf.TryGetValue(name, out idx);

        // Check if the schema contains a column
        public bool Contains(string name) => _indexOf.ContainsKey(name);

        // All column names in order
        public IReadOnlyList<string> Names => _names;
    }
}
