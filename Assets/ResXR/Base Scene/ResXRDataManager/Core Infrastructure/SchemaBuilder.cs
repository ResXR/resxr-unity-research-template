// SchemaBuilder.cs
// Builds column-name schemas. Also includes runtime-detecting factories and face-expression names from the SDK.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;                  // Debug.LogWarning
using UnityEngine.Serialization;    // FormerlySerializedAs
using static OVRPlugin;             // runtime skeleton detection

namespace ResXRData
{
    // Thin builder that only deals in column names.
    public sealed class SchemaBuilder
    {
        private readonly List<string> _names = new();

        public SchemaBuilder Add(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Column name cannot be empty", nameof(name));
            _names.Add(name);
            return this;
        }

        // Adds columns like $"{prefix}_{item}" for each item.
        // e.g. prefix=HeadPos, items=[X,Y,Z] -> HeadPos_X, HeadPos_Y, HeadPos_Z
        // If prefix is empty or null, just adds the items as-is.
        public SchemaBuilder AddMany(string prefix, IEnumerable<string> items)
        {
            if (!string.IsNullOrWhiteSpace(prefix))
                prefix = $"{prefix}_";

            foreach (string item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                    throw new ArgumentException("Column item name cannot be empty in AddMany");
                _names.Add($"{prefix}{item}");
            }

            return this;
        }

        // Adds a numbered sequence: $"{prefix}_{index}" with formatting.
        public SchemaBuilder AddFromRange(string prefix, int startInclusive, int count, string indexFormat = "D2")
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            for (int offset = 0; offset < count; offset++)
                _names.Add($"{prefix}_{(startInclusive + offset).ToString(indexFormat)}");

            return this;
        }

        // Add one column per enum value.
        // Example:
        //   enum MuseumZone { Lobby, RoomA, RoomB }
        //   builder.AddFromEnum<MuseumZone>("TimeInZone");
        // -> TimeInZone_Lobby, TimeInZone_RoomA, TimeInZone_RoomB
        public SchemaBuilder AddFromEnum<TEnum>(string prefix) where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("prefix cannot be empty", nameof(prefix));

            foreach (string enumName in Enum.GetNames(typeof(TEnum)))
                _names.Add($"{prefix}_{enumName}");

