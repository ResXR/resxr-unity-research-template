// CustomTablesSidecarWriter.cs
// Writes {sessionTime}_custom_tables_sidecar.json at session end.
//
// This file is a companion to session_metadata.json. It describes every custom
// data table used during the session: CSV filename, row count, and per-column
// metadata sourced from [ColumnInfo] annotations on each data class.
//
// Consumed by the ResXR Python pipeline post-experiment to:
//   - Auto-generate *_events.json BIDS sidecar files for each custom CSV.
//   - Identify which tables to merge into the single BIDS events file
//     (all tables share the same onset/duration clock as ContinuousData.csv).
//
// Written atomically (.tmp → rename) for crash safety.
// Called from ResXRDataManager.WriteCustomTablesMetadata() in OnDestroy,
// BEFORE CustomCsvFromDataClass.CloseAll().

using System.IO;
using UnityEngine;

namespace ResXRData
{
    public static class CustomTablesSidecarWriter
    {
        /// <summary>
        /// Writes the custom tables sidecar JSON to
        /// <c>{directory}/{sessionPrefix}_custom_tables_sidecar.json</c>.
        /// </summary>
        /// <param name="directory">Output directory (same as all other session files).</param>
        /// <param name="sessionPrefix">Session time prefix (e.g. "2026.05.26_13-15").</param>
        /// <param name="customTablesJson">
        /// The JSON object string produced by <c>ResXRDataManager.BuildCustomTablesJson()</c>.
        /// Wrapped in a root <c>custom_tables</c> key before writing.
        /// </param>
        public static void Write(string directory, string sessionPrefix, string customTablesJson)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                Debug.LogWarning("[CustomTablesSidecarWriter] Output directory is null or empty. Skipping.");
                return;
            }

            Directory.CreateDirectory(directory);

            string fileName = string.IsNullOrWhiteSpace(sessionPrefix)
                ? "custom_tables_sidecar.json"
                : $"{sessionPrefix}_custom_tables_sidecar.json";

            string path    = Path.Combine(directory, fileName);
            string content = "{\n  \"custom_tables\": " + customTablesJson + "\n}";

            AtomicWrite(path, content);
        }

        // Write to .tmp then rename — same crash-safe pattern as SessionMetaWriter.
        private static void AtomicWrite(string path, string content)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string tmp = path + ".tmp";
            File.WriteAllText(tmp, content);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
    }
}
