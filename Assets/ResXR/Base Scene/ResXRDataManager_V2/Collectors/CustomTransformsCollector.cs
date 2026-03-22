// CustomTransformsCollector.cs
// Writes positions and rotations for experiment-specific scene objects selected in the inspector.
// Columns are: Custom_<TransformName>_px/_py/_pz and Custom_<TransformName>_qx/_qy/_qz/_qw

using System;
using System.Collections.Generic;
using UnityEngine;
using static ResXRData.CollectorUtils;

namespace ResXRData
{
    public sealed class CustomTransformsCollector : IContinuousCollector
    {
        public string CollectorName => "CustomTransformsCollector";

        private struct TransformCols
        {
            public string Name;
            public Transform Transform;

            public int Px, Py, Pz;
            public int Qx, Qy, Qz, Qw;
        }

        private readonly List<TransformCols> _targets = new List<TransformCols>();
        private bool _enabled = false;

        public void Configure(ColumnIndex schema, RecordingOptions options)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (options == null) options = new RecordingOptions();

            _enabled = options.customTransformsToRecord != null && options.customTransformsToRecord.Count > 0;
            _targets.Clear();

            if (!_enabled) return;

            foreach (Transform t in options.customTransformsToRecord)
            {
                if (t == null) continue;

                string prefix = $"Custom_{t.name}";
                TransformCols cols = new TransformCols
                {
                    Name = t.name,
                    Transform = t,

                    Px = IndexOrMinusOne(schema, $"{prefix}_px"),
                    Py = IndexOrMinusOne(schema, $"{prefix}_py"),
                    Pz = IndexOrMinusOne(schema, $"{prefix}_pz"),

                    Qx = IndexOrMinusOne(schema, $"{prefix}_qx"),
                    Qy = IndexOrMinusOne(schema, $"{prefix}_qy"),
                    Qz = IndexOrMinusOne(schema, $"{prefix}_qz"),
                    Qw = IndexOrMinusOne(schema, $"{prefix}_qw"),
                };

                _targets.Add(cols);
            }
        }

        public void Collect(RowBuffer row, float timeSinceStartup)
        {
            if (!_enabled) return;

            for (int i = 0; i < _targets.Count; i++)
            {
                TransformCols tc = _targets[i];
                if (tc.Transform == null) continue;

                Vector3 p = tc.Transform.position;
                Quaternion q = tc.Transform.rotation;

                SetIfValid(row, tc.Px, p.x);
                SetIfValid(row, tc.Py, p.y);
                SetIfValid(row, tc.Pz, p.z);

                SetIfValid(row, tc.Qx, q.x);
                SetIfValid(row, tc.Qy, q.y);
                SetIfValid(row, tc.Qz, q.z);
                SetIfValid(row, tc.Qw, q.w);
            }
        }

        public void Dispose()
        {
            _targets.Clear();
        }

    }
}
