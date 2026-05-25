// MazeDataClasses.cs
// Custom data tables specific to the Maze demo.
// Coin position and start zone are recorded per-trial (future-proof: move the coin later).
//
// ── For new developers ────────────────────────────────────────────────────────
// Each class here becomes one CSV file. Each public field = one column.
// To add a new table, add a new class that implements CustomDataClass.
// To log data, add a reporter method to ResXRDataManager_V2 (see LogChoice or
// LogLineToFile there for examples), then call it from your flow scripts.
// For the full explanation of how data classes work, see the
// "custom data classes" region at the top of ResXRDataManager_V2.cs.
// ─────────────────────────────────────────────────────────────────────────────

using ResXRData;
using UnityEngine;

namespace ResXRData
{
    /// <summary>
    /// One row per trial. Includes maze-specific columns on top of the generic TrialsData:
    /// whether the maze was rotated at trial start, and the world-space positions of the
    /// coin and starting zone (recorded fresh each trial so relocating them later is safe).
    /// </summary>
    public class MazeTrialData : CustomDataClass
    {
        public string TableName => "MazeTrials";

        public string Session;
        public string Task;
        public int Trial;
        public string TrialName;
        public float StartTime;
        public float EndTime;
        public float TimeToCoin;
        public bool MazeRotatedAtStart;
        public float CoinX;
        public float CoinY;
        public float CoinZ;
        public float StartZoneX;
        public float StartZoneY;
        public float StartZoneZ;

        public MazeTrialData(string session, string task, int trial, string trialName,
            float startTime, float endTime, bool mazeRotatedAtStart,
            Vector3 coinPos, Vector3 startZonePos)
        {
            Session = session;
            Task = task;
            Trial = trial;
            TrialName = trialName;
            StartTime = startTime;
            EndTime = endTime;
            TimeToCoin = endTime - startTime;
            MazeRotatedAtStart = mazeRotatedAtStart;
            CoinX = coinPos.x;
            CoinY = coinPos.y;
            CoinZ = coinPos.z;
            StartZoneX = startZonePos.x;
            StartZoneY = startZonePos.y;
            StartZoneZ = startZonePos.z;
        }
    }
}
