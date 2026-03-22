using System;
using UnityEngine;
using static ResXRData.CollectorUtils;
namespace ResXRData
{

    public class OVRPerformanceCollector : IContinuousCollector

    {
        public string CollectorName => "OVRPerformanceCollector";
        private bool _includePerf = false;
        private int _idxMotionPhotonLatency = -1; // AppMotionToPhotonLatency


        public void Collect(RowBuffer row, float timeSinceStartup)
        {
            if (!_includePerf) return;
            if (_idxMotionPhotonLatency < 0) return;

            float motionToPhoton = float.NaN;

            // collect AppMotionToPhotonLatency
            // Unsupported on OpenXR – this will just return an empty struct (and warn once).
            // Switch to Oculus XR plugin backend to get real data.
            var stats = OVRPlugin.GetAppPerfStats();

            if (stats.FrameStats != null && stats.FrameStatsCount > 0)
            {
                // Use the most recent frame in the buffer (max 5)
                int last = Mathf.Clamp(stats.FrameStatsCount - 1, 0, stats.FrameStats.Length - 1);
                motionToPhoton = stats.FrameStats[last].AppMotionToPhotonLatency; // seconds
            }

            row.Set(_idxMotionPhotonLatency, motionToPhoton);
        }

        public void Configure(ColumnIndex schema, RecordingOptions options)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (options == null) options = new RecordingOptions();

            _includePerf = options.includePerformance;
            if (!_includePerf) return;

            TryIndex(schema, "AppMotionToPhotonLatency", out _idxMotionPhotonLatency);




        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

    }
}