// RecordingConfigOverride.cs
// Perf-test only. Applies one of PerfTestConfig's presets to the scene's ResXRDataManager
// BEFORE ResXRDataManager.Awake() runs, so the preset actually controls which collectors and
// columns get built. Add to a GameObject in Base Scene only for the duration of a perf-test
// campaign, alongside SoakLoopController and ResXRDataPerfSampler — see README.md.
//
// Must run before ResXRDataManager.Awake(): ResXRSingleton<T>.Awake() sets Instance and then
// synchronously calls DoInAwake(), which is where recordingOptions gets consumed to build the
// schema and collector list. So this script (a) uses [DefaultExecutionOrder] to run first, and
// (b) can't use ResXRDataManager.Instance at that point — it isn't set until ResXRDataManager's
// own Awake runs — so it finds the component directly instead.

using UnityEngine;

namespace ResXRData
{
    [DefaultExecutionOrder(-1000)]
    public class RecordingConfigOverride : MonoBehaviour
    {
        [Header("Editor convenience — ignored in device builds, where PerfTestConfig.txt always decides")]
        [Tooltip("When true and running in the Editor, use Editor Preset below instead of reading PerfTestConfig.txt — lets you smoke-test a preset with just Play, no file writes needed.")]
        public bool useEditorPresetOverride = true;

        [Tooltip("Preset applied when running in the Editor with the toggle above on.")]
        public PerfTestPreset editorPreset = PerfTestPreset.Default;

        private PerfTestPreset _appliedPreset;

        private void Awake()
        {
            PerfTestConfig.EnsureLoaded();
            _appliedPreset = (Application.isEditor && useEditorPresetOverride) ? editorPreset : PerfTestConfig.Preset;

            ResXRDataManager dataManager = FindFirstObjectByType<ResXRDataManager>();
            if (dataManager == null)
            {
                Debug.LogError("[RecordingConfigOverride] No ResXRDataManager found in scene — cannot apply perf-test preset.");
                return;
            }

            ApplyPreset(dataManager, _appliedPreset);
            Debug.Log($"[RecordingConfigOverride] Applied preset: {_appliedPreset}");
        }

        private void Start()
        {
            // Deferred to Start(): ResXRDataManager.Instance is only set once its own Awake()
            // has run, which Unity guarantees has happened by the time any Start() runs,
            // regardless of script execution order.
            if (ResXRDataManager.Instance == null) return;

            string eventName = $"perf_preset:{_appliedPreset}";
            ResXRDataManager.Instance.ReportEvent(eventName, Time.realtimeSinceStartup, 0f);
            ResXRDataManager.Instance.LogLineToFile($"[RecordingConfigOverride] Applied preset: {_appliedPreset}");
        }

        private static void ApplyPreset(ResXRDataManager dataManager, PerfTestPreset preset)
        {
            RecordingOptions options = dataManager.recordingOptions;

            switch (preset)
            {
                case PerfTestPreset.Default:
                    // No-op — leave the scene's shipped configuration untouched.
                    break;

                case PerfTestPreset.Minimal:
                    SetAllOff(options);
                    options.includeNodes = true;
                    dataManager.recordFaceExpressions = false;
                    break;

                case PerfTestPreset.Maximum:
                    // Shipped config already has everything else on — only Body needs flipping.
                    options.includeBody = true;
                    break;

                case PerfTestPreset.AllOff:
                    SetAllOff(options);
                    dataManager.recordFaceExpressions = false;
                    break;

                case PerfTestPreset.FullyOff:
                    SetAllOff(options);
                    dataManager.recordFaceExpressions = false;
                    dataManager.DisableContinuousLoggingForBaselineTest = true;
                    break;
            }
        }

        private static void SetAllOff(RecordingOptions options)
        {
            options.includeNodes = false;
            options.includeEyes = false;
            options.includeHands = false;
            options.includeBody = false;
            options.includePerformance = false;
            options.includeGaze = false;
            options.includeSeparateEyesGaze = false;
            options.includeSystemStatus = false;
        }
    }
}
