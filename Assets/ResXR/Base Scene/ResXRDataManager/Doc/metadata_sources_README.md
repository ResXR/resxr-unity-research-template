# SessionMetadata — Field Reference

This file documents every field in `{sessionTime}_SessionMetadata.json`.
For each field: the C# call that populates it, what it means in plain English, and known possible values or an example.

Written once at session start by `SessionMetaWriter.WriteInitial()`, called from `ResXRDataManager.WriteMetadata()` in `DoInAwake()`.

---

## Identity / Timing

| Field | Source | Meaning | Example / Values |
|---|---|---|---|
| `session_id` | `sessionTime` | Session timestamp. Shared prefix for every file in this session folder. | `"2026.06.08_16-58"` |
| `utc_start_iso8601` | `DateTime.UtcNow.ToString("o")` | Exact UTC moment the session was initialised, ISO 8601 round-trip format. | `"2026-06-08T16:58:57.1213380Z"` |
| `device_utc_offset` | `DateTimeOffset.Now.Offset.ToString()` | The device's local UTC offset at session start, DST-aware. Use this to convert `session_id` to local time if needed. | `"03:00:00"`, `"-05:00:00"` |

---

## Unity / Platform

| Field | Source | Meaning | Example / Values |
|---|---|---|---|
| `unity_version` | `Application.unityVersion` (overridden by `build_info.json` when available) | Unity runtime version the app was built with. | `"6000.0.68f1"` |
| `platform` | `Application.platform.ToString()` | Unity `RuntimePlatform` enum — where the app is actually running. | `"Android"` (Quest standalone), `"WindowsPlayer"` (PCVR build), `"WindowsEditor"` (Play Mode) |

---

## Build Provenance

Populated from `StreamingAssets/build_info.json`, which is stamped into the APK at build time by `AutoBuildInfo.cs`. When running directly from the Editor (no build step), or if the JSON is missing, `build_info_available` is `false` and the three build fields are left empty — no placeholders.

| Field | Source | Meaning | Example / Values |
|---|---|---|---|
| `build_info_available` | `BuildInfoLoader.Instance?.Current` — `true` when loaded and `build_id` non-empty | Tells the pipeline whether build provenance fields are trustworthy. When `false`, treat `build_id`, `git_commit`, and `utc_build_iso8601` as absent. | `true` / `false` |
| `build_id` | `bi.build_id` — random hex string written by `AutoBuildInfo.cs` at build time | Unique identifier for the specific APK build. Lets you correlate a data session to an exact binary. Empty `""` when `build_info_available` is `false`. | `"a3f7c1b2"` |
| `git_commit` | `bi.git_commit` — short hash from `git rev-parse --short HEAD` at build time | Git commit the build was made from. Blank if git was unavailable when the build ran. | `"d4e8f12"`, `""` |
| `utc_build_iso8601` | `bi.utc_build_iso8601` — `DateTime.UtcNow.ToString("o")` at build time | UTC timestamp of when `AutoBuildInfo.cs` ran (i.e. when the Unity build completed). | `"2026-06-08T12:00:00.0000000Z"` |

---

## OVR / SDK Versions

Both fields are read inside a `try/catch` — safe no-op if the OVR runtime is not present (e.g. in Editor without a headset).

| Field | Source | Meaning | Example / Values |
|---|---|---|---|
| `ovrplugin_runtime_version` | `OVRPlugin.version.ToString()` | Meta XR SDK version running on the device's system firmware. | `"1.110.0"` |
| `ovrplugin_wrapper_version` | `OVRPlugin.wrapperVersion.ToString()` | Meta XR SDK version compiled into the Unity app itself. Should match runtime; a mismatch may indicate a stale build. | `"1.110.0"` |

---

## Sampling / Clocks

Documents how continuous data is timed so the pipeline can interpret `timeSinceStartup` correctly.

| Field | Source | Meaning | Example / Values |
|---|---|---|---|
| `sampling_mode` | Hardcoded `"FixedUpdate"` | Continuous data is always sampled once per Unity physics tick. | `"FixedUpdate"` |
| `fixedDeltaTime` | `Time.fixedDeltaTime` | Duration of one physics tick in seconds — the actual continuous data sampling interval. 1 / this = Hz. | `0.01` → 100 Hz |
| `timeScale` | `Time.timeScale` | Unity's time scale at session start; normally 1.0. Values other than 1.0 affect `FixedUpdate` rate. | `1.0` |
| `ovr_step_name` | `OvrSampling.StepDefault.ToString()` | OVR pose-sampling step used when reading all device poses. Determines which point in the render pipeline data is read from. | `"Render"` |
| `ovr_step_value` | `(int)OvrSampling.StepDefault` | Integer value of the OVR sampling step. `-1` = `Render`. | `-1` |

