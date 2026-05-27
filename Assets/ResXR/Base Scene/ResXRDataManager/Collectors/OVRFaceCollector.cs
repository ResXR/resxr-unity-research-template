// OVRFaceCollector.cs
// Collects face expression weights (by OVRPlugin.FaceExpression2 name) and region confidences.
// Targets the FaceExpressions.csv schema built by SchemaFactories.BuildFaceExpressionsV2().

using System;
using System.Collections.Generic;
using static OVRPlugin;   // FaceState, FaceExpression2, Step
using static ResXRData.CollectorUtils;

namespace ResXRData
{
    public sealed class OVRFaceCollector : IContinuousCollector
    {
        public string CollectorName => "OVRFaceCollector";
        private const Step SampleStep = OvrSampling.StepDefault;
        private const int LatestFrame = OvrSampling.LatestFrame;

        // Column indices
        private int _idxTimeSinceStartup = -1;  // "timeSinceStartup"
        private int _idxFaceTime = -1;          // "Face_Time" (double)
        private int _idxFaceStatus = -1;        // "Face_Status" (bool: IsValid)

        // Expression weight columns in SDK enum order.
        // Prefer named columns (e.g., "Brow_Lowerer_L"). Fallback to Face_Expression_00.. if needed.
        private readonly List<int> _exprCols = new List<int>();
        private bool _usedNamedExpressions = true;

        // Region confidences (two values: Upper / Lower)
        private int _idxRegionUpper = -1;
        private int _idxRegionLower = -1;

        private float[] faceWeightsOldAPi = new float[(int)OVRFaceExpressions.FaceExpression.Max];

        public void Configure(ColumnIndex schema, RecordingOptions options)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            // Base
            TryIndex(schema, "timeSinceStartup", out _idxTimeSinceStartup);
            TryIndex(schema, "Face_Time", out _idxFaceTime);
            TryIndex(schema, "Face_Status", out _idxFaceStatus);

            // Expressions: try by names from the enum (excluding Invalid/Max)
            _exprCols.Clear();
            string[] exprNames = Enum.GetNames(typeof(FaceExpression2));
            for (int i = 0; i < exprNames.Length; i++)
            {
                string name = exprNames[i];
                if (name == "Invalid" || name == "Max") continue;

                if (schema.TryGetIndex(name, out int idx))
                {
                    _exprCols.Add(idx);
                }
                else
                {
                    _usedNamedExpressions = false;
                    break;
                }
            }

            // Fallback to numeric headers if names weren’t used in the schema
            if (!_usedNamedExpressions)
            {
                _exprCols.Clear();

                int realCount = 0;
                for (int i = 0; i < exprNames.Length; i++)
                {
                    if (exprNames[i] == "Invalid" || exprNames[i] == "Max") continue;
                    realCount++;
                }

                for (int i = 0; i < realCount; i++)
                {
                    string col = $"Face_Expression_{i.ToString("D2")}";
                    _exprCols.Add(IndexOrMinusOne(schema, col));
                }
            }

            // Region confidences (canonical names; also accept older alias)
            _idxRegionUpper = IndexFirstFound(schema, "FaceRegionConfidence_Upper", "RegionConf_Upper");
            _idxRegionLower = IndexFirstFound(schema, "FaceRegionConfidence_Lower", "RegionConf_Lower");
        }

        public void Collect(RowBuffer row, float timeSinceStartup)
        {
            FaceState face = default;

            // Plugin form: GetFaceState2(step, frameIndex, ref FaceState)
            bool ok = GetFaceState2(SampleStep, LatestFrame, ref face);
            if (!ok) return;

            // Base
            SetIfValid(row, _idxFaceTime, face.Time);                 // double
            SetIfValid(row, _idxFaceStatus, face.Status.IsValid);     // bool

            // Expressions (clamped to available columns and weights length)
            int count = Math.Min(_exprCols.Count, face.ExpressionWeights != null ? face.ExpressionWeights.Length : 0);
            for (int i = 0; i < count; i++)
            {
                int colIndex = _exprCols[i];
                if (colIndex >= 0) row.Set(colIndex, face.ExpressionWeights[i]);
            }

            // Region confidences (two values: Upper = [0], Lower = [1])
            if (face.ExpressionWeightConfidences != null && face.ExpressionWeightConfidences.Length >= 2)
            {
                SetIfValid(row, _idxRegionLower , face.ExpressionWeightConfidences[0]);
                SetIfValid(row,  _idxRegionUpper, face.ExpressionWeightConfidences[1]);
            }
        }

        public void Dispose() { }



    }
}
