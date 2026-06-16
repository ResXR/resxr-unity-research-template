// MazeDataClasses.cs
// Custom data tables specific to the Maze demo.
// Coin position and start zone are recorded per-trial (future-proof: move the coin later).
// Each class defines one CSV file; its static Log() method is the reporter —
// call it from your flow scripts instead of calling LogCustom() directly.
//
// ── For new developers ────────────────────────────────────────────────────────
// PATTERN: ClassName.Log(...) writes one row to that class's CSV.
// Pass plain values; the method constructs the data object and handles the rest.
// Example:
//   MazeTrialData.Log(task, trial, trialName, startTime, endTime, rotated, coinPos, startPos);
//
// To add a new table:
//   1. Add a class implementing CustomDataClass (onset, duration, public fields)
//   2. Annotate every public field with [ColumnInfo("description")]
//   3. Add a static Log() method at the bottom of the class
//   4. Call ClassName.Log(...) from your flow script
//
// For the full explanation of how data classes and reporters work, see
// ResXRDataManager_README.md or the "custom data classes" region in
// ResXRDataManager.cs.
// ─────────────────────────────────────────────────────────────────────────────

using ResXRData;
using UnityEngine;

namespace ResXRData
{
    /// <summary>
    /// One row per trial. Extends the generic TrialsData with maze-specific columns:
    /// whether the maze was rotated at trial start, and the world-space positions of the
    /// coin and starting zone (recorded fresh each trial so relocating them later is safe).
    /// Call <c>MazeTrialData.Log(...)</c> from your flow scripts to write a row.
    /// </summary>
    public class MazeTrialData : CustomDataClass
    {
        public float onset    { get; }   // Time.realtimeSinceStartup at trial start
        public float duration { get; }   // Trial duration in seconds

        [ColumnInfo("Task name or index within the session", Format = "string")]
        public string Task;
        [ColumnInfo("Trial index within the task", Format = "integer", Minimum = 0)]
        public int Trial;
        [ColumnInfo("Human-readable unique trial identifier", Format = "string")]
        public string TrialName;
        [ColumnInfo("Whether the maze orientation was randomised at trial start", Format = "boolean")]
        public bool MazeRotatedAtStart;
        [ColumnInfo("World-space X position of the coin at trial start", Units = "m", Format = "number")]
        public float CoinX;
        [ColumnInfo("World-space Y position of the coin at trial start", Units = "m", Format = "number")]
        public float CoinY;
        [ColumnInfo("World-space Z position of the coin at trial start", Units = "m", Format = "number")]
        public float CoinZ;
        [ColumnInfo("World-space X position of the starting zone at trial start", Units = "m", Format = "number")]
        public float StartZoneX;
        [ColumnInfo("World-space Y position of the starting zone at trial start", Units = "m", Format = "number")]
        public float StartZoneY;
        [ColumnInfo("World-space Z position of the starting zone at trial start", Units = "m", Format = "number")]
        public float StartZoneZ;

        public MazeTrialData(string task, int trial, string trialName,
            float startTime, float endTime, bool mazeRotatedAtStart,
            Vector3 coinPos, Vector3 startZonePos)
        {
            onset = startTime;
            duration = endTime - startTime;
            Task = task;
            Trial = trial;
            TrialName = trialName;
            MazeRotatedAtStart = mazeRotatedAtStart;
            CoinX = coinPos.x;
            CoinY = coinPos.y;
            CoinZ = coinPos.z;
            StartZoneX = startZonePos.x;
            StartZoneY = startZonePos.y;
            StartZoneZ = startZonePos.z;
        }

        /// <summary>
        /// Writes one MazeTrialData row. Call at trial end alongside <c>TrialsData.Log()</c>.
        /// </summary>
        public static void Log(string task, int trial, string trialName,
            float startTime, float endTime, bool mazeRotatedAtStart,
            Vector3 coinPos, Vector3 startZonePos)
        {
            ResXRDataManager.Instance.LogCustom(
                new MazeTrialData(task, trial, trialName, startTime, endTime,
                                  mazeRotatedAtStart, coinPos, startZonePos));
        }
    }
}
