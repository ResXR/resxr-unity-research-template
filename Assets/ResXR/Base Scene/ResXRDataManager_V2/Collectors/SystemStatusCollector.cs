// SystemStatusCollector.cs
// Collects system status: recenter detection, tracking space transform, user presence, tracking loss
//
// Recenter detection (existing + new):
//   - shouldRecenter: OVRPlugin.shouldRecenter (continuous 0/1 flag)
//   - recenterEvent: Derived edge detection (1 on 0->1 transition, else 0)
//   - RecenterCount: OVRPlugin.GetLocalTrackingSpaceRecenterCount() (cumulative counter)
//   - TrackingOriginChange_Event: OVRManager.TrackingOriginChangePending event (pulse, most precise)
//   - TrackingOriginChange_PrevPose: Previous tracking origin from event (converted to world space to preserve spatial relationship)
//
// Tracking space (new):
//   - TrackingTransform: OVRPlugin.GetTrackingTransformRawPose() (current origin pose in world space)
//
// User & tracking status (new):
//   - UserPresent: OVRPlugin.userPresent (user wearing headset)
//   - TrackingLost: !GetNodePositionTracked(Node.EyeCenter) (SLAM tracking lost)
//
// COORDINATE SPACE NOTE:
// TrackingOriginChange_PrevPose comes from event in OLD tracking space but is converted to world space
// before recording to maintain consistency with post-recenter data. This allows analysis to compare
// absolute spatial positions before/after recenter.

using System;
using UnityEngine;
using static OVRPlugin;
using static ResXRData.CollectorUtils;

namespace ResXRData
{
    public sealed class SystemStatusCollector : IContinuousCollector
    {
        public string CollectorName => "SystemStatusCollector";

        // Recenter detection (existing)
        private int _idxShouldRecenter = -1;
        private int _idxRecenterEvent = -1;

        // NEW: Recenter count
        private int _idxRecenterCount = -1;

        // NEW: Tracking origin change event (8 columns)
        private int _idxTrackingOriginChangeEvent = -1;
        private int _idxTrackingOriginChangePrevPx = -1, _idxTrackingOriginChangePrevPy = -1, _idxTrackingOriginChangePrevPz = -1;
        private int _idxTrackingOriginChangePrevQx = -1, _idxTrackingOriginChangePrevQy = -1;
        private int _idxTrackingOriginChangePrevQz = -1, _idxTrackingOriginChangePrevQw = -1;
        private bool _trackingOriginChangePulse = false;  // Set to true by event, cleared after write
        private Posef? _trackingOriginChangePrevPose = null;  // Stored from event

        // NEW: Tracking transform (7 columns)
        private int _idxTrackingTransformPx = -1, _idxTrackingTransformPy = -1, _idxTrackingTransformPz = -1;
        private int _idxTrackingTransformQx = -1, _idxTrackingTransformQy = -1;
        private int _idxTrackingTransformQz = -1, _idxTrackingTransformQw = -1;

        // NEW: User presence & tracking loss
        private int _idxUserPresent = -1;
        private int _idxTrackingLost = -1;

        private int _prevShouldRecenter = 0;
        private bool _haveSignal = false;
        private bool _enabled = true;

        public void Configure(ColumnIndex schema, RecordingOptions options)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            
            // Existing columns
            TryIndex(schema, "shouldRecenter", out _idxShouldRecenter);
            TryIndex(schema, "recenterEvent", out _idxRecenterEvent);
            
            // NEW: Recenter count
            TryIndex(schema, "RecenterCount", out _idxRecenterCount);
            
