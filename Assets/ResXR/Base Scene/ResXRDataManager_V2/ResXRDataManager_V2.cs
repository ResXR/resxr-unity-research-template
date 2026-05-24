// ResXRDataManager_V2.cs
// Builds schemas, opens CSVs, runs collectors every FixedUpdate.
// Researchers use: LogCustom(...) and the inspector list of custom transforms.

using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static OVRPlugin;
using static ResXRData.BuildInfoLoader;


namespace ResXRData
{
    #region custom data classes defenitions

    public class ResXRLogs : CustomDataClass
    {
        public string TableName => "ResXRLogs";

        public float timeSinceStartup;
        public string message;
        public ResXRLogs(float t, string msg)
        {
            timeSinceStartup = t;
            message = msg;
        }
    }

    public class TrialsData : CustomDataClass
    {
        public string TableName => "TrialsData";
        public string Session; // can be changed to int if needed
        public string Task; // can be changed to int if needed
        public string Trial; // can be changed to int if needed
        public string TrialName; // unique name for the trial
        public float StartTime; // timeSinceStartup at trial start
        public float EndTime; // timeSinceStartup at trial end

        public TrialsData(string session, string task, string trial, string trialName, float startTime, float endTime)
        {
            Session = session;
            Task = task;
            Trial = trial;
            TrialName = trialName;
            StartTime = startTime;
            EndTime = endTime;
        }

    }

    public class ChoiceEvent : CustomDataClass
    {
        public string TableName => "ChoiceEvents";
        public float TimeSinceStart;
        public string Task;
        public int Trial;
        public string OptionAName;
        public string OptionBName;
        public string Choice;
        public string ChosenOption;
        public string HandUsed;
        public float ReactionTime;
        public float displayTime;
        public float ChoiceTime;

        public ChoiceEvent(string task, int trial, string optionAName, string optionBName, string choice,
            string chosenOption, string handUsed, float reactionTime, float displayTime, float choiceTime)
        {
            this.TimeSinceStart = Time.realtimeSinceStartup;
            this.Task = task;
            this.Trial = trial;
            this.OptionAName = optionAName;
            this.OptionBName = optionBName;
            this.Choice = choice;
            this.ChosenOption = chosenOption;
            this.HandUsed = handUsed;
            this.ReactionTime = reactionTime;
            this.displayTime = displayTime;
            this.ChoiceTime = choiceTime;
        }
    }

    public class StimulusBounds : CustomDataClass
    {
        public string TableName => "StimulusBounds";
        public float TimeSinceStart;
        public string ChoiceId;
        public float RendererCenterX;
        public float RendererCenterY;
        public float RendererCenterZ;
        public float RendererSizeX;
        public float RendererSizeY;
        public float RendererSizeZ;
        public float ColliderCenterX;
        public float ColliderCenterY;
        public float ColliderCenterZ;
        public float ColliderSizeX;
        public float ColliderSizeY;
        public float ColliderSizeZ;

        public StimulusBounds(float timeSinceStart, string choiceId, float rendererCenterX, float rendererCenterY, float rendererCenterZ,
            float rendererSizeX, float rendererSizeY, float rendererSizeZ, float colliderCenterX, float colliderCenterY, float colliderCenterZ,
            float colliderSizeX, float colliderSizeY, float colliderSizeZ)
        {
            TimeSinceStart = timeSinceStart;
            ChoiceId = choiceId;
            RendererCenterX = rendererCenterX;
            RendererCenterY = rendererCenterY;
            RendererCenterZ = rendererCenterZ;
            RendererSizeX = rendererSizeX;
            RendererSizeY = rendererSizeY;
            RendererSizeZ = rendererSizeZ;
            ColliderCenterX = colliderCenterX;
            ColliderCenterY = colliderCenterY;
            ColliderCenterZ = colliderCenterZ;
            ColliderSizeX = colliderSizeX;
            ColliderSizeY = colliderSizeY;
            ColliderSizeZ = colliderSizeZ;
        }
    }

