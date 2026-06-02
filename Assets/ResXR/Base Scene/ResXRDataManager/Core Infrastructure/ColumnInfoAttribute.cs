// ColumnInfoAttribute.cs
// Annotates a public field on a CustomDataClass with BIDS-compatible column metadata.
// The Python pipeline reads these attributes via reflection to generate
// *_columns.json sidecar files alongside each custom CSV.
//
// Undecorated fields still appear in the CSV — they just produce no sidecar entry.
//
// Usage:
//   [ColumnInfo("Seconds since app start", units: "s")]
//   public float TimeSinceStart;
//
//   [ColumnInfo("Which option slot was chosen", levels: "A|B")]
//   public string ChosenOption;
//
//   [ColumnInfo("Name of the chosen image")]
//   public string Choice;

using System;

namespace ResXRData
{
    /// <summary>
    /// Attach to any public field of a <see cref="CustomDataClass"/> to provide
    /// BIDS sidecar metadata. All three properties are surfaced to the Python
    /// pipeline via reflection; undecorated fields are included in the CSV but
    /// will have no entry in the sidecar JSON.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ColumnInfoAttribute : Attribute
    {
        /// <summary>Human-readable description of what this column contains.</summary>
        public string Description { get; }

        /// <summary>
        /// Physical units of the value (e.g. "s", "m", "degrees").
        /// Use "n/a" (default) for dimensionless or categorical columns.
        /// </summary>
        public string Units { get; }

        /// <summary>
        /// Pipe-separated list of all possible values for categorical columns
        /// (e.g. "A|B", "left|right|both"). Null for continuous or non-categorical fields.
        /// </summary>
        public string Levels { get; }

        public ColumnInfoAttribute(string description, string units = "n/a", string levels = null)
        {
            Description = description;
            Units = units;
            Levels = levels;
        }
    }
}
