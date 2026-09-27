// PerfTestConfig.cs
// Perf-test only. Shared run parameters for the ResXRDataManager perf-test harness, read from
// a plain-text file at Application.persistentDataPath/PerfTestConfig.txt so the whole test
// matrix (Minimal/Default/Maximum/AllOff/FullyOff) can be driven from one build: edit the file
// via `adb shell`, relaunch — no rebuild needed between conditions. See README.md.
//
// File format (one "Key=Value" per line, "#" starts a comment):
//   Preset=Maximum
//   RepeatCount=3
//   RepeatDurationSeconds=300

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ResXRData
{
    public enum PerfTestPreset
    {
        Default,   // No-op — leaves the scene's shipped RecordingOptions untouched.
        Minimal,   // Only includeNodes on; everything else (incl. face) off.
        Maximum,   // Shipped config + includeBody on.
        AllOff,    // Every RecordingOptions field off — isolates the fixed per-tick I/O tax.
        FullyOff,  // AllOff + DisableContinuousLoggingForBaselineTest — true zero-cost floor.
    }

    public static class PerfTestConfig
    {
        private const string FileName = "PerfTestConfig.txt";

        public static PerfTestPreset Preset { get; private set; } = PerfTestPreset.Default;
        public static int RepeatCount { get; private set; } = 3;
        public static float RepeatDurationSeconds { get; private set; } = 300f;

        private static bool s_loaded;

        /// <summary>
        /// Reads PerfTestConfig.txt on first call; subsequent calls are a no-op. Missing file,
        /// unreadable file, or an unrecognized Preset value all fall back to the defaults above
        /// and log why via Debug.Log/LogWarning.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (s_loaded) return;
            s_loaded = true;

            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (!File.Exists(path))
            {
                Debug.Log($"[PerfTestConfig] No {FileName} at {path} — using defaults " +
                          $"(Preset={Preset}, RepeatCount={RepeatCount}, RepeatDurationSeconds={RepeatDurationSeconds}).");
                return;
            }

            try
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                }

                if (values.TryGetValue("Preset", out string presetStr))
                {
                    if (Enum.TryParse(presetStr, ignoreCase: true, out PerfTestPreset parsed))
                        Preset = parsed;
                    else
                        Debug.LogWarning($"[PerfTestConfig] Unrecognized Preset '{presetStr}' in {FileName} — falling back to {Preset}.");
                }

                if (values.TryGetValue("RepeatCount", out string repeatStr)
                    && int.TryParse(repeatStr, out int repeatCount) && repeatCount > 0)
                    RepeatCount = repeatCount;

                if (values.TryGetValue("RepeatDurationSeconds", out string durationStr)
                    && float.TryParse(durationStr, out float duration) && duration > 0f)
                    RepeatDurationSeconds = duration;

                Debug.Log($"[PerfTestConfig] Loaded from {path}: " +
                          $"Preset={Preset}, RepeatCount={RepeatCount}, RepeatDurationSeconds={RepeatDurationSeconds}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerfTestConfig] Failed to read {path}: {ex.Message} — using defaults " +
                                $"(Preset={Preset}, RepeatCount={RepeatCount}, RepeatDurationSeconds={RepeatDurationSeconds}).");
            }
        }
    }
}