    /// <summary>
    /// One row in Events.csv — pipeline-friendly markers (name, onset, duration).
    /// Times are expected to use <see cref="Time.realtimeSinceStartup"/> for onset (seconds since app start), matching
    /// continuous CSV time columns; duration is in seconds (use 0 for point events). Same convention for downstream output standardization.
    /// </summary>
    public class ReportEvent : CustomDataClass
    {
        public string TableName => "Events";

        public string name;
        public float onset;
        public float duration;

        public ReportEvent(string name, float onset, float duration)
        {
            this.name = name;
            this.onset = onset;
            this.duration = duration;
        }
    }

    #endregion


    public sealed class ResXRDataManager_V2 : ResXRSingleton<ResXRDataManager_V2>
    {
        #region fields and properties

        [Header("Editor Export")]
        public bool exportInEditor = false;

        [ShowIf(nameof(exportInEditor))]
        public string saveFilePath;  // if null/empty, falls back to tmp

        [Header("Output")]
        private string sessionTime;
        public string SessionTime => sessionTime; // read-only property
        private bool appendIfFilesExist = false;
        public string csvDelimiter = ",";

        [Header("What to record (ContinuousData)")]
        public RecordingOptions recordingOptions = new RecordingOptions();

        [Header("FaceExpressions.csv")]
        public bool recordFaceExpressions = true;

        // paths
        private string _rootDir;

        // schemas
        private ColumnIndex _continuousSchema;
        private ColumnIndex _faceSchema;

        // row buffers
        private RowBuffer _continuousRow;
        private RowBuffer _faceRow;

        // writers
        private CsvRowWriter _continuousWriter;
        private CsvRowWriter _faceWriter;

        // collectors
        private readonly List<IContinuousCollector> _continuousCollectors = new List<IContinuousCollector>();
        private OVRFaceCollector _faceCollector;

        // Live Monitoring Events
        // Low-GC, read-only notification (UI pulls & copies what it needs)
        public event Action<ContinuousSample> OnContinuousSample; // Fired once per physics tick, right before writing ContinuousData csv
        public event Action<FaceExpressionSample> OnFaceExpressionSample; // Fired once per physics tick, right before writing FaceExpressions csv

        // declare pointers for all experience-specific analytics classes
        private ResXRLogs logLine;

        #endregion

        public void LogLineToFile(string line)
        {
            // creates a new instance of AnalyticsLogLine data class. In it's constructor, it gets the line and automatically assign Time.time to the log time.
            logLine = new ResXRLogs(Time.realtimeSinceStartup, line);
            // pass the data class instance to CustomCsvFromDataClass.Write to write it to file.
            this.LogCustom(logLine);
        }

        #region project specific analitics reportes

        public void LogChoice(string task, int trial, string optionAName, string optionBName, string choice,
            string chosenOption, string handUsed, float reactionTime, float displayTime, float choiceTime)
        {
            var choiceEvent = new ChoiceEvent(task, trial, optionAName, optionBName, choice, chosenOption, handUsed, reactionTime, displayTime, choiceTime);
            LogCustom(choiceEvent);
        }

        /// <summary>
        /// Writes one row to Events.csv. Pass <see cref="Time.realtimeSinceStartup"/> for onset unless you intentionally use another clock.
        /// </summary>
        public void ReportEvent(string name, float onset, float duration)
        {
            LogCustom(new ReportEvent(name, onset, duration));
        }

        #endregion



