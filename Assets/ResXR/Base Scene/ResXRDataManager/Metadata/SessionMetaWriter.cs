// Writes session_metadata.json with detected skeleton sizes and flags.
// Metadata is designed to support later Motion-BIDS export (Python pipeline generates BIDS files).

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace ResXRData
{
    [Serializable]
    public class ReferenceFrameDesc
    {
        public string RotationRule = "";
        public string RotationOrder = "";
        public string SpatialAxes = "";
    }

    [Serializable]
    public class ReferenceFramesBlock
    {
        public ReferenceFrameDesc UnityWorld;
        public ReferenceFrameDesc HandLocal;
    }

    [Serializable]
    public sealed class SessionMetaData
    {
        // ---- Identity / timing ----
        public string session_id = "";
        public string utc_start_iso8601 = "";
        public string device_utc_offset = "";

        // ---- Unity / platform ----
        public string unity_version = "";
        public string platform = "";

        // ---- Build info ----
        public bool build_info_available = false;
        public string build_id = "";
        public string git_commit = "";
        public string utc_build_iso8601 = "";

        // ---- OVR / SDK versions ----
        public string ovrplugin_runtime_version = "";
        public string ovrplugin_wrapper_version = "";

        // ---- Sampling / clocks ----
        public string sampling_mode = "FixedUpdate";
        public float fixedDeltaTime = 0f;
        public float timeScale = 1f;
        public string ovr_step_name = OvrSampling.StepDefault.ToString();
        public int ovr_step_value = 0;

        // ---- Schema / rotation ----
        public string schema_rev = "2";
        public string rotation_euler_order = "ZXY";
        public string rotation_units = "degrees";

        // ---- Recording options (1:1 with RecordingOptions) ----
        public bool includeNodes;
        public bool includeEyes;
        public bool includeHands;
        public bool includeBody;
        public bool includePerformance;
        public bool includeGaze;
        public bool includeSeparateEyesGaze;
        public bool includeSystemStatus;
        public int custom_transforms_count;
        public string[] custom_transforms_names;

        // ---- Legacy toggles (kept for backward compatibility; mirror include* above) ----
        public bool face_enabled;   // from recordFaceExpressions (manager)
        public bool body_enabled;
        public bool hands_enabled;
        public bool eyes_enabled;
        public bool controllers_enabled;

        // ---- Detected skeleton sizes (for traceability) ----
        public int detected_hand_bones = 0;
        public bool overprovisioned_hand_bones = false;
        public int detected_body_joints = 0;
        public bool overprovisioned_body_joints = false;
        public int detected_face_expr_count = 0;

        // ---- Motion-BIDS / motion.json provenance (for pipeline) ----
        public string manufacturers_model_name_raw = "";
        public string software_versions_raw = "";
        public string device_serial_number = "";
        public string device_serial_number_note = "";

        // ---- Tracking origin (Meta: EyeLevel / FloorLevel / Stage) ----
        public string tracking_origin_type = "";

        // ---- Reference frames for later *_channels.json generation ----
        public ReferenceFramesBlock reference_frames;

        // ---- Compact data source provenance ----
        public Dictionary<string, string> data_sources = new();
    }

    public static class SessionMetaWriter
    {
        private static string FileName = "session_metadata.json";
        public static string GetPath(string directory) => Path.Combine(directory, FileName);

        public static void WriteInitial(string directory, string fileNamePrefix, SessionMetaData meta)
        {
            FileName = string.IsNullOrWhiteSpace(fileNamePrefix) ? FileName : $"{fileNamePrefix}_{FileName}";
            Directory.CreateDirectory(directory);
            var json = JsonUtility.ToJson(meta, prettyPrint: true);
            AtomicWrite(GetPath(directory), json);
        }

        // Small safety: write to .tmp then move
        private static void AtomicWrite(string path, string json)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
    }

}