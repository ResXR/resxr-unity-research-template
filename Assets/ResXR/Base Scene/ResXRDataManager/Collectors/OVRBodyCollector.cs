// OVRBodyCollector.cs
// Collects body tracking data (frame-level fields + per-joint pose/flags) via OVRPlugin.GetBodyState4.

using System;
using UnityEngine;
using static OVRPlugin;   // BodyState, BodyJointLocation, BodyJointSet, Step, Vector3f, Quatf
using static ResXRData.CollectorUtils;
namespace ResXRData
{
    public sealed class OVRBodyCollector : IContinuousCollector
    {
        public string CollectorName => "OVRBodyCollector";
        private const Step SampleStep = OvrSampling.StepDefault;   // Plugin maps Physics -> Render when needed
        private const BodyJointSet JointSet = BodyJointSet.FullBody; //UpperBody or FullBody if you enable it in options

        private bool _includeBody = false;
        private int _jointCount = 0;

        // Root/body state columns
        private int _idxBodyTime = -1;
        private int _idxBodyConfidence = -1;
        // Body Fidelity: single column, will write 0/1 (0=Low, 1=High) instead of string
        private int _idxBodyFidelity = -1;
        // Body Calibration Status: 3 flag columns instead of single string column
        private int[] _idxBodyCalibrationStatusFlags; // [Invalid, Calibrating, Valid]
        private int _idxBodySkeletonChangedCount = -1;

        // Per-joint column indices
        private int[] _idxPosX, _idxPosY, _idxPosZ;
        private int[] _idxQx, _idxQy, _idxQz, _idxQw;
        // Joint Flags: 4 flag columns per joint instead of single string column
        private int[][] _idxJointFlags; // per joint: [OrientationValid, PositionValid, OrientationTracked, PositionTracked]

        public void Configure(ColumnIndex schema, RecordingOptions options)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (options == null) options = new RecordingOptions();

            _includeBody = options.includeBody;
            if (!_includeBody) return;

            // Match the body joint count used by SchemaBuilder
            _jointCount = SchemaFactories.DetectBodyJointCount(out bool bodyDetectionOk);


            // Root/body state indices
            TryIndex(schema, "Body_Time", out _idxBodyTime);
            TryIndex(schema, "Body_Confidence", out _idxBodyConfidence);
            TryIndex(schema, "Body_Fidelity", out _idxBodyFidelity);
            // Body Calibration Status: 3 flag columns
            _idxBodyCalibrationStatusFlags = new int[3];
            _idxBodyCalibrationStatusFlags[0] = IndexOrMinusOne(schema, "Body_CalibrationStatus_Invalid");
            _idxBodyCalibrationStatusFlags[1] = IndexOrMinusOne(schema, "Body_CalibrationStatus_Calibrating");
            _idxBodyCalibrationStatusFlags[2] = IndexOrMinusOne(schema, "Body_CalibrationStatus_Valid");
            TryIndex(schema, "Body_SkeletonChangedCount", out _idxBodySkeletonChangedCount);

            // Allocate and cache per-joint indices
            _idxPosX = new int[_jointCount];
            _idxPosY = new int[_jointCount];
            _idxPosZ = new int[_jointCount];

            _idxQx = new int[_jointCount];
            _idxQy = new int[_jointCount];
            _idxQz = new int[_jointCount];
            _idxQw = new int[_jointCount];

            // Joint Flags: 4 flag columns per joint
            _idxJointFlags = new int[_jointCount][];

            string[] bodyJointNames = SchemaFactories.GetBodyJointNames();
            if (bodyJointNames.Length != _jointCount)
            {
                Debug.LogError($"[OVRBodyCollector] Body joint names count mismatch. Detected count: {_jointCount}, Names count: {bodyJointNames.Length}");
            }

