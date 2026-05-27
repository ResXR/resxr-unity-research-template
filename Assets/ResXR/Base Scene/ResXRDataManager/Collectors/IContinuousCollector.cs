// IContinuousCollector.cs
// Interface for all per-tick collectors (head/eyes/hands/body/face/custom/recenter).
// DataManager calls Configure() once, then Collect() every FixedUpdate, then Dispose() on shutdown.

using System;
using static OVRPlugin;

namespace ResXRData
{
    public interface IContinuousCollector : IDisposable
    {
        // Human-friendly name for logs/diagnostics
        string CollectorName { get; }

        // Called once after schemas are created and before collection starts.
        // Use this to cache column indices, resolve scene references, enable/disable internal paths, etc.
        void Configure(ColumnIndex schema, RecordingOptions options);

        // Called every physics tick (FixedUpdate) to write one row�s values.
        // Implementations should ONLY write to columns that exist in 'schema'
        // (use RowBuffer.TrySet to be safe). timeSinceStartup is Unity's Time.realtimeSinceStartup.
        void Collect(RowBuffer row, float timeSinceStartup);
    }

    public static class OvrSampling // shared OVRPlugin sampling parameters
    {
        public const OVRPlugin.Step StepDefault = OVRPlugin.Step.Render; // Step.physics is deprecated, render is default
        public const int LatestFrame = -1; // default parameter to get the latest available frame
    }

    public static class CollectorUtils
    {
        public static int IndexOrMinusOne(ColumnIndex schema, string name)
        {
            return schema.TryGetIndex(name, out int idx) ? idx : -1;
        }

        public static void TryIndex(ColumnIndex schema, string name, out int idx)
        {
            if (!schema.TryGetIndex(name, out idx)) idx = -1;
        }

        // used by OVRFaceCollector to try multiple column names
        public static int IndexFirstFound(ColumnIndex schema, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
                if (schema.TryGetIndex(names[i], out int idx)) return idx;
            return -1;
        }

        public static void SetIfValid(RowBuffer row, int colIndex, float value)
        {
            if (colIndex >= 0) row.Set(colIndex, value);
        }
        public static void SetIfValid(RowBuffer row, int colIndex, int value)
        {
            if (colIndex >= 0) row.Set(colIndex, value);
        }
        public static void SetIfValid(RowBuffer row, int colIndex, double value)
        {
            if (colIndex >= 0) row.Set(colIndex, value);
        }
        public static void SetIfValid(RowBuffer row, int colIndex, bool value)
        {
            if (colIndex >= 0) row.Set(colIndex, value);
        }

        public static void SetIfValid(RowBuffer row, int colIndex, string value)
        {
            if (colIndex >= 0) row.Set(colIndex, value);
        }

        // Helper methods for parsing enums to binary flags

        /// <summary>
        /// Parses HandStatus enum flags into 5 boolean values.
        /// Returns array of 5 booleans: [HandTracked, InputStateValid, SystemGestureInProgress, DominantHand, MenuPressed]
        /// </summary>
        public static bool[] ParseHandStatusFlags(HandStatus status)
        {
            return new bool[]
            {
                (status & HandStatus.HandTracked) != 0,
                (status & HandStatus.InputStateValid) != 0,
                (status & HandStatus.SystemGestureInProgress) != 0,
                (status & HandStatus.DominantHand) != 0,
                (status & HandStatus.MenuPressed) != 0
            };
        }

        /// <summary>
        /// Converts TrackingConfidence enum to binary value: 0=Low, 1=High
        /// </summary>
        public static int ParseTrackingConfidenceToBinary(TrackingConfidence confidence)
        {
            return confidence == TrackingConfidence.High ? 1 : 0;
        }

        /// <summary>
        /// Converts BodyTrackingFidelity2 enum to binary value: 0=Low, 1=High
        /// </summary>
        public static int ParseBodyTrackingFidelityToBinary(BodyTrackingFidelity2 fidelity)
        {
            return fidelity == BodyTrackingFidelity2.High ? 1 : 0;
        }

        /// <summary>
        /// Parses BodyTrackingCalibrationState enum into 3 boolean flags.
        /// Returns array of 3 booleans: [Invalid, Calibrating, Valid]
        /// </summary>
        public static bool[] ParseBodyCalibrationStatus(BodyTrackingCalibrationState status)
        {
            return new bool[]
            {
                status == BodyTrackingCalibrationState.Invalid,
                status == BodyTrackingCalibrationState.Calibrating,
                status == BodyTrackingCalibrationState.Valid
            };
        }

        /// <summary>
        /// Parses SpaceLocationFlags enum into 4 boolean flags.
        /// Returns array of 4 booleans: [OrientationValid, PositionValid, OrientationTracked, PositionTracked]
        /// </summary>
        public static bool[] ParseSpaceLocationFlags(SpaceLocationFlags flags)
        {
            return new bool[]
            {
                (flags & SpaceLocationFlags.OrientationValid) != 0,
                (flags & SpaceLocationFlags.PositionValid) != 0,
                (flags & SpaceLocationFlags.OrientationTracked) != 0,
                (flags & SpaceLocationFlags.PositionTracked) != 0
            };
        }

    }
}