---

## Schema / Rotation

Fixed values that document the coordinate conventions used in every CSV file.

| Field | Source | Meaning | Example / Values |
|---|---|---|---|
| `schema_rev` | Hardcoded `"2"` | Internal version of this metadata schema. Incremented if the field layout changes. | `"2"` |
| `rotation_euler_order` | Hardcoded `"ZXY"` | Order in which Unity decomposes quaternions into Euler angles (`Transform.eulerAngles`). All rotation columns in the CSV follow this order. | `"ZXY"` |
| `rotation_units` | Hardcoded `"degrees"` | Unit of all rotation values in the CSV files. | `"degrees"` |

---

## Recording Options

1:1 with the inspector toggles on the `ResXR_DataManager` prefab. Each flag controls both what is logged to CSV and what columns are present in `ContinuousData.csv`.

| Field | Source | Meaning | Example / Values |
|---|---|---|---|
| `includeNodes` | `recordingOptions.includeNodes` | Device node poses (head, controllers, hand roots) logged to ContinuousData. | `true` / `false` |
| `includeEyes` | `recordingOptions.includeEyes` | Eye gaze quaternions, validity, and per-eye confidence logged. | `true` / `false` |
| `includeHands` | `recordingOptions.includeHands` | Hand tracking state, per-bone poses, and confidence logged. | `true` / `false` |
| `includeBody` | `recordingOptions.includeBody` | Full-body joint tracking logged. | `true` / `false` |
| `includePerformance` | `recordingOptions.includePerformance` | Reserved — performance metrics (not yet implemented, columns always absent). | `false` |
| `includeGaze` | `recordingOptions.includeGaze` | Combined (cyclopean) gaze hit point (`EyeGazeHitPosition_X/Y/Z`) and `FocusedObject` logged. | `true` / `false` |
| `includeSeparateEyesGaze` | `recordingOptions.includeSeparateEyesGaze` | Per-eye hit points and focused objects logged. When `true`, ResXREyeTracker runs 3 raycasts per frame instead of 1. | `false` / `true` |
| `includeSystemStatus` | `recordingOptions.includeSystemStatus` | Recenter events, tracking origin changes, user presence, and tracking loss logged. | `true` / `false` |
| `custom_transforms_count` | `recordingOptions.customTransformsToRecord?.Count ?? 0` | Number of scene objects registered for custom transform tracking in the inspector. | `0`, `3` |
| `custom_transforms_names` | Non-null transform names from `customTransformsToRecord` | Array of Unity GameObject names for the custom-tracked objects. In declaration order, matching their CSV columns. | `[]`, `["Stimulus_A", "Target_B"]` |

---

## Legacy Toggles

Kept for backward compatibility with older pipeline versions. They mirror the `include*` fields above — use the `include*` fields for new code.

| Field | Source | Mirrors | Example / Values |
|---|---|---|---|
| `face_enabled` | `recordFaceExpressions` (manager field) | Whether face expressions are recorded (controls `FaceExpressions.csv`). Separate inspector toggle from the main recording options. | `true` / `false` |
| `body_enabled` | `recordingOptions.includeBody` | `includeBody` | `true` / `false` |
| `hands_enabled` | `recordingOptions.includeHands` | `includeHands` | `true` / `false` |
| `eyes_enabled` | `recordingOptions.includeEyes` | `includeEyes` | `true` / `false` |
| `controllers_enabled` | Hardcoded `true` | Always `true` — no mechanism to gate controllers yet. | `true` |

---

## Schema Allocation Sizes

The number of columns reserved in the CSV for each modality. Set once at session start from the same values used to build the schema — so these counts always match the actual column count in the file. `0` means the modality was disabled.

If a live device query after initialisation returns a different count, a `Debug.LogError` is fired and the mismatch is written to `ResXRDebugLogs.csv`.

