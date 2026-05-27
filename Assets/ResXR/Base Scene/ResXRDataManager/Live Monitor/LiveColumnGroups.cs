// LiveColumnGroups.cs
// Groups schema columns into logical domains (Nodes, Eyes, Hands, Body, Face, etc.)
// and defines summary vs. full column lists.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResXRData
{
    public enum LiveStreamKind
    {
        Continuous,
        Face
    }

    public enum LiveColumnGroupKind
    {
        Nodes,
        Eyes,
        Hands,
        Body,
        Performance,
        Gaze,
        SystemStatus,  // RENAMED from Recenter
        Custom,
        FaceBase,
        FaceExpressions,
        Other
    }

    public enum LiveColumnSubGroupKind
    {
        All,
        FlagsOnly,
        ValuesOnly,
        TimeOnly
    }


    public sealed class LiveColumnGroup
    {
        public LiveColumnGroupKind Kind;
        public string DisplayName;
        public List<int> AllColumns = new List<int>();
        public List<int> Summary = new List<int>();
    }

    public static class LiveColumnGroups
    {
        public static List<LiveColumnGroup> BuildGroups(ColumnIndex schema, LiveStreamKind streamKind)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            return streamKind == LiveStreamKind.Continuous
                ? BuildContinuousGroups(schema)
                : BuildFaceGroups(schema);
        }

        #region ContinuousData grouping

        private static List<LiveColumnGroup> BuildContinuousGroups(ColumnIndex schema)
        {
            // Initialize groups
            var byKind = new Dictionary<LiveColumnGroupKind, LiveColumnGroup>();

            LiveColumnGroup Get(LiveColumnGroupKind kind, string displayName)
            {
                if (!byKind.TryGetValue(kind, out var g))
                {
                    g = new LiveColumnGroup
                    {
                        Kind = kind,
                        DisplayName = displayName
                    };
                    byKind[kind] = g;
                }
                return g;
            }

            int count = schema.Count;
            for (int i = 0; i < count; i++)
            {
                string name = schema[i];
                LiveColumnGroupKind kind = ClassifyContinuousColumn(name);
                var group = Get(kind, DefaultDisplayName(kind));
                group.AllColumns.Add(i);
            }

            // Summary columns: pick a few high-signal ones by name if they exist
            BuildContinuousSummaries(schema, byKind);

            // Return in a nice logical order
            var ordered = new List<LiveColumnGroup>();

            void AddIfExists(LiveColumnGroupKind kind)
            {
                if (byKind.TryGetValue(kind, out var g) && g.AllColumns.Count > 0)
                    ordered.Add(g);
            }

            AddIfExists(LiveColumnGroupKind.Nodes);
            AddIfExists(LiveColumnGroupKind.Eyes);
            AddIfExists(LiveColumnGroupKind.Hands);
            AddIfExists(LiveColumnGroupKind.Body);
            AddIfExists(LiveColumnGroupKind.Performance);
            AddIfExists(LiveColumnGroupKind.Gaze);
            AddIfExists(LiveColumnGroupKind.SystemStatus);
            AddIfExists(LiveColumnGroupKind.Custom);
            AddIfExists(LiveColumnGroupKind.Other);


            //DEBUG: print group sizes once
            foreach (var g in ordered)
            {
                Debug.Log($"[LiveColumnGroups] Group {g.Kind} has {g.AllColumns.Count} columns.");
            }

            return ordered;
        }

        private static LiveColumnGroupKind ClassifyContinuousColumn(string name)
        {
            if (string.IsNullOrEmpty(name))
                return LiveColumnGroupKind.Other;

            // Nodes / head
            if (name.StartsWith("Head_", StringComparison.Ordinal) ||
                name.StartsWith("Node_", StringComparison.Ordinal))
                return LiveColumnGroupKind.Nodes;

            // Eyes (dedicated API only: quat, valid, confidence, time)
            if (name.StartsWith("LeftEye_", StringComparison.Ordinal) ||
                name.StartsWith("RightEye_", StringComparison.Ordinal) ||
                name.StartsWith("Eyes_", StringComparison.Ordinal))
                return LiveColumnGroupKind.Eyes;

            // Gaze (combined + left/right eye hit points and focused objects)
            if (name == "FocusedObject" ||
                name.StartsWith("EyeGazeHitPosition_", StringComparison.Ordinal) ||
                name.StartsWith("LeftEyeGazeHitPosition_", StringComparison.Ordinal) ||
                name.StartsWith("RightEyeGazeHitPosition_", StringComparison.Ordinal) ||
                name == "LeftFocusedObject" || name == "RightFocusedObject" ||
                name == "HasLeftEyeHit" || name == "HasRightEyeHit")
                return LiveColumnGroupKind.Gaze;

            // Hands (root + bones)
            if (name.Contains("Hand_", StringComparison.Ordinal))
                return LiveColumnGroupKind.Hands;

            if (name.StartsWith("Left_", StringComparison.Ordinal) ||
                name.StartsWith("Right_", StringComparison.Ordinal))
            {
                // Bone names from SchemaFactories typically start as "Left_<BoneName>_x"
                if (name.IndexOf("Thumb", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("Index", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("Middle", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("Ring", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("Pinky", StringComparison.Ordinal) >= 0 ||
                    name.Contains("_Hand", StringComparison.Ordinal))
                {
                    return LiveColumnGroupKind.Hands;
                }
            }

            // Body
            if (name.StartsWith("Body_", StringComparison.Ordinal))
                return LiveColumnGroupKind.Body;

            // Performance
            if (name == "AppMotionToPhotonLatency")
                return LiveColumnGroupKind.Performance;

            // System status (consolidated - renamed from Recenter)
            if (name == "shouldRecenter" || name == "recenterEvent" ||
                name == "RecenterCount" ||
                name.StartsWith("TrackingOriginChange_", StringComparison.Ordinal) ||
                name.StartsWith("TrackingTransform_", StringComparison.Ordinal) ||
                name == "UserPresent" || name == "TrackingLost")
                return LiveColumnGroupKind.SystemStatus;  // RENAMED from Recenter

            // Custom transforms
            if (name.StartsWith("Custom_", StringComparison.Ordinal))
                return LiveColumnGroupKind.Custom;

            return LiveColumnGroupKind.Other;
        }

        private static void BuildContinuousSummaries(ColumnIndex schema, Dictionary<LiveColumnGroupKind, LiveColumnGroup> byKind)
        {
            // helper
            void AddIfExists(LiveColumnGroupKind kind, string columnName)
            {
                if (!byKind.TryGetValue(kind, out var g)) return;
                if (schema.TryGetIndex(columnName, out int idx) && g.AllColumns.Contains(idx))
                {
                    if (!g.Summary.Contains(idx))
                        g.Summary.Add(idx);
                }
            }

            // Eyes summary (dedicated API only)
            AddIfExists(LiveColumnGroupKind.Eyes, "LeftEye_qx");
            AddIfExists(LiveColumnGroupKind.Eyes, "LeftEye_qy");
            AddIfExists(LiveColumnGroupKind.Eyes, "LeftEye_qz");
            AddIfExists(LiveColumnGroupKind.Eyes, "LeftEye_qw");
            AddIfExists(LiveColumnGroupKind.Eyes, "RightEye_qx");
            AddIfExists(LiveColumnGroupKind.Eyes, "RightEye_qy");
            AddIfExists(LiveColumnGroupKind.Eyes, "RightEye_qz");
            AddIfExists(LiveColumnGroupKind.Eyes, "RightEye_qw");
            AddIfExists(LiveColumnGroupKind.Eyes, "Eyes_Time");
            AddIfExists(LiveColumnGroupKind.Eyes, "LeftEye_IsValid");
            AddIfExists(LiveColumnGroupKind.Eyes, "RightEye_IsValid");

            // Gaze summary (combined + left/right eye hit points and focused objects)
            AddIfExists(LiveColumnGroupKind.Gaze, "FocusedObject");
            AddIfExists(LiveColumnGroupKind.Gaze, "EyeGazeHitPosition_X");
            AddIfExists(LiveColumnGroupKind.Gaze, "EyeGazeHitPosition_Y");
            AddIfExists(LiveColumnGroupKind.Gaze, "EyeGazeHitPosition_Z");
            AddIfExists(LiveColumnGroupKind.Gaze, "LeftEyeGazeHitPosition_X");
            AddIfExists(LiveColumnGroupKind.Gaze, "LeftEyeGazeHitPosition_Y");
            AddIfExists(LiveColumnGroupKind.Gaze, "LeftEyeGazeHitPosition_Z");
            AddIfExists(LiveColumnGroupKind.Gaze, "RightEyeGazeHitPosition_X");
            AddIfExists(LiveColumnGroupKind.Gaze, "RightEyeGazeHitPosition_Y");
            AddIfExists(LiveColumnGroupKind.Gaze, "RightEyeGazeHitPosition_Z");
            AddIfExists(LiveColumnGroupKind.Gaze, "LeftFocusedObject");
            AddIfExists(LiveColumnGroupKind.Gaze, "RightFocusedObject");
            AddIfExists(LiveColumnGroupKind.Gaze, "HasLeftEyeHit");
            AddIfExists(LiveColumnGroupKind.Gaze, "HasRightEyeHit");

            // Hands summary (one or two key fields)
            AddIfExists(LiveColumnGroupKind.Hands, "LeftHand_Status");
            AddIfExists(LiveColumnGroupKind.Hands, "LeftHand_Root_px");
            AddIfExists(LiveColumnGroupKind.Hands, "LeftHand_Root_py");
            AddIfExists(LiveColumnGroupKind.Hands, "LeftHand_Root_pz");
            AddIfExists(LiveColumnGroupKind.Hands, "LeftHand_HandConfidence");
            AddIfExists(LiveColumnGroupKind.Hands, "RightHand_Status");
            AddIfExists(LiveColumnGroupKind.Hands, "RightHand_Root_px");
            AddIfExists(LiveColumnGroupKind.Hands, "RightHand_Root_py");
            AddIfExists(LiveColumnGroupKind.Hands, "RightHand_Root_pz");
            AddIfExists(LiveColumnGroupKind.Hands, "RightHand_HandConfidence");

            // Body summary
            AddIfExists(LiveColumnGroupKind.Body, "Body_Time");
            AddIfExists(LiveColumnGroupKind.Body, "Body_Confidence");
            AddIfExists(LiveColumnGroupKind.Body, "Body_Fidelity");
            AddIfExists(LiveColumnGroupKind.Body, "Body_CalibrationStatus");
            AddIfExists(LiveColumnGroupKind.Body, "Body_SkeletonChangedCount");

            // Performance summary (AppMotionToPhotonLatency removed)

            // System status summary (group renamed from Recenter)
            AddIfExists(LiveColumnGroupKind.SystemStatus, "shouldRecenter");
            AddIfExists(LiveColumnGroupKind.SystemStatus, "recenterEvent");
            AddIfExists(LiveColumnGroupKind.SystemStatus, "RecenterCount"); // NEW
            AddIfExists(LiveColumnGroupKind.SystemStatus, "TrackingOriginChange_Event"); // NEW
            AddIfExists(LiveColumnGroupKind.SystemStatus, "UserPresent"); // NEW
            AddIfExists(LiveColumnGroupKind.SystemStatus, "TrackingLost"); // NEW

            // Custom summary: first few Custom_ columns (if any)
            if (byKind.TryGetValue(LiveColumnGroupKind.Custom, out var custom) && custom.Summary.Count == 0)
            {
                const int maxCustomSummary = 8;
                int added = 0;
                for (int i = 0; i < custom.AllColumns.Count && added < maxCustomSummary; i++)
                {
                    int idx = custom.AllColumns[i];
                    string name = schema[idx];
                    if (name != null && name.StartsWith("Custom_", StringComparison.Ordinal))
                    {
                        custom.Summary.Add(idx);
                        added++;
                    }
                }
            }

            // If summary is empty for any group, fall back to first few cols
            foreach (var kvp in byKind)
            {
                LiveColumnGroup g = kvp.Value;
                if (g.Summary.Count == 0)
                {
                    int take = Math.Min(8, g.AllColumns.Count);
                    for (int i = 0; i < take; i++)
                        g.Summary.Add(g.AllColumns[i]);
                }
            }
        }

        #endregion

        #region FaceExpressions grouping

        private static List<LiveColumnGroup> BuildFaceGroups(ColumnIndex schema)
        {
            var baseGroup = new LiveColumnGroup
            {
                Kind = LiveColumnGroupKind.FaceBase,
                DisplayName = "Face Base"
            };

            var exprGroup = new LiveColumnGroup
            {
                Kind = LiveColumnGroupKind.FaceExpressions,
                DisplayName = "Face Expressions"
            };

            int count = schema.Count;
            for (int i = 0; i < count; i++)
            {
                string name = schema[i];
                if (IsFaceBaseColumn(name))
                    baseGroup.AllColumns.Add(i);
                else
                    exprGroup.AllColumns.Add(i);
            }

            // Base summary: all base columns
            baseGroup.Summary.AddRange(baseGroup.AllColumns);

            // Expressions summary: first few expressions
            int maxExprSummary = Math.Min(12, exprGroup.AllColumns.Count);
            for (int i = 0; i < maxExprSummary; i++)
                exprGroup.Summary.Add(exprGroup.AllColumns[i]);

            var result = new List<LiveColumnGroup>();
            if (baseGroup.AllColumns.Count > 0) result.Add(baseGroup);
            if (exprGroup.AllColumns.Count > 0) result.Add(exprGroup);



            return result;
        }

        private static bool IsFaceBaseColumn(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            if (name == "timeSinceStartup") return true;
            if (name == "Face_Time") return true;
            if (name == "Face_Status") return true;
            if (name == "FaceRegionConfidence_Upper" ||
                name == "FaceRegionConfidence_Lower" ||
                name == "RegionConf_Upper" ||
                name == "RegionConf_Lower")
                return true;

            // anything else is considered an expression
            return false;
        }

        #endregion

        private static string DefaultDisplayName(LiveColumnGroupKind kind)
        {
            switch (kind)
            {
                case LiveColumnGroupKind.Nodes: return "Nodes / Head";
                case LiveColumnGroupKind.Eyes: return "Eyes (Dedicated)";
                case LiveColumnGroupKind.Hands: return "Hands";
                case LiveColumnGroupKind.Body: return "Body";
                case LiveColumnGroupKind.Performance: return "Performance";
                case LiveColumnGroupKind.Gaze: return "Gaze";
                case LiveColumnGroupKind.SystemStatus: return "System Status";  // RENAMED from Recenter
                case LiveColumnGroupKind.Custom: return "Custom Transforms";
                case LiveColumnGroupKind.FaceBase: return "Face Base";
                case LiveColumnGroupKind.FaceExpressions: return "Face Expressions";
                default: return "Other";
            }
        }
    }
}
