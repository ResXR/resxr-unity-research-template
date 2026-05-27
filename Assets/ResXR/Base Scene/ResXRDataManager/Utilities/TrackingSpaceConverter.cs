// TrackingSpaceConverter.cs
// Utility to convert OVRPlugin tracking space coordinates to Unity world space
// WITHOUT relying on Unity Transform updates (preserves OVRPlugin timestamps)

using UnityEngine;
using static OVRPlugin;

namespace ResXRData
{
    /// <summary>
    /// Converts OVRPlugin Posef (right-handed tracking space) to Unity world space (left-handed)
    /// by applying coordinate system conversion + OVRCameraRig offset
    /// </summary>
    public static class TrackingSpaceConverter
    {
        private static Transform _trackingSpaceTransform;
        private static Vector3 _cachedRigPosition;
        private static Quaternion _cachedRigRotation;
        private static bool _isInitialized = false;

        /// <summary>
        /// Initialize with the OVRCameraRig's TrackingSpace transform
        /// Call this once at startup (e.g., in ResXRDataManager_V2.Awake)
        /// </summary>
        public static void Initialize(Transform trackingSpace)
        {
            _trackingSpaceTransform = trackingSpace;
            UpdateCache();
            _isInitialized = true;
        }

        /// <summary>
        /// Update cached rig position (call if rig moves, or every frame for safety)
        /// </summary>
        public static void UpdateCache()
        {
            if (_trackingSpaceTransform != null)
            {
                _cachedRigPosition = _trackingSpaceTransform.position;
                _cachedRigRotation = _trackingSpaceTransform.rotation;
            }
        }

        /// <summary>
        /// Convert OVRPlugin Posef (tracking space) to Unity world space Vector3
        /// </summary>
        /// <param name="trackingSpacePose">Raw Posef from OVRPlugin (right-handed)</param>
        /// <returns>Position in Unity world space (left-handed)</returns>
        public static Vector3 ToWorldSpacePosition(Posef trackingSpacePose)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[TrackingSpaceConverter] Not initialized! Call Initialize() first.");
                return Vector3.zero;
            }

            // Step 1: Convert from right-handed (OVRPlugin) to left-handed (Unity)
            // This is the same conversion that OVRCommon.ToOVRPose does
            Vector3 trackingSpaceUnityCoords = new Vector3(
                trackingSpacePose.Position.x,
                trackingSpacePose.Position.y,
                -trackingSpacePose.Position.z  // ← Z-FLIP for handedness conversion
            );

            // Step 2: Apply rig rotation (if rig is rotated in scene)
            Vector3 rotated = _cachedRigRotation * trackingSpaceUnityCoords;

            // Step 3: Apply rig position offset
            Vector3 worldSpace = _cachedRigPosition + rotated;

            return worldSpace;
        }

        /// <summary>
        /// Convert OVRPlugin Quatf (tracking space) to Unity world space Quaternion
        /// </summary>
        public static Quaternion ToWorldSpaceRotation(Quatf trackingSpaceOrientation)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[TrackingSpaceConverter] Not initialized! Call Initialize() first.");
                return Quaternion.identity;
            }

            // Convert from right-handed to left-handed quaternion
            Quaternion trackingSpaceUnityCoords = new Quaternion(
                -trackingSpaceOrientation.x,
                -trackingSpaceOrientation.y,
                trackingSpaceOrientation.z,
                trackingSpaceOrientation.w
            );

            // Apply rig rotation
            Quaternion worldSpace = _cachedRigRotation * trackingSpaceUnityCoords;

            return worldSpace;
        }

        /// <summary>
        /// Convert full Posef to world space (position + rotation)
        /// </summary>
        public static (Vector3 position, Quaternion rotation) ToWorldSpace(Posef trackingSpacePose)
        {
            return (
                ToWorldSpacePosition(trackingSpacePose),
                ToWorldSpaceRotation(trackingSpacePose.Orientation)
            );
        }

        /// <summary>
        /// Convert velocity/angular velocity vector from tracking space to world space
        /// Applies rotation but NOT position offset (velocities are direction vectors)
        /// </summary>
        public static Vector3 RotateVectorToWorldSpace(Vector3f trackingSpaceVec)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[TrackingSpaceConverter] Not initialized! Call Initialize() first.");
                return Vector3.zero;
            }

            // Step 1: Z-flip for coordinate system conversion
            Vector3 unityCoords = new Vector3(
                trackingSpaceVec.x,
                trackingSpaceVec.y,
                -trackingSpaceVec.z  // ← Z-FLIP
            );

            // Step 2: Rotate by rig rotation (NO offset for velocities)
            return _cachedRigRotation * unityCoords;
        }

        /// <summary>
        /// Get current rig offset (for debugging/logging)
        /// </summary>
        public static Vector3 GetRigOffset()
        {
            return _cachedRigPosition;
        }
    }
}