            for (int jointIndex = 0; jointIndex < _jointCount; jointIndex++)
            {
                if (jointIndex < bodyJointNames.Length)
                {
                    string jointName = bodyJointNames[jointIndex];
                    _idxPosX[jointIndex] = IndexOrMinusOne(schema, $"{jointName}_px");
                    _idxPosY[jointIndex] = IndexOrMinusOne(schema, $"{jointName}_py");
                    _idxPosZ[jointIndex] = IndexOrMinusOne(schema, $"{jointName}_pz");

                    _idxQx[jointIndex] = IndexOrMinusOne(schema, $"{jointName}_qx");
                    _idxQy[jointIndex] = IndexOrMinusOne(schema, $"{jointName}_qy");
                    _idxQz[jointIndex] = IndexOrMinusOne(schema, $"{jointName}_qz");
                    _idxQw[jointIndex] = IndexOrMinusOne(schema, $"{jointName}_qw");

                    // Joint Flags: 4 flag columns per joint
                    _idxJointFlags[jointIndex] = new int[4];
                    _idxJointFlags[jointIndex][0] = IndexOrMinusOne(schema, $"{jointName}_Flags_OrientationValid");
                    _idxJointFlags[jointIndex][1] = IndexOrMinusOne(schema, $"{jointName}_Flags_PositionValid");
                    _idxJointFlags[jointIndex][2] = IndexOrMinusOne(schema, $"{jointName}_Flags_OrientationTracked");
                    _idxJointFlags[jointIndex][3] = IndexOrMinusOne(schema, $"{jointName}_Flags_PositionTracked");
                }
            }

        }

        public void Collect(RowBuffer row, float timeSinceStartup)
        {
            if (!_includeBody) return;

            BodyState bodyState = default;

            // Plugin API: GetBodyState4(step, jointSet, ref bodyState)
            if (!GetBodyState4(SampleStep, JointSet, ref bodyState)) return;  // fails if not active or unsupported. 

            // Frame-level fields
            SetIfValid(row, _idxBodyTime, bodyState.Time);                    // double.
            SetIfValid(row, _idxBodyConfidence, bodyState.Confidence);        // float. 
            // Body Fidelity: Convert to binary (0=Low, 1=High) instead of string
            SetIfValid(row, _idxBodyFidelity, ParseBodyTrackingFidelityToBinary(bodyState.Fidelity));
            // Body Calibration Status: Parse enum and set 3 flag columns
            bool[] calibrationFlags = ParseBodyCalibrationStatus(bodyState.CalibrationStatus);
            for (int i = 0; i < calibrationFlags.Length && i < _idxBodyCalibrationStatusFlags.Length; i++)
            {
                SetIfValid(row, _idxBodyCalibrationStatusFlags[i], calibrationFlags[i] ? 1 : 0);
            }
            SetIfValid(row, _idxBodySkeletonChangedCount, bodyState.SkeletonChangedCount); // uint -> int ok. 

            // Per-joint
            int count = bodyState.JointLocations != null ? bodyState.JointLocations.Length : 0;
            int limit = Math.Min(_jointCount, count);

            for (int j = 0; j < limit; j++)
            {
                BodyJointLocation loc = bodyState.JointLocations[j];

                // Convert joint position to world space
                Vector3 worldPos = TrackingSpaceConverter.ToWorldSpacePosition(loc.Pose);
                SetIfValid(row, _idxPosX[j], worldPos.x);
                SetIfValid(row, _idxPosY[j], worldPos.y);
                SetIfValid(row, _idxPosZ[j], worldPos.z);

                // Convert joint rotation to world space
                Quaternion worldRot = TrackingSpaceConverter.ToWorldSpaceRotation(loc.Pose.Orientation);
                SetIfValid(row, _idxQx[j], worldRot.x);
                SetIfValid(row, _idxQy[j], worldRot.y);
                SetIfValid(row, _idxQz[j], worldRot.z);
                SetIfValid(row, _idxQw[j], worldRot.w);

                // Joint Flags: Parse enum and set 4 flag columns
                bool[] jointFlags = ParseSpaceLocationFlags(loc.LocationFlags);
                if (j < _idxJointFlags.Length && _idxJointFlags[j] != null)
                {
                    for (int flagIdx = 0; flagIdx < jointFlags.Length && flagIdx < _idxJointFlags[j].Length; flagIdx++)
                    {
                        SetIfValid(row, _idxJointFlags[j][flagIdx], jointFlags[flagIdx] ? 1 : 0);
                    }
                }
            }
        }

        public void Dispose() { }

    }
}