        #region unity lifecycle
        // Change the method declaration to override the base class implementation
        protected override void DoInAwake()
        {
            // 0) session time suffix
            sessionTime = DateTime.UtcNow.ToString("yyyy.MM.dd_HH-mm");

            // 1) Output directory
            if (Application.isEditor && exportInEditor)
            {
                if (!string.IsNullOrWhiteSpace(saveFilePath))
                {
                    _rootDir = saveFilePath.Replace('\\', '/'); // normalize slashes
                    Directory.CreateDirectory(_rootDir);
                }
                else
                {
                    _rootDir = Path.Combine(Path.GetTempPath(), "ResXR_EditorLogs");
                }
            }
            else
            {
                _rootDir = Application.persistentDataPath;
            }

            // 2) Build schemas (face first so we have faceExprCount for metadata)
            var face = SchemaFactories.BuildFaceExpressionsV2();                 // (schema, faceExprCount, ...)
            var cont = SchemaFactories.BuildContinuousDataV2(recordingOptions);   // (schema, counts, flags)
            _continuousSchema = cont.schema;
            _faceSchema = face.schema;

            // 4.5) Initialize tracking space converter for world space conversion
            OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig != null && cameraRig.trackingSpace != null)
            {
                TrackingSpaceConverter.Initialize(cameraRig.trackingSpace);
                Debug.Log("[ResXRDataManager_V2] TrackingSpaceConverter initialized for world space conversion");
            }
            else
            {
                Debug.LogError("[ResXRDataManager_V2] OVRCameraRig not found - world space conversion unavailable!");
            }

            // 5) Writers
            string contPath = Path.Combine(_rootDir, $"{sessionTime}_ContinuousData.csv");
            _continuousWriter = new CsvRowWriter(contPath, csvDelimiter, null, appendIfFilesExist);

            if (recordFaceExpressions)
            {
                string facePath = Path.Combine(_rootDir, $"{sessionTime}_FaceExpressionData.csv");
                _faceWriter = new CsvRowWriter(facePath, csvDelimiter, null, appendIfFilesExist);
            }

            // 6) Row buffers
            _continuousRow = new RowBuffer(_continuousSchema);
            _faceRow = recordFaceExpressions ? new RowBuffer(_faceSchema) : null;

            // 7) Initialize Collectors for ContinuousData
            if (recordingOptions.includeNodes) _continuousCollectors.Add(new OVRNodesCollector());
            if (recordingOptions.includeEyes || recordingOptions.includeGaze || recordingOptions.includeSeparateEyesGaze)
                _continuousCollectors.Add(new OVREyesCollector());
            if (recordingOptions.includeHands) _continuousCollectors.Add(new OVRHandsCollector());
            if (recordingOptions.includeBody) _continuousCollectors.Add(new OVRBodyCollector());
            if (recordingOptions.includeSystemStatus) _continuousCollectors.Add(new SystemStatusCollector());
            if (recordingOptions.includePerformance) _continuousCollectors.Add(new OVRPerformanceCollector());
            if (recordingOptions.customTransformsToRecord != null &&
                recordingOptions.customTransformsToRecord.Count > 0)
                _continuousCollectors.Add(new CustomTransformsCollector());

            foreach (IContinuousCollector c in _continuousCollectors)
                c.Configure(_continuousSchema, recordingOptions);

            if (recordFaceExpressions)
            {
                _faceCollector = new OVRFaceCollector();
                _faceCollector.Configure(_faceSchema, recordingOptions);
            }

            // 8) Custom data tables: set base directory + delimiter once
            CustomCsvFromDataClass.Initialize(_rootDir, csvDelimiter, sessionTime);

            // 9) Metadata (async so build info can load first; recording already set up)
            WriteMetadataAsync(face.faceExprCount).Forget();
        }

        private async void Start()
        {
            if (ResXRPlayer.Instance != null && ResXRPlayer.Instance.EyeTracker != null)
                ResXRPlayer.Instance.EyeTracker.EnableSeparateEyeRaycasts = recordingOptions.includeSeparateEyesGaze;

            if (recordingOptions.includeBody)
            {
                await StartBodyTrackingAsync(BodyJointSet.FullBody, BodyTrackingFidelity2.High);
            }
        }

