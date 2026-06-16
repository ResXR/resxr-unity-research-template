// CustomTablesSidecarWriter.cs
// Writes {sessionTime}_CustomTables.json at session end into the {sessionTime}_CustomTables/ subfolder.
//
// This file is a companion to SessionMetadata.json. It describes every custom
// data table used during the session: CSV filename, row count, and per-column
// metadata sourced from [ColumnInfo] annotations on each data class.
// [BuiltInTable] tables (Events, ResXRDebugLogs) are excluded — they are not part
// of the per-experiment data merged by the Python pipeline.
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
        /// <c>{directory}/{sessionPrefix}_CustomTables/{sessionPrefix}_CustomTables.json</c>.
        /// The subfolder is pre-created by <see cref="CustomCsvFromDataClass.Initialize"/>; this
        /// call creates it again if somehow missing (e.g. if no custom table was ever written).
        /// </summary>
        /// <param name="directory">Session output directory (parent of the CustomTables subfolder).</param>
        /// <param name="sessionPrefix">Session time prefix (e.g. "2026.05.26_13-15").</param>
        /// <param name="customTablesJson">
        /// The JSON object string produced by <c>ResXRDataManager.BuildCustomTablesJson()</c>.
        /// Wrapped in a root <c>CustomTables</c> key before writing.
        /// </param>
        public static void Write(string directory, string sessionPrefix, string customTablesJson)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                Debug.LogWarning("[CustomTablesSidecarWriter] Output directory is null or empty. Skipping.");
                return;
            }

            string subfolderName = string.IsNullOrWhiteSpace(sessionPrefix)
                ? "CustomTables"
                : $"{sessionPrefix}_CustomTables";
            string subfolderPath = Path.Combine(directory, subfolderName);

            try { Directory.CreateDirectory(subfolderPath); }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CustomTablesSidecarWriter] Failed to create CustomTables directory '{subfolderPath}': {ex.Message}");
                return;
            }

            string fileName = string.IsNullOrWhiteSpace(sessionPrefix)
                ? "CustomTables.json"
                : $"{sessionPrefix}_CustomTables.json";

            string path    = Path.Combine(subfolderPath, fileName);
            string content = "{\n  \"CustomTables\": " + customTablesJson + "\n}";

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
