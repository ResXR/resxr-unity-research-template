// OVREyesCollector.cs
// Eye gazes from OVRPlugin + FocusedObject/HitPoint from ResXRPlayer.Instance.

using System;
using UnityEngine;
using static OVRPlugin;   // EyeGazesState, Eye, Step
using static ResXRData.CollectorUtils;

namespace ResXRData
{
    public sealed class OVREyesCollector : IContinuousCollector
    {
        public string CollectorName => "OVREyesCollector";
        private const Step SampleStep = OvrSampling.StepDefault;
        private const int LatestFrame = OvrSampling.LatestFrame;

        // ResXRPlayer reference (assigned to ResXRPlayer.Instance in Configure)
        private ResXRPlayer _player;

        // Dedicated eye block
        private int _idxRightQx = -1, _idxRightQy = -1, _idxRightQz = -1, _idxRightQw = -1;
        private int _idxLeftQx = -1, _idxLeftQy = -1, _idxLeftQz = -1, _idxLeftQw = -1;
        private int _idxLeftValid = -1, _idxLeftConf = -1;
        private int _idxRightValid = -1, _idxRightConf = -1;
        private int _idxEyesTime = -1;

        // Legacy gaze (from ResXRPlayer)
        private int _idxFocusedObject = -1;
        private int _idxHitX = -1, _idxHitY = -1, _idxHitZ = -1;

        // Per-eye gaze (from ResXRPlayer.EyeTracker)
        private int _idxLeftHitX = -1, _idxLeftHitY = -1, _idxLeftHitZ = -1;
        private int _idxRightHitX = -1, _idxRightHitY = -1, _idxRightHitZ = -1;
        private int _idxLeftFocusedObject = -1, _idxRightFocusedObject = -1;
        private int _idxHasLeftEyeHit = -1, _idxHasRightEyeHit = -1;

        private bool _writeDedicatedEyes = false;
        private bool _writeLegacyGaze = false;
        private bool _writeSeparateEyesGaze = false;

        public void Configure(ColumnIndex schema, RecordingOptions options)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (options == null) options = new RecordingOptions();

            _writeDedicatedEyes = options.includeEyes;
            _writeLegacyGaze = options.includeGaze;
            _writeSeparateEyesGaze = options.includeSeparateEyesGaze;

            _player = ResXRPlayer.Instance;

            if (_writeDedicatedEyes)
            {
                TryIndex(schema, "RightEye_qx", out _idxRightQx);
                TryIndex(schema, "RightEye_qy", out _idxRightQy);
                TryIndex(schema, "RightEye_qz", out _idxRightQz);
                TryIndex(schema, "RightEye_qw", out _idxRightQw);
                
                TryIndex(schema, "LeftEye_qx", out _idxLeftQx);
                TryIndex(schema, "LeftEye_qy", out _idxLeftQy);
                TryIndex(schema, "LeftEye_qz", out _idxLeftQz);
                TryIndex(schema, "LeftEye_qw", out _idxLeftQw);

                TryIndex(schema, "LeftEye_IsValid", out _idxLeftValid);
                TryIndex(schema, "LeftEye_Confidence", out _idxLeftConf);
                TryIndex(schema, "RightEye_IsValid", out _idxRightValid);
                TryIndex(schema, "RightEye_Confidence", out _idxRightConf);

                TryIndex(schema, "Eyes_Time", out _idxEyesTime);
            }

            if (_writeLegacyGaze)
            {
                TryIndex(schema, "FocusedObject", out _idxFocusedObject);
                TryIndex(schema, "EyeGazeHitPosition_X", out _idxHitX);
                TryIndex(schema, "EyeGazeHitPosition_Y", out _idxHitY);
                TryIndex(schema, "EyeGazeHitPosition_Z", out _idxHitZ);
            }

            if (_writeSeparateEyesGaze)
            {
                TryIndex(schema, "LeftEyeGazeHitPosition_X", out _idxLeftHitX);
                TryIndex(schema, "LeftEyeGazeHitPosition_Y", out _idxLeftHitY);
                TryIndex(schema, "LeftEyeGazeHitPosition_Z", out _idxLeftHitZ);
                TryIndex(schema, "RightEyeGazeHitPosition_X", out _idxRightHitX);
                TryIndex(schema, "RightEyeGazeHitPosition_Y", out _idxRightHitY);
                TryIndex(schema, "RightEyeGazeHitPosition_Z", out _idxRightHitZ);
                TryIndex(schema, "LeftFocusedObject", out _idxLeftFocusedObject);
                TryIndex(schema, "RightFocusedObject", out _idxRightFocusedObject);
                TryIndex(schema, "HasLeftEyeHit", out _idxHasLeftEyeHit);
                TryIndex(schema, "HasRightEyeHit", out _idxHasRightEyeHit);
            }
        }