| Field | Source | Meaning | Example / Values |
|---|---|---|---|
| `schema_hand_bones` | `SchemaFactories.DetectHandBoneCount()` — compile-time constant based on `OVRRuntimeSettings.HandSkeletonVersion` | Hand bone columns per side (left and right use the same count). `0` if hands not enabled. | `24`, `0` |
| `schema_body_joints` | `SchemaFactories.DetectBodyJointCount()` — falls back to `SkeletonConstants.MaxBodyBones` (70) at session start before body tracking is initialised | Body joint columns in `ContinuousData.csv`. `0` if body not enabled. | `70`, `0` |
| `schema_face_expressions` | `Enum.GetNames(typeof(OVRPlugin.FaceExpression2))` excluding sentinels | Number of face expression weight columns in `FaceExpressionData.csv`. `0` if face recording not enabled. | `70`, `0` |

---

## Device Provenance

Raw strings from Unity's `SystemInfo` API. Forwarded as-is to the pipeline to generate BIDS `*_motion.json` device fields.

| Field | Source | Meaning | Example / Values |
|---|---|---|---|
| `manufacturers_model_name_raw` | `SystemInfo.deviceModel` | Raw device model string Unity reports. Not normalised — exact value varies by OS/firmware. | `"Oculus Quest Pro"`, `"Oculus Quest 3"` |
| `software_versions_raw` | `SystemInfo.operatingSystem` | Full OS version string Unity reports. | `"Android OS 14 / API-34 (UP1A.231005.007.A1/51436340031400340)"` |
| `horizon_os_version` | `android.os.SystemProperties.get("ro.hzos.build.display_name")` via `AndroidJavaClass` | Meta Horizon OS release string — the Meta-specific version shown in the headset's About page, distinct from the Android OS version. Sentinels: `"editor"` = Play Mode; `"n/a"` = PCVR/Windows build (property doesn't exist on Windows); `"unknown"` = ran on device but the call failed. | `"2.4"`, `"editor"`, `"n/a"`, `"unknown"` |
| `device_serial_number` | Hardcoded `""` | Always empty. Meta Quest serial numbers cannot be read from Unity on Android 10+ due to privacy restrictions. | `""` |
| `device_serial_number_note` | Hardcoded string | Explains why `device_serial_number` is always empty. Present so consumers know this is expected, not a bug. | `"It is no longer possible to reliably get the unique hardware serial number..."` |

---

## Tracking Origin

| Field | Source | Meaning | Example / Values |
|---|---|---|---|
| `tracking_origin_type` | `OVRPlugin.GetTrackingOriginType().ToString()` | The Meta tracking origin mode active when the session started. Affects the physical meaning of all world-space positions. `EyeLevel` = origin at head height; `FloorLevel` / `Stage` = origin at floor. | `"EyeLevel"`, `"FloorLevel"`, `"Stage"` |

---

## Reference Frames

Fixed values describing the coordinate systems used in the CSV files. Consumed by the pipeline to generate `*_channels.json` BIDS coordinate frame metadata. Both sub-objects are always present with the values shown — they are hardcoded, not detected at runtime.

| Field | Value | Meaning |
|---|---|---|
| `reference_frames.UnityWorld.RotationRule` | `"left-hand"` | Unity's world coordinate system is left-handed (+X right, +Y up, +Z forward). |
| `reference_frames.UnityWorld.RotationOrder` | `"ZXY"` | Euler decomposition order for world-space rotations (`Transform.eulerAngles`). |
| `reference_frames.UnityWorld.SpatialAxes` | `"+X right, +Y up, +Z forward (Unity world)"` | Axis directions in Unity world space. |
| `reference_frames.HandLocal.RotationRule` | `"right-hand"` | OVR hand-local space is right-handed (native tracking space convention). |
| `reference_frames.HandLocal.RotationOrder` | `"ZXY"` | Euler decomposition order for hand-local rotations. |
| `reference_frames.HandLocal.SpatialAxes` | `"hand-local axes relative to hand root (tracking space)"` | Bone positions are relative to the hand's root pose; not converted to world space. |

---

## Data Sources

| Field | Source | Meaning | Example / Values |
|---|---|---|---|
| `data_sources` | Intended to be populated from `data_sources_README.txt` content; not yet implemented at runtime | A compact map of builtin CSV output files (ContinuousData + FaceExpressionData) were written and their column schema — the machine-readable equivalent of `data_sources_README.txt`. Always an empty object `{}` until implemented. | `{}` |
