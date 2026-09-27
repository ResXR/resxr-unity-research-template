// SoakLoopController.cs
// Perf-test only. Turns a demo's short default session into a controlled-duration soak run by
// intercepting Application.Quit() — which every demo's SessionManager.EndSession() calls as its
// documented last line — and restarting the active scene instead, until RepeatCount x
// RepeatDurationSeconds has elapsed. Add to a GameObject in Base Scene only for the duration of
// a perf-test campaign — see README.md.
//
// Why Application.wantsToQuit and not a timer that force-restarts mid-flow: every demo's
// EndSession() calls Application.Quit() only after its flow has already reached a clean
// completion point (all ReportEvent calls made, nothing mid-await). wantsToQuit fires right
// before the real process exit and lets us cancel it by returning false — so a repeat only ever
// restarts between natural playthroughs, never mid-trial. Expect each repeat to overshoot its
// target duration slightly (bounded by one natural playthrough's length) — accepted, since
// forcing an exact cutoff would risk tearing down mid-flow.
//
// All repeats of one condition land in one continuous session folder — Base Scene (and this
// component) is never reloaded, only the demo scene is, via ResXRSceneManager.RestartActiveScene.
// Repeat boundaries are marked in Events.csv (perf_repeat_start:N / perf_repeat_end:N) rather
// than by separate files, so within-condition drift across the full soak is visible in one CSV.

using UnityEngine;

namespace ResXRData
{
    public class SoakLoopController : MonoBehaviour
    {
        private int _repeatIndex;
        private int _repeatCount;
        private float _repeatDurationSeconds;
        private float _repeatStartTime;
        private bool _subscribed;

        private void Start()
        {
            if (ResXRDataManager.Instance == null)
            {
                Debug.LogError("[SoakLoopController] No ResXRDataManager found in scene — soak loop disabled.");
                enabled = false;
                return;
            }
            if (ResXRSceneManager.Instance == null)
            {
                Debug.LogError("[SoakLoopController] No ResXRSceneManager found in scene — soak loop disabled.");
                enabled = false;
                return;
            }

            PerfTestConfig.EnsureLoaded();
            _repeatCount = PerfTestConfig.RepeatCount;
            _repeatDurationSeconds = PerfTestConfig.RepeatDurationSeconds;

            _repeatIndex = 0;
            _repeatStartTime = Time.realtimeSinceStartup;

            Application.wantsToQuit += OnWantsToQuit;
            _subscribed = true;

            LogEvent($"perf_repeat_start:{_repeatIndex}");
        }

        private void OnDestroy()
        {
            if (_subscribed) Application.wantsToQuit -= OnWantsToQuit;
        }

        private bool OnWantsToQuit()
        {
            float elapsed = Time.realtimeSinceStartup - _repeatStartTime;

            if (elapsed < _repeatDurationSeconds)
            {
                LogEvent("perf_loop_restart");
                RestartWithinRun();
                return false; // cancel this quit — same repeat continues with another playthrough
            }

            LogEvent($"perf_repeat_end:{_repeatIndex}");
            _repeatIndex++;

            if (_repeatIndex < _repeatCount)
            {
                _repeatStartTime = Time.realtimeSinceStartup;
                LogEvent($"perf_repeat_start:{_repeatIndex}");
                RestartWithinRun();
                return false;
            }

            LogEvent("perf_run_complete");
            return true; // let the real quit proceed — this is what triggers OnDestroy()'s flush
        }

        private void RestartWithinRun()
        {
            ResXRSceneManager.Instance.RestartActiveScene();
        }

        private void LogEvent(string name)
        {
            ResXRDataManager.Instance.ReportEvent(name, Time.realtimeSinceStartup, 0f);
            Debug.Log($"[SoakLoopController] {name} (repeat {_repeatIndex + 1}/{_repeatCount})");
        }
    }
}
