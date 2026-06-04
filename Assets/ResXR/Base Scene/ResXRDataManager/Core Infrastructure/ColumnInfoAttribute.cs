// ColumnInfoAttribute.cs
// Annotates a public field on a CustomDataClass with BIDS-compatible column metadata.
// At session end, C# reflection (CustomCsvFromDataClass.GetTableSummaries) reads these
// attributes and writes them into {sessionTime}_custom_tables_sidecar.json.
// The Python pipeline then reads that JSON to generate *_events.json BIDS sidecar files.
//
// Undecorated fields still appear in the CSV and in the custom_tables_sidecar.json
// with an auto-generated placeholder description (the field name prettified, e.g.
// "ReactionTime" → "Reaction Time"). A hard error is logged at session end — add
// [ColumnInfo("description")] to replace the placeholder with accurate metadata.
//
// ── Constructor (required) ────────────────────────────────────────────────────
//   description  — human-readable description of the column. Required.
//                  Missing annotation or empty description both log a hard error +
//                  ResXRLogs entry at session end; the field name is used as a placeholder.
//   levels       — one string per categorical level, each in "value:description" format.
//                  "value" is what appears in the CSV; "description" is the human-readable label.
//                  Value-only entries (e.g. "Left") are NOT allowed — BIDS requires a description
//                  for every level. Use "Left:Left hand" instead.
//                  Splitting is on the FIRST colon only, so extra colons in the description
//                  are fine: "url:https://example.com" → value="url", desc="https://example.com".
//                  Omit entirely for non-categorical fields (empty params = no levels).
//                  Ref: https://bids-specification.readthedocs.io/en/stable/common-principles.html#levels
//
// ── Named properties (all optional) ──────────────────────────────────────────
//   Units        — physical units string (e.g. "s", "m", "degrees").
//                  Omit for dimensionless or categorical columns.
//   Format       — BIDS column format type (see allowed values below).
//                  An unrecognised value logs a console error + ResXRLogs entry at session end.
//   Minimum      — minimum expected value (numeric columns). Omit if not applicable.
//   Maximum      — maximum expected value (numeric columns). Omit if not applicable.
//
// ── Allowed values for Format ─────────────────────────────────────────────────
//   string, number, integer, boolean, index, label, date, datetime, time, unit,
//   uri, rrid, bids_uri, dataset_relative, file_relative, participant_relative,
//   stimuli_relative, hed_version
//
// ── Usage examples ────────────────────────────────────────────────────────────
//
//   Non-categorical, no units:
//     [ColumnInfo("Name of the chosen image")]
//     public string Choice;
//
//   Non-categorical, with units and format:
//     [ColumnInfo("Seconds from stimulus display to choice", Units = "s", Format = "number", Minimum = 0.0)]
//     public float ReactionTime;
//
//   Categorical — levels as positional params:
//     [ColumnInfo("Slot chosen by the participant", "A:Left slot", "B:Right slot")]
//     public string ChosenOption;
//
//   Both units and levels (uncommon):
//     [ColumnInfo("Signal level", "Low:Below threshold", "High:Above threshold", Units = "dB")]
//     public string SignalLevel;

using System;

namespace ResXRData
{
    /// <summary>
    /// Attach to any public field of a <see cref="CustomDataClass"/> to provide
    /// BIDS sidecar metadata. At session end, C# reflection reads these attributes and
    /// writes them into <c>{sessionTime}_custom_tables_sidecar.json</c>; the Python
    /// pipeline then reads that JSON to generate <c>*_events.json</c> BIDS sidecars.
    /// Undecorated fields are included in the CSV and in the custom_tables_sidecar.json with
    /// an auto-generated placeholder description (field name prettified). A hard error is
    /// logged at session end — add <c>[ColumnInfo("description")]</c> to replace the placeholder.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ColumnInfoAttribute : Attribute
    {
        // ── Allowed BIDS format values (from https://bids-specification.readthedocs.io/en/stable/glossary.html#format-metadata) ──
        internal static readonly string[] AllowedFormats = new[]
        {
            "string", "number", "integer", "boolean", "index", "label",
            "date", "datetime", "time", "unit", "uri", "rrid", "bids_uri",
            "dataset_relative", "file_relative", "participant_relative",
            "stimuli_relative", "hed_version"
        };

        /// <summary>Human-readable description of what this column contains. Required.</summary>
        public string Description { get; }

        /// <summary>
        /// Categorical levels as an array of <c>"value:description"</c> strings
        /// (e.g. <c>"A:Left slot"</c>, <c>"Left:Left hand"</c>).
        /// Every entry must contain a colon — value-only entries (e.g. <c>"Left"</c>) are not
        /// allowed because BIDS requires a description for every level.
        /// <c>null</c> when the field is not categorical.
        /// Written as a JSON object in the sidecar: <c>{"A": "Left slot", ...}</c>.
        /// See https://bids-specification.readthedocs.io/en/stable/common-principles.html#levels
        /// </summary>
        public string[] Levels { get; }

        /// <summary>
        /// Physical units of the value (e.g. <c>"s"</c>, <c>"m"</c>, <c>"degrees"</c>).
        /// Omit for dimensionless or categorical columns.
        /// Set via named property syntax: <c>[ColumnInfo("desc", Units = "s")]</c>.
        /// </summary>
        public string Units { get; set; }

        /// <summary>
        /// BIDS column format type. Must be one of the 18 recognised values
        /// (string, number, integer, boolean, index, label, date, datetime, time,
        /// unit, uri, rrid, bids_uri, dataset_relative, file_relative,
        /// participant_relative, stimuli_relative, hed_version).
        /// An unrecognised value logs a console error and a ResXRLogs entry at session end.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Minimum expected value for numeric columns.
        /// Omit if not applicable (<c>double.NaN</c> = not set).
        /// </summary>
        public double Minimum { get; set; } = double.NaN;

        /// <summary>
        /// Maximum expected value for numeric columns.
        /// Omit if not applicable (<c>double.NaN</c> = not set).
        /// </summary>
        public double Maximum { get; set; } = double.NaN;

        /// <param name="description">Human-readable description of the column. Required.</param>
        /// <param name="levels">
        /// Zero or more <c>"value:description"</c> strings for categorical columns.
        /// Every entry must include a colon — value-only entries are not allowed.
        /// Omit entirely for non-categorical fields.
        /// </param>
        public ColumnInfoAttribute(string description, params string[] levels)
        {
            Description = description;
            Levels = levels.Length > 0 ? levels : null;
        }
    }
}