        private void FixedUpdate()
        {
            // sample time
            float t = Time.realtimeSinceStartup;

            #region [Continuous Data] Collect + fire live monitor + write to file

            // collect
            _continuousRow.Clear();
            _continuousRow.TrySet("timeSinceStartup", t);
            for (int i = 0; i < _continuousCollectors.Count; i++)
            {
                _continuousCollectors[i].Collect(_continuousRow, t);
            }

            // live monitor tap (ContinuousData) � only if there are subscribers
            if (OnContinuousSample != null)
            {
                var sample = new ContinuousSample(
                    t,
                    _continuousSchema,
                    _continuousRow.ValuesArray,
                    _continuousRow.ColumnIsSetMask
                );
                OnContinuousSample(sample);
            }

            // write row
            _continuousWriter.WriteRow(_continuousSchema,
                                       _continuousRow.ValuesArray,
                                       _continuousRow.ColumnIsSetMask);

            #endregion

            #region [Face Expressions] Collect + fire live monitor + write to file

            // collect 
            if (recordFaceExpressions && _faceCollector != null)
            {
                _faceRow.Clear();
                _faceRow.TrySet("timeSinceStartup", t);
                _faceCollector.Collect(_faceRow, t);

                // live monitor tap (FaceExpressions) � only if there are subscribers
                if (OnFaceExpressionSample != null)
                {
                    var faceSample = new FaceExpressionSample(
                        t,
                        _faceSchema,
                        _faceRow.ValuesArray,
                        _faceRow.ColumnIsSetMask
                    );
                    OnFaceExpressionSample(faceSample);
                }

                _faceWriter.WriteRow(_faceSchema,
                                     _faceRow.ValuesArray,
                                     _faceRow.ColumnIsSetMask);
            }

            #endregion
        }

        private void OnDestroy()
        {
            // collectors
            for (int i = 0; i < _continuousCollectors.Count; i++)
            {
                try { _continuousCollectors[i].Dispose(); } catch { }
            }
            _continuousCollectors.Clear();

            // writers
            try { _continuousWriter?.Dispose(); } catch { }
            try { _faceWriter?.Dispose(); } catch { }

            // custom tables
            try { CustomCsvFromDataClass.CloseAll(); } catch { }
        }

        #endregion

        #region Tracking Initializing 
        public async UniTask StartBodyTrackingAsync(BodyJointSet jointSet = BodyJointSet.FullBody,
                                                   BodyTrackingFidelity2 fidelity = BodyTrackingFidelity2.High)
        {
            // Wait a couple of frames so OVR/Link fully initializes
            await UniTask.Yield(PlayerLoopTiming.Update);
            await UniTask.Yield(PlayerLoopTiming.Update);

            Debug.Log($"[DataManager_V2] Body Tracking Supported={OVRPlugin.bodyTrackingSupported}");

            // Request fidelity first, then try v2 start, then fallback to legacy start
            OVRPlugin.RequestBodyTrackingFidelity(fidelity);
            bool started = OVRPlugin.StartBodyTracking2(jointSet);
            if (!started)
                started = OVRPlugin.StartBodyTracking();

            // Wait up to ~2 seconds for the runtime to enable body tracking
            float timeout = 2f;
            float elapsed = 0f;

            while (elapsed < timeout && !OVRPlugin.bodyTrackingEnabled)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                elapsed += Time.unscaledDeltaTime;
            }

