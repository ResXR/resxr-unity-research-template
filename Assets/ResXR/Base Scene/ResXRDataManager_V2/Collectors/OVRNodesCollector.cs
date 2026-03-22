// OVRNodesCollector.cs
// Collects node pose/validity blocks. [nodes: EyeCenter, Head, HandLeft, HandRight, ControllerLeft, ControllerRight. removed eyeleft, eyeright - use dedicated eye gaze collector instead]

using System;
using System.Collections.Generic;
using UnityEngine;
using static OVRPlugin; // Node, Step, Posef, Vector3f, API calls
using static ResXRData.CollectorUtils;

namespace ResXRData
{
    public sealed class OVRNodesCollector : IContinuousCollector
    {
        public string CollectorName => "OVRNodesCollector";
        private const Step SampleStep = OvrSampling.StepDefault;

        private struct NodeCols
        {
            public int Present;
            public int PosX, PosY, PosZ;
            public int Qx, Qy, Qz, Qw;
            public int ValidPos, ValidOrient, TrackedPos, TrackedOrient;
            public int Time;
        }

        private readonly Dictionary<Node, NodeCols> _nodeCols = new Dictionary<Node, NodeCols>();
        
        private static readonly Node[] NodeOrder = 
        {
            Node.EyeCenter, Node.Head,
            Node.HandLeft, Node.HandRight, Node.ControllerLeft, Node.ControllerRight
        }; // EyeLeft, EyeRight removed - use dedicated eye gaze API instead

        public void Configure(ColumnIndex schema, RecordingOptions options)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            _nodeCols.Clear();
            foreach (Node node in NodeOrder)
            {
                string baseName = $"Node_{node}";
                NodeCols cols = new NodeCols
                {
                    Present = IndexOrMinusOne(schema, $"{baseName}_Present"),

                    PosX = IndexOrMinusOne(schema, $"{baseName}_px"),
                    PosY = IndexOrMinusOne(schema, $"{baseName}_py"),
                    PosZ = IndexOrMinusOne(schema, $"{baseName}_pz"),

                    Qx = IndexOrMinusOne(schema, $"{baseName}_qx"),
                    Qy = IndexOrMinusOne(schema, $"{baseName}_qy"),
                    Qz = IndexOrMinusOne(schema, $"{baseName}_qz"),
                    Qw = IndexOrMinusOne(schema, $"{baseName}_qw"),

                    ValidPos = IndexOrMinusOne(schema, $"{baseName}_Valid_Position"),
                    ValidOrient = IndexOrMinusOne(schema, $"{baseName}_Valid_Orientation"),
                    TrackedPos = IndexOrMinusOne(schema, $"{baseName}_Tracked_Position"),
                    TrackedOrient = IndexOrMinusOne(schema, $"{baseName}_Tracked_Orientation"),

                    Time = IndexOrMinusOne(schema, $"{baseName}_Time"),
                };
                _nodeCols[node] = cols;
            }
        }

        public void Collect(RowBuffer row, float timeSinceStartup)
        {
            // ----- Per-node block -----
            foreach (KeyValuePair<Node, NodeCols> pair in _nodeCols)
            {
                Node node = pair.Key;
                NodeCols cols = pair.Value;

                bool present = GetNodePresent(node);
                SetIfValid(row, cols.Present, present ? 1 : 0);

                // Convert position to world space
                Posef pose = GetNodePose(node, SampleStep);
                Vector3 worldPos = TrackingSpaceConverter.ToWorldSpacePosition(pose);
                SetIfValid(row, cols.PosX, worldPos.x);
                SetIfValid(row, cols.PosY, worldPos.y);
                SetIfValid(row, cols.PosZ, worldPos.z);
                
                // Convert rotation to world space
                Quaternion worldRot = TrackingSpaceConverter.ToWorldSpaceRotation(pose.Orientation);
                SetIfValid(row, cols.Qx, worldRot.x);
                SetIfValid(row, cols.Qy, worldRot.y);
                SetIfValid(row, cols.Qz, worldRot.z);
                SetIfValid(row, cols.Qw, worldRot.w);

                // Velocity/AngularVelocity collection removed (not in schema)

                bool validPos = GetNodePositionValid(node);
                bool validOrient = GetNodeOrientationValid(node);
                bool trackedPos = GetNodePositionTracked(node);
                bool trackedOrient = GetNodeOrientationTracked(node);
                SetIfValid(row, cols.ValidPos, validPos ? 1 : 0);
                SetIfValid(row, cols.ValidOrient, validOrient ? 1 : 0);
                SetIfValid(row, cols.TrackedPos, trackedPos ? 1 : 0);
                SetIfValid(row, cols.TrackedOrient, trackedOrient ? 1 : 0);

                // Per-node time, if that column exists
                if (cols.Time >= 0)
                {
                    PoseStatef state = GetNodePoseStateRaw(node, SampleStep);
                    row.Set(cols.Time, state.Time);
                }
            }
        }

        public void Dispose()
        {
            _nodeCols.Clear();
        }
    }
}
