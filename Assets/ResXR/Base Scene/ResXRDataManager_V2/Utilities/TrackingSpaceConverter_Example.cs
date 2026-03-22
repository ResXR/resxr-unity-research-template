// TrackingSpaceConverter_Example.cs
// Example of how to use TrackingSpaceConverter in your collectors

using UnityEngine;
using static OVRPlugin;

namespace ResXRData
{
    public class TrackingSpaceConverterExample
    {
        // Example 1: Get head position in world space from OVRPlugin (with timestamp)
        public static void ExampleGetHeadWorldPosition()
        {
            // Get raw tracking space pose with timestamp from OVRPlugin
            PoseStatef headState = GetNodePoseStateRaw(Node.Head, Step.Render);

            // Convert to world space
            Vector3 worldPos = TrackingSpaceConverter.ToWorldSpacePosition(headState.Pose);

            // Now you have:
            // - worldPos: position in Unity world space (for scene context)
            // - headState.Time: precise timestamp from OVRPlugin
            // - headState.Velocity: velocity data

            Debug.Log($"Head world position: {worldPos}, Time: {headState.Time}");
        }

        // Example 2: Use in a collector (like OVRNodesCollector)
        public static void ExampleCollectorUsage(RowBuffer row, ColumnIndex schema)
        {
            // Get raw pose from OVRPlugin
            Posef headPose = GetNodePose(Node.Head, Step.Render);
            PoseStatef headState = GetNodePoseStateRaw(Node.Head, Step.Render);

            // Option A: Log raw tracking space (hypothetical example)
            int idxTrackingX;
            if (schema.TryGetIndex("Head_Tracking_x", out idxTrackingX))
            {
                row.Set(idxTrackingX, headPose.Position.x);
            }

            // Option B: Log world space (NEW!)
            int idxWorldX;
            if (schema.TryGetIndex("Head_World_x", out idxWorldX))
            {
                Vector3 worldPos = TrackingSpaceConverter.ToWorldSpacePosition(headPose);
                row.Set(idxWorldX, worldPos.x);
            }

            // Option C: Log timestamp from OVRPlugin
            int idxTime;
            if (schema.TryGetIndex("Head_Time", out idxTime))
            {
                row.Set(idxTime, headState.Time);
            }
        }

        // Example 3: Initialize at startup (in ResXRDataManager_V2.Awake)
        public static void ExampleInitialization()
        {
            // Find OVRCameraRig in scene
            OVRCameraRig cameraRig = GameObject.FindAnyObjectByType<OVRCameraRig>();

            if (cameraRig != null && cameraRig.trackingSpace != null)
            {
                // Initialize converter with TrackingSpace transform
                TrackingSpaceConverter.Initialize(cameraRig.trackingSpace);
                Debug.Log("[TrackingSpaceConverter] Initialized with OVRCameraRig");
            }
            else
            {
                Debug.LogError("[TrackingSpaceConverter] OVRCameraRig not found!");
            }
        }

        // Example 4: Update cache if rig moves (optional, for dynamic rigs)
        public static void ExampleUpdateCache()
        {
            // If your OVRCameraRig moves during gameplay, call this each frame:
            TrackingSpaceConverter.UpdateCache();

            // Or update only when rig moves:
            // if (rigMoved) { TrackingSpaceConverter.UpdateCache(); }
        }

        // Example 5: Get both tracking and world space for comparison
        public static void ExampleDualLogging()
        {
            Posef headPose = GetNodePose(Node.Head, Step.Render);

            // Tracking space (raw OVRPlugin)
            Vector3 trackingPos = new Vector3(
                headPose.Position.x,
                headPose.Position.y,
                headPose.Position.z
            );

            // World space (converted)
            Vector3 worldPos = TrackingSpaceConverter.ToWorldSpacePosition(headPose);

            Debug.Log($"Tracking: {trackingPos}, World: {worldPos}, Rig offset: {TrackingSpaceConverter.GetRigOffset()}");
        }
    }
}
