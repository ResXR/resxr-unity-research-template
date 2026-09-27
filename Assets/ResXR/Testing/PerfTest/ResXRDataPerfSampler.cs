// ResXRDataPerfSampler.cs
// Perf-test only. Samples ResXRDataManager's ProfilerMarkers plus frame time, GC pressure, and
// session file size at ~1 Hz and writes them to {sessionTime}_ResXRDataPerfSamples.csv in the
// session root. Add to a GameObject in Base Scene only for the duration of a perf-test
// campaign, alongside RecordingConfigOverride and SoakLoopController — see README.md.
//
// Deliberately does NOT log through CustomDataClass/LogCustom: that path re-runs
// Type.GetFields() plus a LINQ OrderBy on every single write, even for an already-initialized
// table (see CustomCsvFromDataClass.EnsureTableInitialized) — using it here would inject the
// measuring tool's own GC allocations into the exact GC counters this script is trying to
// attribute to ResXRDataManager. Instead this builds its own schema/writer directly from the
// same Core Infrastructure primitives ResXRDataManager itself uses (SchemaBuilder, RowBuffer,
// CsvRowWriter) — real reuse of the documented-as-reusable low-level pieces, without the
// reflection-based custom-table machinery that exists to serve a different job (BIDS metadata).

using System;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace ResXRData
{
    public class ResXRDataPerfSampler : MonoBehaviour
    {
        private const float SampleIntervalSeconds = 1f;
        private const int FrameTimeBufferCapacity = 512; // generous headroom above any plausible refresh rate

        private Recorder _fixedUpdateRecorder;
        private Recorder _writeContinuousRecorder;
        private Recorder _writeFaceRecorder;
        private Recorder _csvWriteRowRecorder;
        private Recorder _csvFlushRecorder;

        private long _fixedUpdateNsAccum, _writeContinuousNsAccum, _writeFaceNsAccum, _csvWriteRowNsAccum, _csvFlushNsAccum;

        private string[] _collectorNames = Array.Empty<string>();
        private Recorder[] _collectorRecorders = Array.Empty<Recorder>();
        private long[] _collectorNsAccum = Array.Empty<long>();

        private ProfilerRecorder _gcAllocRecorder;
        private long _gcAllocBytesAccum;
        private int _lastGen0Count;

        private readonly float[] _frameTimes = new float[FrameTimeBufferCapacity];
        private int _frameTimeCount;

        private float _windowStartTime;

        private string _continuousFilePath;
        private string _faceFilePath;

        private ColumnIndex _schema;
        private RowBuffer _row;
        private CsvRowWriter _writer;

        private void Start()
        {
            ResXRDataManager dataManager = ResXRDataManager.Instance;
            if (dataManager == null)
            {
                Debug.LogError("[ResXRDataPerfSampler] No ResXRDataManager found in scene — sampling disabled.");
                enabled = false;
                return;
            }

            _collectorNames = dataManager.ActiveCollectorNames ?? Array.Empty<string>();

            // Recorder/ProfilerRecorder only actually capture anything while the underlying
            // profiler capture system is on — normally driven by the Profiler window's Record
            // toggle. Outside that (which is the whole point here: no profiler attached, on-device
            // or in Play Mode), Profiler.enabled must be set explicitly, or every Recorder reads a
            // permanent 0 regardless of its own .enabled flag. Individually-named recorders still
            // need their own .enabled = true on top of this — see BuildRecorders().
            Profiler.enabled = true;

            BuildSchemaAndWriter(dataManager);
            BuildRecorders();

            _continuousFilePath = Path.Combine(dataManager.GetOutputDirectory(), $"{dataManager.SessionTime}_ContinuousData.csv");
            _faceFilePath = Path.Combine(dataManager.GetOutputDirectory(), $"{dataManager.SessionTime}_FaceExpressionData.csv");

            _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _lastGen0Count = GC.CollectionCount(0);
            _windowStartTime = Time.realtimeSinceStartup;

            Debug.Log($"[ResXRDataPerfSampler] Sampling started, tracking {_collectorNames.Length} collector(s).");
        }

        private void OnDestroy()
        {
            if (_gcAllocRecorder.Valid) _gcAllocRecorder.Dispose();
            try { _writer?.Dispose(); } catch { /* best-effort on shutdown */ }
        }

        private void BuildSchemaAndWriter(ResXRDataManager dataManager)
        {
            var builder = new SchemaBuilder()
                .Add("timeSinceStartup")
                .Add("FrameTime_Avg").Add("FrameTime_P95").Add("FrameTime_Max")
                .Add("FixedUpdate_ns")
                .Add("WriteContinuous_ns")
                .Add("WriteFace_ns")
                .Add("CsvWriteRow_ns")
                .Add("CsvFlush_ns");

            for (int i = 0; i < _collectorNames.Length; i++)
                builder.Add($"Collect_{_collectorNames[i]}_ns");

            builder.Add("GCAllocBytesInWindow")
                   .Add("GCGen0CollectionsInWindow")
                   .Add("MonoHeapUsedBytes")
                   .Add("ContinuousFileBytes")
                   .Add("FaceFileBytes");

            _schema = builder.Build();
            _row = new RowBuffer(_schema);

            string path = Path.Combine(dataManager.GetOutputDirectory(), $"{dataManager.SessionTime}_ResXRDataPerfSamples.csv");
            _writer = new CsvRowWriter(path, dataManager.csvDelimiter);
        }

        private void BuildRecorders()
        {
            _fixedUpdateRecorder = GetEnabledRecorder("ResXR.DataManager.FixedUpdate");
            _writeContinuousRecorder = GetEnabledRecorder("ResXR.DataManager.WriteContinuous");
            _writeFaceRecorder = GetEnabledRecorder("ResXR.DataManager.WriteFace");
            _csvWriteRowRecorder = GetEnabledRecorder("ResXR.CsvRowWriter.WriteRow");
            _csvFlushRecorder = GetEnabledRecorder("ResXR.CsvRowWriter.Flush");

            _collectorRecorders = new Recorder[_collectorNames.Length];
            _collectorNsAccum = new long[_collectorNames.Length];
            for (int i = 0; i < _collectorNames.Length; i++)
                _collectorRecorders[i] = GetEnabledRecorder($"ResXR.Collect.{_collectorNames[i]}");
        }

        private static Recorder GetEnabledRecorder(string markerName)
        {
            Recorder recorder = Recorder.Get(markerName);
            recorder.enabled = true;
            return recorder;
        }

        private void Update()
        {
            // Recorder.elapsedNanoseconds (and the GC allocation counter below) reflect only the
            // current/last rendered frame — they reset every frame. FixedUpdate can fire more
            // than once per rendered frame (100Hz physics vs ~72-90Hz Quest Pro refresh), so this
            // must accumulate every single Update(), not just once per ~1Hz sample window, or it
            // silently under-counts.
            _fixedUpdateNsAccum += _fixedUpdateRecorder.elapsedNanoseconds;
            _writeContinuousNsAccum += _writeContinuousRecorder.elapsedNanoseconds;
            _writeFaceNsAccum += _writeFaceRecorder.elapsedNanoseconds;
            _csvWriteRowNsAccum += _csvWriteRowRecorder.elapsedNanoseconds;
            _csvFlushNsAccum += _csvFlushRecorder.elapsedNanoseconds;

            for (int i = 0; i < _collectorRecorders.Length; i++)
                _collectorNsAccum[i] += _collectorRecorders[i].elapsedNanoseconds;

            if (_gcAllocRecorder.Valid)
                _gcAllocBytesAccum += _gcAllocRecorder.LastValue;

            if (_frameTimeCount < _frameTimes.Length)
                _frameTimes[_frameTimeCount++] = Time.unscaledDeltaTime;

            if (Time.realtimeSinceStartup - _windowStartTime >= SampleIntervalSeconds)
                FlushWindow();
        }

        private void FlushWindow()
        {
            float t = Time.realtimeSinceStartup;

            float avg = 0f, p95 = 0f, max = 0f;
            if (_frameTimeCount > 0)
            {
                float sum = 0f;
                for (int i = 0; i < _frameTimeCount; i++) sum += _frameTimes[i];
                avg = sum / _frameTimeCount;

                Array.Sort(_frameTimes, 0, _frameTimeCount); // in place, zero allocation
                max = _frameTimes[_frameTimeCount - 1];
                int p95Index = Mathf.Clamp((int)(0.95f * (_frameTimeCount - 1)), 0, _frameTimeCount - 1);
                p95 = _frameTimes[p95Index];
            }

            int gen0Now = GC.CollectionCount(0);
            int gen0Delta = gen0Now - _lastGen0Count;
            _lastGen0Count = gen0Now;

            _row.Clear();
            _row.Set("timeSinceStartup", t);
            _row.Set("FrameTime_Avg", avg);
            _row.Set("FrameTime_P95", p95);
            _row.Set("FrameTime_Max", max);
            _row.Set("FixedUpdate_ns", _fixedUpdateNsAccum);
            _row.Set("WriteContinuous_ns", _writeContinuousNsAccum);
            _row.Set("WriteFace_ns", _writeFaceNsAccum);
            _row.Set("CsvWriteRow_ns", _csvWriteRowNsAccum);
            _row.Set("CsvFlush_ns", _csvFlushNsAccum);

            for (int i = 0; i < _collectorNames.Length; i++)
                _row.Set($"Collect_{_collectorNames[i]}_ns", _collectorNsAccum[i]);

            _row.Set("GCAllocBytesInWindow", _gcAllocBytesAccum);
            _row.Set("GCGen0CollectionsInWindow", gen0Delta);
            _row.Set("MonoHeapUsedBytes", Profiler.GetMonoUsedSizeLong());
            _row.Set("ContinuousFileBytes", SafeFileLength(_continuousFilePath));
            _row.Set("FaceFileBytes", SafeFileLength(_faceFilePath));

            _writer.WriteRow(_schema, _row.ValuesArray, _row.ColumnIsSetMask);

            // Reset window accumulators.
            _fixedUpdateNsAccum = _writeContinuousNsAccum = _writeFaceNsAccum = _csvWriteRowNsAccum = _csvFlushNsAccum = 0;
            for (int i = 0; i < _collectorNsAccum.Length; i++) _collectorNsAccum[i] = 0;
            _gcAllocBytesAccum = 0;
            _frameTimeCount = 0;
            _windowStartTime = t;
        }

        private static long SafeFileLength(string path)
        {
            try
            {
                return File.Exists(path) ? new FileInfo(path).Length : 0L;
            }
            catch
            {
                return 0L;
            }
        }
    }
}