            Debug.Log($"[DataManager_V2] Body Tracking started={started}, Body Tracking enabled={OVRPlugin.bodyTrackingEnabled}, set={jointSet}, fid={fidelity}");
        }
        #endregion

        #region custom DataClass logging
        // ---------- minimal API for researchers ----------

        // Create & write a row to <TableName>.csv using a custom data class instance.
        public void LogCustom(CustomDataClass data)
        {
            if (data == null) return;
            CustomCsvFromDataClass.Write(data);
        }

        // Overload that builds the object on demand (avoids allocations at call site).
        public void LogCustom(Func<CustomDataClass> make)
        {
            if (make == null) return;
            var inst = make();
            if (inst == null) return;
            CustomCsvFromDataClass.Write(inst);
        }

        #endregion

        public string GetOutputDirectory() => _rootDir;

        #region default data class logging



        #endregion

        #region METADATA

        private async UniTaskVoid WriteMetadataAsync(int faceExprCount)
        {
            if (BuildInfoLoader.Instance != null)
                await BuildInfoLoader.Instance.LoadAsync();
            WriteMetadata(faceExprCount);
        }

        private void WriteMetadata(int? faceExprCount = null)
        {
            // 0) Capture skeletons once (if hands are enabled)
            OVRPlugin.Skeleton2 leftSkel = default, rightSkel = default;
            bool haveLeft = recordingOptions.includeHands && OVRPlugin.GetSkeleton2(OVRPlugin.SkeletonType.HandLeft, ref leftSkel);
            bool haveRight = recordingOptions.includeHands && OVRPlugin.GetSkeleton2(OVRPlugin.SkeletonType.HandRight, ref rightSkel);

            // 1) Build the object
            var meta = new SessionMetaData
            {
                // session and identity
                session_id = sessionTime,
                utc_start_iso8601 = DateTime.UtcNow.ToString("o"),
                device_utc_offset = DateTimeOffset.Now.Offset.ToString(),
                platform = Application.platform.ToString(),
                unity_version = Application.unityVersion,

                // recording options (1:1 with RecordingOptions)
                includeNodes = recordingOptions.includeNodes,
                includeEyes = recordingOptions.includeEyes,
                includeHands = recordingOptions.includeHands,
                includeBody = recordingOptions.includeBody,
                includePerformance = recordingOptions.includePerformance,
                includeGaze = recordingOptions.includeGaze,
                includeSeparateEyesGaze = recordingOptions.includeSeparateEyesGaze,
                includeSystemStatus = recordingOptions.includeSystemStatus,
                custom_transforms_count = recordingOptions.customTransformsToRecord?.Count ?? 0,
                custom_transforms_names = recordingOptions.customTransformsToRecord != null
                    ? recordingOptions.customTransformsToRecord.Where(t => t != null).Select(t => t.name).ToArray()
                    : System.Array.Empty<string>(),

                // legacy toggles (backward compatibility)
                eyes_enabled = recordingOptions.includeEyes,
                hands_enabled = recordingOptions.includeHands,
                body_enabled = recordingOptions.includeBody,
                face_enabled = recordFaceExpressions,
                controllers_enabled = true, // set to always true for now, since we don't have a way to gate controllers yet

                // OVR sampling (document the choice)
                ovr_step_name = OvrSampling.StepDefault.ToString(),
                ovr_step_value = (int)OvrSampling.StepDefault,

                // sampling timing
                sampling_mode = "FixedUpdate",
                timeScale = Time.timeScale,
                fixedDeltaTime = Time.fixedDeltaTime,

                // for rotation conversion (Unity default: ZXY)
                rotation_units = "degrees",

                // Motion-BIDS provenance (for pipeline)
                manufacturers_model_name_raw = SystemInfo.deviceModel ?? "",
                software_versions_raw = SystemInfo.operatingSystem ?? "",
                device_serial_number = "",
                device_serial_number_note = "It is no longer possible to reliably get the unique hardware serial number of a Meta Quest device from within a Unity application due to privacy restrictions imposed by Android 10 and later",

                // Reference frames for later *_channels.json generation
                reference_frames = new ReferenceFramesBlock
                {
                    UnityWorld = new ReferenceFrameDesc
                    {
                        RotationRule = "left-hand",
                        RotationOrder = "ZXY",
                        SpatialAxes = "+X right, +Y up, +Z forward (Unity world)"
                    },
                    HandLocal = new ReferenceFrameDesc
                    {
                        RotationRule = "right-hand",
                        RotationOrder = "ZXY",
                        SpatialAxes = "hand-local axes relative to hand root (tracking space)"
                    }
                }
            };


            // fill detected_hand_bones if we have a skeleton
            if (haveLeft) meta.detected_hand_bones = Math.Max(meta.detected_hand_bones, (int)leftSkel.NumBones);
            if (haveRight) meta.detected_hand_bones = Math.Max(meta.detected_hand_bones, (int)rightSkel.NumBones);

            // detected_face_expr_count (from schema when face recording enabled)
            if (recordFaceExpressions && faceExprCount.HasValue)
                meta.detected_face_expr_count = faceExprCount.Value;

            // Build info: only set build_id, git_commit, utc_build_iso8601 when available (no placeholders)
            BuildInfo bi = BuildInfoLoader.Instance?.Current;
            meta.build_info_available = bi != null && !string.IsNullOrWhiteSpace(bi.build_id);
            if (meta.build_info_available)
            {
                meta.build_id = bi.build_id;
                meta.utc_build_iso8601 = bi.utc_build_iso8601 ?? "";
                meta.git_commit = bi.git_commit ?? "";
                if (!string.IsNullOrEmpty(bi.unity))
                    meta.unity_version = bi.unity;
            }
            // when false: leave build_id, git_commit, utc_build_iso8601 at default empty

            // 3) OVR/OVRPlugin versions and tracking origin (Meta: EyeLevel/FloorLevel/Stage)
            try
            {
                meta.ovrplugin_wrapper_version = OVRPlugin.wrapperVersion.ToString();
                meta.ovrplugin_runtime_version = OVRPlugin.version.ToString();
                meta.tracking_origin_type = OVRPlugin.GetTrackingOriginType().ToString();
            }
            catch { /* safe no-op if OVR not present */ }

            // 4) Finally write
            SessionMetaWriter.WriteInitial(GetOutputDirectory(), SessionTime, meta);
        }
        #endregion

    }

    #region helper structs
    public readonly struct ContinuousSample
    {
        public readonly float sampleTime; // timeSinceStartup at the beginning of the sample window
        public readonly ColumnIndex schema;
        public readonly object[] values;
        public readonly System.Collections.BitArray columnIsSetMask; // which columns were written this frame
        public ContinuousSample(float t, ColumnIndex s, object[] v, System.Collections.BitArray m)
        { sampleTime = t; schema = s; values = v; columnIsSetMask = m; }
    }

    public readonly struct FaceExpressionSample
    {
        public readonly float sampleTime;
        public readonly ColumnIndex schema;
        public readonly object[] values;
        public readonly System.Collections.BitArray columnIsSetMask;
        public FaceExpressionSample(float t, ColumnIndex s, object[] v, System.Collections.BitArray m)
        { sampleTime = t; schema = s; values = v; columnIsSetMask = m; }
    }



    // this class was made for hand skeleton reconstruction from the raw data. it is not used in the current implementation, since they are pretty well documented under the Meta sdk.
    [Serializable]
    public class HandSkeletonMeta
    {
        public string skeleton_type;        // "HandLeft" or "HandRight"
        public int num_bones;
        public int[] parent_index;         // length = num_bones
        public string[] bone_id;            // human readable, optional
        public float[][] bind_pos;          // [i][x,y,z]
        public float[][] bind_rot;          // [i][x,y,z,w]


        public static void WriteHandSkeletonJson(string path, OVRPlugin.Skeleton2 sk)
        {
            var m = new HandSkeletonMeta
            {
                skeleton_type = sk.Type.ToString(),
                num_bones = (int)sk.NumBones,
                parent_index = new int[sk.NumBones],
                bone_id = new string[sk.NumBones],
                bind_pos = new float[sk.NumBones][],
                bind_rot = new float[sk.NumBones][]
            };

            for (int i = 0; i < sk.NumBones; i++)
            {
                var b = sk.Bones[i];
                m.parent_index[i] = b.ParentBoneIndex;
                m.bone_id[i] = b.Id.ToString();
                m.bind_pos[i] = new[] { b.Pose.Position.x, b.Pose.Position.y, b.Pose.Position.z };
                m.bind_rot[i] = new[] { b.Pose.Orientation.x, b.Pose.Orientation.y, b.Pose.Orientation.z, b.Pose.Orientation.w };
            }

            var json = JsonUtility.ToJson(m, true);
            File.WriteAllText(path, json);
        }
    }

    #endregion
}


