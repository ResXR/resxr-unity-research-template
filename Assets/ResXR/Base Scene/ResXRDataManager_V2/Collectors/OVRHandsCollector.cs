// OVRHandsCollector.cs
// Collects hand skeleton data from OVRPlugin.GetHandState().
// Logs status, root pose, scale, confidences, per-finger confidences, timestamps, and bone arrays.

using System;
using UnityEngine;
using static OVRPlugin;   // HandState, Skeleton, Posef, Hand, Step
using static ResXRData.CollectorUtils;
namespace ResXRData
{
    public sealed class OVRHandsCollector : IContinuousCollector
    {
        public string CollectorName => "OVRHandsCollector";
        private const Step SampleStep = OvrSampling.StepDefault;

        private struct HandCols
        {
            // Hand Status: 5 flag columns instead of single string column
            public int[] StatusFlags; // [HandTracked, InputStateValid, SystemGestureInProgress, DominantHand, MenuPressed]
            
            public int RootPosX, RootPosY, RootPosZ;
            public int RootQx, RootQy, RootQz, RootQw;
            public int HandScale;
            // Hand Confidence: single column, will write 0/1 (0=Low, 1=High) instead of string
            public int HandConfidence;

            // Finger Confidence: single columns, will write 0/1 (0=Low, 1=High) instead of string
            public int ConfThumb, ConfIndex, ConfMiddle, ConfRing, ConfPinky;

            public int RequestedTs;
            public int SampleTs;

            public int[] BonePosX;
            public int[] BonePosY;
            public int[] BonePosZ;
            public int[] BoneQx;
            public int[] BoneQy;
            public int[] BoneQz;
            public int[] BoneQw;
        }

        private HandCols _leftCols;
        private HandCols _rightCols;

        private int _handBoneCount = 0;
        private bool _includeHands = false;

        public void Configure(ColumnIndex schema, RecordingOptions options)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (options == null) options = new RecordingOptions();

            _includeHands = options.includeHands;
            if (!_includeHands) return;

            // Use the same count the schema was built with
            _handBoneCount = SchemaFactories.DetectHandBoneCount(out bool handDetectionOk);
            if (!handDetectionOk)
            {
                Debug.LogError($"[OVRHandsCollector] Hand bone count detection failed. No hand bones will be outputted, defaulting to 0 bones.");
                _handBoneCount = 0;
            }

            _leftCols = CacheHandIndices(schema, "Left");
            _rightCols = CacheHandIndices(schema, "Right");
        }

        public void Collect(RowBuffer row, float timeSinceStartup)
        {
            if (!_includeHands) return;

            WriteHand(row, Hand.HandLeft, _leftCols);
            WriteHand(row, Hand.HandRight, _rightCols);
        }

        public void Dispose() { }

        // --- helpers ---

        private HandCols CacheHandIndices(ColumnIndex schema, string side)
        {
            HandCols cols = new HandCols();

            string[] handBoneNames = SchemaFactories.GetHandBonesNames(out bool handBoneNamesOk);
            if (!handBoneNamesOk || handBoneNames.Length != _handBoneCount)
            {
                Debug.LogError($"[SchemaFactories] Hand bone names detection failed or count mismatch. Detected count: {_handBoneCount}, Names count: {handBoneNames.Length}");
            }

            // Cache indices for Hand Status flags (5 columns)
            cols.StatusFlags = new int[5];
            cols.StatusFlags[0] = IndexOrMinusOne(schema, $"{side}Hand_Status_HandTracked");
            cols.StatusFlags[1] = IndexOrMinusOne(schema, $"{side}Hand_Status_InputStateValid");
            cols.StatusFlags[2] = IndexOrMinusOne(schema, $"{side}Hand_Status_SystemGestureInProgress");
            cols.StatusFlags[3] = IndexOrMinusOne(schema, $"{side}Hand_Status_DominantHand");
            cols.StatusFlags[4] = IndexOrMinusOne(schema, $"{side}Hand_Status_MenuPressed");

            cols.RootPosX = IndexOrMinusOne(schema, $"{side}Hand_Root_px");
            cols.RootPosY = IndexOrMinusOne(schema, $"{side}Hand_Root_py");
            cols.RootPosZ = IndexOrMinusOne(schema, $"{side}Hand_Root_pz");

            cols.RootQx = IndexOrMinusOne(schema, $"{side}Hand_Root_qx");
            cols.RootQy = IndexOrMinusOne(schema, $"{side}Hand_Root_qy");
            cols.RootQz = IndexOrMinusOne(schema, $"{side}Hand_Root_qz");
            cols.RootQw = IndexOrMinusOne(schema, $"{side}Hand_Root_qw");

            cols.HandScale = IndexOrMinusOne(schema, $"{side}Hand_HandScale");
            cols.HandConfidence = IndexOrMinusOne(schema, $"{side}Hand_HandConfidence");

            cols.ConfThumb = IndexOrMinusOne(schema, $"{side}Hand_FingerConf_Thumb");
            cols.ConfIndex = IndexOrMinusOne(schema, $"{side}Hand_FingerConf_Index");
            cols.ConfMiddle = IndexOrMinusOne(schema, $"{side}Hand_FingerConf_Middle");
            cols.ConfRing = IndexOrMinusOne(schema, $"{side}Hand_FingerConf_Ring");
            cols.ConfPinky = IndexOrMinusOne(schema, $"{side}Hand_FingerConf_Pinky");

            cols.RequestedTs = IndexOrMinusOne(schema, $"{side}Hand_RequestedTS");
            cols.SampleTs = IndexOrMinusOne(schema, $"{side}Hand_SampleTS");

            cols.BonePosX = new int[_handBoneCount];
            cols.BonePosY = new int[_handBoneCount];
            cols.BonePosZ = new int[_handBoneCount];
            cols.BoneQx = new int[_handBoneCount];
            cols.BoneQy = new int[_handBoneCount];
            cols.BoneQz = new int[_handBoneCount];
            cols.BoneQw = new int[_handBoneCount];

            for (int i = 0; i < _handBoneCount; i++)
            {
                string boneName = handBoneNames[i];
                cols.BonePosX[i] = IndexOrMinusOne(schema, $"{side}_{boneName}_x");
                cols.BonePosY[i] = IndexOrMinusOne(schema, $"{side}_{boneName}_y");
                cols.BonePosZ[i] = IndexOrMinusOne(schema, $"{side}_{boneName}_z");

                cols.BoneQx[i] = IndexOrMinusOne(schema, $"{side}_{boneName}_qx");
                cols.BoneQy[i] = IndexOrMinusOne(schema, $"{side}_{boneName}_qy");
                cols.BoneQz[i] = IndexOrMinusOne(schema, $"{side}_{boneName}_qz");
                cols.BoneQw[i] = IndexOrMinusOne(schema, $"{side}_{boneName}_qw");
            }

            return cols;
        }