        public void Collect(RowBuffer row, float timeSinceStartup)
        {
            if (_writeDedicatedEyes)
            {
                EyeGazesState state = default;
                bool ok = GetEyeGazesState(SampleStep, LatestFrame, ref state);
                if (ok && state.EyeGazes != null && state.EyeGazes.Length >= (int)Eye.Count)
                {
                    // Right eye - convert orientation to world space
                    EyeGazeState right = state.EyeGazes[(int)Eye.Right];
                    Quaternion rightWorldRot = TrackingSpaceConverter.ToWorldSpaceRotation(right.Pose.Orientation);
                    SetIfValid(row, _idxRightQx, rightWorldRot.x);
                    SetIfValid(row, _idxRightQy, rightWorldRot.y);
                    SetIfValid(row, _idxRightQz, rightWorldRot.z);
                    SetIfValid(row, _idxRightQw, rightWorldRot.w);
                    SetIfValid(row, _idxRightValid, right.IsValid ? 1 : 0);
                    SetIfValid(row, _idxRightConf, right.Confidence);

                    // Left eye - convert orientation to world space
                    EyeGazeState left = state.EyeGazes[(int)Eye.Left];
                    Quaternion leftWorldRot = TrackingSpaceConverter.ToWorldSpaceRotation(left.Pose.Orientation);
                    SetIfValid(row, _idxLeftQx, leftWorldRot.x);
                    SetIfValid(row, _idxLeftQy, leftWorldRot.y);
                    SetIfValid(row, _idxLeftQz, leftWorldRot.z);
                    SetIfValid(row, _idxLeftQw, leftWorldRot.w);
                    SetIfValid(row, _idxLeftValid, left.IsValid ? 1 : 0);
                    SetIfValid(row, _idxLeftConf, left.Confidence);

                    // Shared timestamp
                    SetIfValid(row, _idxEyesTime, state.Time);
                }
            }

            if (_writeLegacyGaze)
            {
                if (!_player)
                {
                    _player = ResXRPlayer.Instance;   // try again if ResXRPlayer was not ready in Configure
                }
                if (_player)                        // only write if player exists
                {
                    // FocusedObject (null/destroy-safe)
                    if (_idxFocusedObject >= 0)
                    {
                        Transform focusedObject = _player.FocusedObject;
                        row.Set(_idxFocusedObject, focusedObject ? focusedObject.name : "");
                    }

                    // Hit point
                    Vector3 hit = _player.EyeGazeHitPosition;
                    SetIfValid(row, _idxHitX, hit.x);
                    SetIfValid(row, _idxHitY, hit.y);
                    SetIfValid(row, _idxHitZ, hit.z);
                }
            }

            if (_writeSeparateEyesGaze && _player != null && _player.EyeTracker != null)
            {
                ResXREyeTracker tracker = _player.EyeTracker;
                Vector3 leftHit = tracker.LeftEyeGazeHitPosition;
                Vector3 rightHit = tracker.RightEyeGazeHitPosition;
                SetIfValid(row, _idxLeftHitX, leftHit.x);
                SetIfValid(row, _idxLeftHitY, leftHit.y);
                SetIfValid(row, _idxLeftHitZ, leftHit.z);
                SetIfValid(row, _idxRightHitX, rightHit.x);
                SetIfValid(row, _idxRightHitY, rightHit.y);
                SetIfValid(row, _idxRightHitZ, rightHit.z);
                if (_idxLeftFocusedObject >= 0)
                    row.Set(_idxLeftFocusedObject, tracker.LeftFocusedObject ? tracker.LeftFocusedObject.name : "");
                if (_idxRightFocusedObject >= 0)
                    row.Set(_idxRightFocusedObject, tracker.RightFocusedObject ? tracker.RightFocusedObject.name : "");
                SetIfValid(row, _idxHasLeftEyeHit, tracker.HasLeftEyeHit ? 1 : 0);
                SetIfValid(row, _idxHasRightEyeHit, tracker.HasRightEyeHit ? 1 : 0);
            }
        }

        public void Dispose() { }

    }
}