            // NEW: Tracking origin change event
            TryIndex(schema, "TrackingOriginChange_Event", out _idxTrackingOriginChangeEvent);
            TryIndex(schema, "TrackingOriginChange_PrevPose_px", out _idxTrackingOriginChangePrevPx);
            TryIndex(schema, "TrackingOriginChange_PrevPose_py", out _idxTrackingOriginChangePrevPy);
            TryIndex(schema, "TrackingOriginChange_PrevPose_pz", out _idxTrackingOriginChangePrevPz);
            TryIndex(schema, "TrackingOriginChange_PrevPose_qx", out _idxTrackingOriginChangePrevQx);
            TryIndex(schema, "TrackingOriginChange_PrevPose_qy", out _idxTrackingOriginChangePrevQy);
            TryIndex(schema, "TrackingOriginChange_PrevPose_qz", out _idxTrackingOriginChangePrevQz);
            TryIndex(schema, "TrackingOriginChange_PrevPose_qw", out _idxTrackingOriginChangePrevQw);
            
            // NEW: Tracking transform
            TryIndex(schema, "TrackingTransform_px", out _idxTrackingTransformPx);
            TryIndex(schema, "TrackingTransform_py", out _idxTrackingTransformPy);
            TryIndex(schema, "TrackingTransform_pz", out _idxTrackingTransformPz);
            TryIndex(schema, "TrackingTransform_qx", out _idxTrackingTransformQx);
            TryIndex(schema, "TrackingTransform_qy", out _idxTrackingTransformQy);
            TryIndex(schema, "TrackingTransform_qz", out _idxTrackingTransformQz);
            TryIndex(schema, "TrackingTransform_qw", out _idxTrackingTransformQw);

            // NEW: User presence & tracking loss
            TryIndex(schema, "UserPresent", out _idxUserPresent);
            TryIndex(schema, "TrackingLost", out _idxTrackingLost);

            _enabled = (_idxShouldRecenter >= 0) || (_idxRecenterEvent >= 0) || 
                       (_idxRecenterCount >= 0) || (_idxTrackingOriginChangeEvent >= 0) ||
                       (_idxTrackingTransformPx >= 0) ||
                       (_idxUserPresent >= 0) || (_idxTrackingLost >= 0);
            
            // Subscribe to tracking origin change event if needed
            if (_idxTrackingOriginChangeEvent >= 0)
            {
                OVRManager.TrackingOriginChangePending += OnTrackingOriginChangePending;
            }

            if (TryGetShouldRecenter(out int sr))
            {
                _prevShouldRecenter = sr;
                _haveSignal = true;
            }
        }