            return this;
        }

        public ColumnIndex Build()
        {
            ColumnIndex columnIndex = new ColumnIndex();
            foreach (string name in _names)
                columnIndex.Add(name);
            return columnIndex;
        }
    }

    // Options that control what goes into the ContinuousData schema (exposed in DataManager inspector).
    [Serializable]
    public sealed class RecordingOptions
    {
        [Tooltip("Device node poses: Node_EyeCenter, Node_Head, Node_HandLeft, Node_HandRight, Node_ControllerLeft, Node_ControllerRight. Each includes Present, position (px/py/pz), rotation (qx/qy/qz/qw), Valid_Position/Orientation, Tracked_Position/Orientation, Time.")]
        public bool includeNodes = true;

        [Tooltip("Dedicated eye API: RightEye/LeftEye quaternion (qx,qy,qz,qw), LeftEye_IsValid, LeftEye_Confidence, RightEye_IsValid, RightEye_Confidence, Eyes_Time. World-space orientation from OVR.")]
        public bool includeEyes = true;

        [Tooltip("Hand skeleton: per-hand status flags, root pose, finger confidences, bone positions and rotations (world space). One block per hand (Left/Right).")]
        public bool includeHands = true;

        [Tooltip("Body skeleton: body joint positions and rotations (world space), calibration status, fidelity. Recommended to leave OFF for now—Meta SDK body data is an estimation for smooth avatar animation, not from an actual body sensor. This option is kept for future headsets that may provide real body tracking.")]
        public bool includeBody = true;

        [Tooltip("Performance: AppMotionToPhotonLatency (motion-to-photon latency in seconds).")]
        public bool includePerformance = true;

        [Tooltip("Combined (cyclopean) gaze: FocusedObject (name of object under binocular gaze), EyeGazeHitPosition_X/Y/Z (world hit point). Requires both eyes confident.")]
        public bool includeGaze = true;

        [Tooltip("Per-eye gaze: LeftEyeGazeHitPosition_X/Y/Z, RightEyeGazeHitPosition_X/Y/Z, LeftFocusedObject, RightFocusedObject, HasLeftEyeHit, HasRightEyeHit. Enables 3 raycasts per frame instead of 1. In heavy scenes with many colliders this may reduce performance—consider turning off.")]
        public bool includeSeparateEyesGaze = false;

        [FormerlySerializedAs("includeRecenter")]
        [Tooltip("System status: recenter detection (shouldRecenter, recenterEvent, RecenterCount, TrackingOriginChange event+pose), tracking space transform (TrackingTransform_*), user presence (UserPresent), tracking loss (TrackingLost)")]
        public bool includeSystemStatus = true;

        [Tooltip("Drag Transforms here to record their world position and rotation every frame in ContinuousData. For each transform, columns are added: Custom_<TransformName>_px/py/pz (position) and Custom_<TransformName>_qx/qy/qz/qw (rotation).")]
        public List<Transform> customTransformsToRecord = new();
    }

    public static class SchemaFactories
    {
        // --------- Runtime detection helpers ---------

        public static int DetectHandBoneCount(out bool success)
        {
            success = false;

            OVRHandSkeletonVersion handSkeletonVersion = OVRRuntimeSettings.Instance.HandSkeletonVersion;

            if (handSkeletonVersion == OVRHandSkeletonVersion.OpenXR)
            {
                // OpenXR hands path
                success = true;
                return (int)OVRPlugin.SkeletonConstants.MaxXRHandBones; // TODO should we use getskeleton2 instead? has bones[] array
            }
            else if (handSkeletonVersion == OVRHandSkeletonVersion.OVR)
            {
                // OVR hands path
                success = true;
                return (int)OVRPlugin.SkeletonConstants.MaxHandBones;
            }

            return 0;
        }

        public static int DetectBodyJointCount(out bool success)
        {
            success = false;
            try
            {
                Skeleton2 skel = new Skeleton2();
                if (GetSkeleton2(SkeletonType.Body, ref skel) && skel.NumBones > 0)
                {
                    success = true;
                    return (int)skel.NumBones;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SchemaFactories] Body skeleton detection failed: {e.Message}");
            }
            Debug.LogWarning("[SchemaFactories] Body skeleton detection failed, defaulting to SkeletonConstants.MaxBodyBones.");
            return (int)SkeletonConstants.MaxBodyBones; // safe fallback (may over-provision)
        }

        public static string[] GetHandBonesNames(out bool success)
        {
            success = false;
            OVRHandSkeletonVersion handSkeletonVersion = OVRRuntimeSettings.Instance.HandSkeletonVersion;

            // Get all enum values of BoneId
            string[] allNames = Enum.GetNames(typeof(BoneId));

            if (handSkeletonVersion == OVRHandSkeletonVersion.OpenXR)
            {
                // Filter for only XRHand bones
                // Exactly the 26 XR joints (excluding Start/Max/End)
                string[] xrHandNames = allNames
                    .Where(n => n.StartsWith("XRHand_"))
                    .Where(n => n != nameof(BoneId.XRHand_Start) &&
                                n != nameof(BoneId.XRHand_Max) &&
                                n != nameof(BoneId.XRHand_End))
                    .ToArray();

                success = xrHandNames.Length == 26;
                return xrHandNames;
            }
            else if (handSkeletonVersion == OVRHandSkeletonVersion.OVR)
            {
                // Filter for only OVR bones
                string[] ovrAllHandNames = allNames
                    .Where(n => n.StartsWith("Hand_"))
                    .Where(n => n != nameof(BoneId.Hand_Start) &&
                                n != nameof(BoneId.Hand_MaxSkinnable) &&
                                n != nameof(BoneId.Hand_End))
                    .ToArray();
                success = ovrAllHandNames.Length > 0;
                return ovrAllHandNames;
            }
            return Array.Empty<string>();
        }

        public static string[] GetBodyJointNames()
        {
            // Get all enum values of BoneId
            string[] allNames = Enum.GetNames(typeof(BoneId));

            // Filter for only FullBody joints
            string[] bodyJointNames = allNames
                .Where(n => n.StartsWith("Body"))
                .Where(n => n != nameof(BoneId.Body_Start) &&
                            n != nameof(BoneId.Body_End))
                .ToArray();
            return bodyJointNames;
        }

        // --------- Schema builders ---------

        // Build ContinuousData schema based on options and runtime counts.
        // Returns schema and the detected counts + over-provision flags (for metadata).
        public static (ColumnIndex schema, int handBones, bool handOverprovisioned,
                       int bodyJoints, bool bodyOverprovisioned)
            BuildContinuousDataV2(RecordingOptions recordingOptions)
        {
            SchemaBuilder schemaBuilder = new SchemaBuilder();
            int detectedHandBoneCount = 0;
            bool handDetectionOk = false;
            if (recordingOptions.includeHands) detectedHandBoneCount = DetectHandBoneCount(out handDetectionOk);
            int detectedBodyJointCount = 0;
            bool bodyDetectionOk = false;
            if (recordingOptions.includeBody) detectedBodyJointCount = DetectBodyJointCount(out bodyDetectionOk);

            // Timing
            schemaBuilder.Add("timeSinceStartup"); // Unity Time.realTimeSinceStartup

            // Legacy gaze / raycast
            if (recordingOptions.includeGaze)
            {
                schemaBuilder.Add("FocusedObject");
                schemaBuilder.AddMany("EyeGazeHitPosition", new[] { "X", "Y", "Z" });
            }

            // Per-eye gaze (independent of includeGaze)
            if (recordingOptions.includeSeparateEyesGaze)
            {
                schemaBuilder.AddMany("LeftEyeGazeHitPosition", new[] { "X", "Y", "Z" });
                schemaBuilder.AddMany("RightEyeGazeHitPosition", new[] { "X", "Y", "Z" });
                schemaBuilder.Add("LeftFocusedObject");
                schemaBuilder.Add("RightFocusedObject");
                schemaBuilder.Add("HasLeftEyeHit");
                schemaBuilder.Add("HasRightEyeHit");
            }

            // Eyes (dedicated API)
            if (recordingOptions.includeEyes)
            {
                schemaBuilder.AddMany("RightEye", new[] { "qx", "qy", "qz", "qw" });
                schemaBuilder.AddMany("LeftEye", new[] { "qx", "qy", "qz", "qw" });
                schemaBuilder.Add("LeftEye_IsValid");
                schemaBuilder.Add("LeftEye_Confidence");
                schemaBuilder.Add("RightEye_IsValid");
                schemaBuilder.Add("RightEye_Confidence");
                schemaBuilder.Add("Eyes_Time");
            }

            // System status (recenter, tracking space, user presence, tracking loss)
            if (recordingOptions.includeSystemStatus)
            {
                // Recenter detection (existing + new)
                schemaBuilder.Add("shouldRecenter");
                schemaBuilder.Add("recenterEvent");
                schemaBuilder.Add("RecenterCount");
                
                // Recenter event with pose delta (NEW)
                schemaBuilder.Add("TrackingOriginChange_Event");
                schemaBuilder.AddMany("TrackingOriginChange_PrevPose", new[] { "px", "py", "pz" });
                schemaBuilder.AddMany("TrackingOriginChange_PrevPose", new[] { "qx", "qy", "qz", "qw" });
                
                // Tracking space transform
                schemaBuilder.AddMany("TrackingTransform", new[] { "px", "py", "pz" });
                schemaBuilder.AddMany("TrackingTransform", new[] { "qx", "qy", "qz", "qw" });
                
                // User presence & tracking loss
                schemaBuilder.Add("UserPresent");
                schemaBuilder.Add("TrackingLost");
            }

            // Device nodes
            if (recordingOptions.includeNodes)
            {
                // EyeLeft, EyeRight removed - use dedicated eye gaze API (OVREyesCollector) instead
                string[] nodeNames =
                {
                    "EyeCenter","Head",
                    "HandLeft","HandRight","ControllerLeft","ControllerRight"
                };

                foreach (string node in nodeNames)
                {
                    schemaBuilder.Add($"Node_{node}_Present");
                    schemaBuilder.AddMany($"Node_{node}", new[] { "px", "py", "pz" });
                    schemaBuilder.AddMany($"Node_{node}", new[] { "qx", "qy", "qz", "qw" });
                    
                    // Velocity/AngularVelocity removed for all nodes
                    
                    schemaBuilder.Add($"Node_{node}_Valid_Position");
                    schemaBuilder.Add($"Node_{node}_Valid_Orientation");
                    schemaBuilder.Add($"Node_{node}_Tracked_Position");
                    schemaBuilder.Add($"Node_{node}_Tracked_Orientation");
                    schemaBuilder.Add($"Node_{node}_Time");
                }
            }

            // Hands (skeleton arrays)
            if (recordingOptions.includeHands)
            {
                string[] handBoneNames = GetHandBonesNames(out bool handBoneNamesOk);
                if (!handBoneNamesOk || handBoneNames.Length != detectedHandBoneCount)
                {
                    Debug.LogError($"[SchemaFactories] Hand bone names detection failed or count mismatch. Detected count: {detectedHandBoneCount}, Names count: {handBoneNames.Length}");
                }

                foreach (string side in new[] { "Left", "Right" })
                {
                    // Hand Status: 5 flag columns instead of single string column
                    schemaBuilder.Add($"{side}Hand_Status_HandTracked");
                    schemaBuilder.Add($"{side}Hand_Status_InputStateValid");
                    schemaBuilder.Add($"{side}Hand_Status_SystemGestureInProgress");
                    schemaBuilder.Add($"{side}Hand_Status_DominantHand");
                    schemaBuilder.Add($"{side}Hand_Status_MenuPressed");
                    
                    schemaBuilder.AddMany($"{side}Hand_Root", new[] { "px", "py", "pz" });
                    schemaBuilder.AddMany($"{side}Hand_Root", new[] { "qx", "qy", "qz", "qw" });
                    schemaBuilder.Add($"{side}Hand_HandScale");
                    // Hand Confidence: Keep same column name, but will write 0/1 (0=Low, 1=High) instead of string
                    schemaBuilder.Add($"{side}Hand_HandConfidence");

                    // Finger Confidence: Keep same column names, but will write 0/1 (0=Low, 1=High) instead of string
                    schemaBuilder.Add($"{side}Hand_FingerConf_Thumb");
                    schemaBuilder.Add($"{side}Hand_FingerConf_Index");
                    schemaBuilder.Add($"{side}Hand_FingerConf_Middle");
                    schemaBuilder.Add($"{side}Hand_FingerConf_Ring");
                    schemaBuilder.Add($"{side}Hand_FingerConf_Pinky");

                    schemaBuilder.Add($"{side}Hand_RequestedTS");
                    schemaBuilder.Add($"{side}Hand_SampleTS");

                    for (int boneIndex = 0; boneIndex < detectedHandBoneCount; boneIndex++)
                    {

                        string boneName = handBoneNames[boneIndex];
                        schemaBuilder.AddMany($"{side}_{boneName}", new[] { "x", "y", "z" });
                        schemaBuilder.AddMany($"{side}_{boneName}", new[] { "qx", "qy", "qz", "qw" });

                    }
                }
            }

            // Body (skeleton arrays)
            if (recordingOptions.includeBody)
            {
                schemaBuilder.Add("Body_Time");
                schemaBuilder.Add("Body_Confidence");
                // Body Fidelity: Keep same column name, but will write 0/1 (0=Low, 1=High) instead of string
                schemaBuilder.Add("Body_Fidelity");
                // Body Calibration Status: 3 flag columns instead of single string column
                schemaBuilder.Add("Body_CalibrationStatus_Invalid");
                schemaBuilder.Add("Body_CalibrationStatus_Calibrating");
                schemaBuilder.Add("Body_CalibrationStatus_Valid");
                schemaBuilder.Add("Body_SkeletonChangedCount");

                string[] bodyJointNames = GetBodyJointNames();
                if (bodyJointNames.Length != detectedBodyJointCount)
                {
                    Debug.LogError($"[SchemaFactories] Body joint names count mismatch. Detected count: {detectedBodyJointCount}, Names count: {bodyJointNames.Length}");
                }

                for (int jointIndex = 0; jointIndex < detectedBodyJointCount; jointIndex++)
                {
                    if (jointIndex < bodyJointNames.Length)
                    {
                        string jointName = bodyJointNames[jointIndex];
                        schemaBuilder.AddMany($"{jointName}", new[] { "px", "py", "pz" });
                        schemaBuilder.AddMany($"{jointName}", new[] { "qx", "qy", "qz", "qw" });
                        // Joint Flags: 4 flag columns instead of single string column
                        schemaBuilder.Add($"{jointName}_Flags_OrientationValid");
                        schemaBuilder.Add($"{jointName}_Flags_PositionValid");
                        schemaBuilder.Add($"{jointName}_Flags_OrientationTracked");
                        schemaBuilder.Add($"{jointName}_Flags_PositionTracked");
                    }
                }
            }

            // Perf - AppMotionToPhotonLatency removed (keep OVRPerformanceCollector for future metrics)

            // Custom transforms (from inspector)
            if (recordingOptions.customTransformsToRecord != null)
            {
                foreach (Transform transform in recordingOptions.customTransformsToRecord)
                {
                    if (transform == null) continue;
                    string name = transform.name;
                    schemaBuilder.AddMany($"Custom_{name}", new[] { "px", "py", "pz" });
                    schemaBuilder.AddMany($"Custom_{name}", new[] { "qx", "qy", "qz", "qw" });
                }
            }

            ColumnIndex schema = schemaBuilder.Build();
            bool handOverprovisioned = !handDetectionOk;
            bool bodyOverprovisioned = !bodyDetectionOk;

            return (schema, detectedHandBoneCount, handOverprovisioned, detectedBodyJointCount, bodyOverprovisioned);
        }

        // Build FaceExpressions schema using expression names from OVRPlugin.FaceExpression2, and region confidences from OVRPlugin.FaceRegionConfidence.
        // Returns schema and counts of expressions and confidences (for metadata).
        public static (ColumnIndex schema, int faceExprCount, int perExpressionConfidenceCount, int regionConfidenceCount)
        BuildFaceExpressionsV2()
        {
            SchemaBuilder schemaBuilder = new SchemaBuilder();

            // timing + state
            schemaBuilder.Add("timeSinceStartup");
            schemaBuilder.Add("Face_Time");
            schemaBuilder.Add("Face_Status");

            // expression names from enum (skip sentinels)
            List<string> expressionNames = new List<string>();
            foreach (string enumName in Enum.GetNames(typeof(OVRPlugin.FaceExpression2)))
            {
                if (enumName == "Invalid" || enumName == "Max") continue;
                expressionNames.Add(enumName);
            }
            int expressionCount = expressionNames.Count; // typically 70

            // weights: one column per expression name
            schemaBuilder.AddMany("", expressionNames); // e.g., Brow_Lowerer_L, Jaw_Drop, ...

            // region confidences: Upper/Lower (skip Max)
            List<string> regionNames = new List<string>();
            foreach (string region in Enum.GetNames(typeof(OVRPlugin.FaceRegionConfidence)))
            {
                if (region == "Max") continue;
                regionNames.Add(region);
            }
            schemaBuilder.AddMany("FaceRegionConfidence", regionNames); // RegionConf_Upper, RegionConf_Lower

            ColumnIndex built = schemaBuilder.Build();
            return (built, expressionCount, expressionCount, regionNames.Count);
        }
    }
}
