// LiveTapService.cs
// ResXRSingleton that listens to ResXRDataManager_V2 events and caches the latest rows.

using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;

namespace ResXRData
{
    /// <summary>
    /// Lightweight view of a CSV row at a specific sample time.
    /// Holds references to the RowBuffer's values & mask (read-only for callers).
    /// </summary>
    public struct LiveRow
    {
        public float sampleTime;
        public ColumnIndex schema;
        public object[] values;
        public BitArray columnIsSetMask;

        public bool IsValid =>
            schema != null &&
            values != null &&
            columnIsSetMask != null;
    }

    public sealed class LiveMonitorService : ResXRSingleton<LiveMonitorService>
    {
        private LiveRow _latestContinuous;
        private LiveRow _latestFace;
        private bool _haveContinuous;
        private bool _haveFace;

        protected override void DoInAwake()
        {

        }

        private async UniTaskVoid Start()
        {
            // Wait until DataManager is available
            Debug.Log("[LiveMonitorService] Waiting for DataManager to initialize.");
            // Wait one frame to ensure ResXRDataManager_V2.Instance is created
            await UniTask.Yield(PlayerLoopTiming.Update);

            Debug.Log("[LiveMonitorService] DataManager is available, subscribing to events.");
            ResXRDataManager_V2 dm = ResXRDataManager_V2.Instance;
            dm.OnContinuousSample += OnContinuousSample;
            dm.OnFaceExpressionSample += OnFaceExpressionSample;
            Debug.Log("[LiveMonitorService] Initialized, Subscribed to ResXRDataManager_V2 events.");
        }

        private void OnDestroy()
        {
            var dm = ResXRDataManager_V2.Instance;
            if (dm != null)
            {
                dm.OnContinuousSample -= OnContinuousSample;
                dm.OnFaceExpressionSample -= OnFaceExpressionSample;
            }
        }

        // Called by DataManager via events
        private void OnContinuousSample(ContinuousSample sample)
        {
            _latestContinuous = new LiveRow
            {
                sampleTime = sample.sampleTime,
                schema = sample.schema,
                values = sample.values,
                columnIsSetMask = sample.columnIsSetMask
            };
            _haveContinuous = true;
        }

        private void OnFaceExpressionSample(FaceExpressionSample sample)
        {
            _latestFace = new LiveRow
            {
                sampleTime = sample.sampleTime,
                schema = sample.schema,
                values = sample.values,
                columnIsSetMask = sample.columnIsSetMask
            };
            _haveFace = true;
        }

        /// <summary>
        /// Get the latest ContinuousData row, if any.
        /// </summary>
        public bool TryGetLatestContinuous(out LiveRow row)
        {
            if (_haveContinuous && _latestContinuous.IsValid)
            {
                row = _latestContinuous;
                return true;
            }

            row = default;
            return false;
        }

        /// <summary>
        /// Get the latest FaceExpressions row, if any.
        /// </summary>
        public bool TryGetLatestFace(out LiveRow row)
        {
            if (_haveFace && _latestFace.IsValid)
            {
                row = _latestFace;
                return true;
            }

            row = default;
            return false;
        }

        public float GetContinuousAgeSeconds()
        {
            if (!_haveContinuous || !_latestContinuous.IsValid)
                return float.PositiveInfinity;

            return Time.realtimeSinceStartup - _latestContinuous.sampleTime;
        }

        public float GetFaceAgeSeconds()
        {
            if (!_haveFace || !_latestFace.IsValid)
                return float.PositiveInfinity;

            return Time.realtimeSinceStartup - _latestFace.sampleTime;
        }
    }
}