        public void Collect(RowBuffer row, float timeSinceStartup)
        {
            if (!_enabled) return;

            // Existing: shouldRecenter and recenterEvent
            if (TryGetShouldRecenter(out int sr))
            {
                if (_idxShouldRecenter >= 0) row.Set(_idxShouldRecenter, sr);

                int pulse = 0;
                if (_haveSignal && _idxRecenterEvent >= 0)
                    pulse = (_prevShouldRecenter == 0 && sr == 1) ? 1 : 0;

                if (_idxRecenterEvent >= 0) row.Set(_idxRecenterEvent, pulse);

                _prevShouldRecenter = sr;
                _haveSignal = true;
            }
            
            // NEW: Recenter count
            if (_idxRecenterCount >= 0 && TryGetRecenterCount(out int count))
            {
                row.Set(_idxRecenterCount, count);
            }
            
            // NEW: Tracking origin change event (event-based, most precise)
            if (_idxTrackingOriginChangeEvent >= 0)
            {
                // Write pulse (1 for this frame if event fired, else 0)
                row.Set(_idxTrackingOriginChangeEvent, _trackingOriginChangePulse ? 1 : 0);
                
                // Write previous pose if available
                if (_trackingOriginChangePulse && _trackingOriginChangePrevPose.HasValue)
                {
                    Posef prevPose = _trackingOriginChangePrevPose.Value;
                    // Note: Previous pose is in tracking space, convert to world space
                    Vector3 worldPos = TrackingSpaceConverter.ToWorldSpacePosition(prevPose);
                    SetIfValid(row, _idxTrackingOriginChangePrevPx, worldPos.x);
                    SetIfValid(row, _idxTrackingOriginChangePrevPy, worldPos.y);
                    SetIfValid(row, _idxTrackingOriginChangePrevPz, worldPos.z);
                    
                    Quaternion worldRot = TrackingSpaceConverter.ToWorldSpaceRotation(prevPose.Orientation);
                    SetIfValid(row, _idxTrackingOriginChangePrevQx, worldRot.x);
                    SetIfValid(row, _idxTrackingOriginChangePrevQy, worldRot.y);
                    SetIfValid(row, _idxTrackingOriginChangePrevQz, worldRot.z);
                    SetIfValid(row, _idxTrackingOriginChangePrevQw, worldRot.w);
                }
                
                // Clear pulse flag after writing
                _trackingOriginChangePulse = false;
                _trackingOriginChangePrevPose = null;
            }
            
            // NEW: Tracking transform
            if (_idxTrackingTransformPx >= 0 && TryGetTrackingTransform(out Posef pose))
            {
                // Convert to world space
                Vector3 worldPos = TrackingSpaceConverter.ToWorldSpacePosition(pose);
                SetIfValid(row, _idxTrackingTransformPx, worldPos.x);
                SetIfValid(row, _idxTrackingTransformPy, worldPos.y);
                SetIfValid(row, _idxTrackingTransformPz, worldPos.z);
                
                Quaternion worldRot = TrackingSpaceConverter.ToWorldSpaceRotation(pose.Orientation);
                SetIfValid(row, _idxTrackingTransformQx, worldRot.x);
                SetIfValid(row, _idxTrackingTransformQy, worldRot.y);
                SetIfValid(row, _idxTrackingTransformQz, worldRot.z);
                SetIfValid(row, _idxTrackingTransformQw, worldRot.w);
            }
            
            // NEW: User presence
            if (_idxUserPresent >= 0 && TryGetUserPresent(out bool present))
            {
                row.Set(_idxUserPresent, present ? 1 : 0);
            }
            
            // NEW: Tracking loss
            if (_idxTrackingLost >= 0 && TryGetTrackingLost(out bool lost))
            {
                row.Set(_idxTrackingLost, lost ? 1 : 0);
            }
        }

        public void Dispose()
        {
            // Unsubscribe from event to prevent memory leak
            OVRManager.TrackingOriginChangePending -= OnTrackingOriginChangePending;
        }

        // --- Plugin wrappers ---
        
        private static bool TryGetShouldRecenter(out int value)
        {
            try
            {
                value = OVRPlugin.shouldRecenter ? 1 : 0;
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        // NEW: Get recenter count
        private static bool TryGetRecenterCount(out int value)
        {
            try
            {
                value = OVRPlugin.GetLocalTrackingSpaceRecenterCount();
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        // NEW: Get tracking transform
        private static bool TryGetTrackingTransform(out Posef value)
        {
            try
            {
                value = OVRPlugin.GetTrackingTransformRawPose();
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        // NEW: Get user present
        private static bool TryGetUserPresent(out bool value)
        {
            try
            {
                value = OVRPlugin.userPresent;
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        // NEW: Get tracking lost
        private static bool TryGetTrackingLost(out bool value)
        {
            try
            {
                // Tracking is lost if EyeCenter position is NOT tracked
                value = !GetNodePositionTracked(Node.EyeCenter);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        // NEW: Tracking origin change event handler
        private void OnTrackingOriginChangePending(OVRManager.TrackingOrigin trackingOrigin, OVRPose? poseInPreviousSpace)
        {
            _trackingOriginChangePulse = true;
            
            // Store previous pose if available
            if (poseInPreviousSpace.HasValue)
            {
                OVRPose ovrPose = poseInPreviousSpace.Value;
                _trackingOriginChangePrevPose = new Posef
                {
                    Position = new Vector3f { x = ovrPose.position.x, y = ovrPose.position.y, z = ovrPose.position.z },
                    Orientation = new Quatf { x = ovrPose.orientation.x, y = ovrPose.orientation.y, z = ovrPose.orientation.z, w = ovrPose.orientation.w }
                };
            }
        }
    }
}