        private void WriteHand(RowBuffer row, Hand whichHand, HandCols cols)
        {
            HandState handState = default;

            // NOTE: signature is (Step, Hand, ref HandState)
            bool got = GetHandState(SampleStep, whichHand, ref handState);
            if (!got) return;

            // Parse Hand Status flags and write to individual flag columns
            bool[] statusFlags = ParseHandStatusFlags(handState.Status);
            for (int i = 0; i < statusFlags.Length && i < cols.StatusFlags.Length; i++)
            {
                SetIfValid(row, cols.StatusFlags[i], statusFlags[i] ? 1 : 0);
            }

            // Convert hand root pose to world space
            Posef root = handState.RootPose;
            Vector3 rootWorldPos = TrackingSpaceConverter.ToWorldSpacePosition(root);
            SetIfValid(row, cols.RootPosX, rootWorldPos.x);
            SetIfValid(row, cols.RootPosY, rootWorldPos.y);
            SetIfValid(row, cols.RootPosZ, rootWorldPos.z);
            
            Quaternion rootWorldRot = TrackingSpaceConverter.ToWorldSpaceRotation(root.Orientation);
            SetIfValid(row, cols.RootQx, rootWorldRot.x);
            SetIfValid(row, cols.RootQy, rootWorldRot.y);
            SetIfValid(row, cols.RootQz, rootWorldRot.z);
            SetIfValid(row, cols.RootQw, rootWorldRot.w);

            SetIfValid(row, cols.HandScale, handState.HandScale);
            // Hand Confidence: Convert to binary (0=Low, 1=High) instead of string
            SetIfValid(row, cols.HandConfidence, ParseTrackingConfidenceToBinary(handState.HandConfidence));

            // Finger confidences: Convert to binary (0=Low, 1=High) instead of string
            if (handState.FingerConfidences != null && handState.FingerConfidences.Length >= 5)
            {
                SetIfValid(row, cols.ConfThumb, ParseTrackingConfidenceToBinary(handState.FingerConfidences[0]));
                SetIfValid(row, cols.ConfIndex, ParseTrackingConfidenceToBinary(handState.FingerConfidences[1]));
                SetIfValid(row, cols.ConfMiddle, ParseTrackingConfidenceToBinary(handState.FingerConfidences[2]));
                SetIfValid(row, cols.ConfRing, ParseTrackingConfidenceToBinary(handState.FingerConfidences[3]));
                SetIfValid(row, cols.ConfPinky, ParseTrackingConfidenceToBinary(handState.FingerConfidences[4]));
            }

            // Timestamps (double)
            SetIfValid(row, cols.RequestedTs, handState.RequestedTimeStamp);
            SetIfValid(row, cols.SampleTs, handState.SampleTimeStamp);

            // Bone arrays
            int positionsCount = handState.BonePositions != null ? handState.BonePositions.Length : 0;
            int rotationsCount = handState.BoneRotations != null ? handState.BoneRotations.Length : 0;

            for (int i = 0; i < positionsCount; i++)
            {
                Vector3f bonePositions = handState.BonePositions[i];
                SetIfValid(row, cols.BonePosX[i], bonePositions.x);
                SetIfValid(row, cols.BonePosY[i], bonePositions.y);
                SetIfValid(row, cols.BonePosZ[i], bonePositions.z);

            }

            for (int i = 0; i < rotationsCount; i++)
            {
                Quatf boneRotations = handState.BoneRotations[i];
                SetIfValid(row, cols.BoneQx[i], boneRotations.x);
                SetIfValid(row, cols.BoneQy[i], boneRotations.y);
                SetIfValid(row, cols.BoneQz[i], boneRotations.z);
                SetIfValid(row, cols.BoneQw[i], boneRotations.w);
            }
        }
    }
}
