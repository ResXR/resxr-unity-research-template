# ResXR DataManager — Reference Guide

> **Quick reference**: For the full list of columns logged automatically (head/eyes/hands/body/face), see `data_sources_README.txt`.

---

## What is the ResXR DataManager?

The ResXR DataManager is a self-contained data collection and export system. Once it is in your scene, it handles everything related to saving data — you never write a file, open a stream, or manage a CSV manually.

It does two things:

1. **Continuous logging** — every physics tick (100 Hz by default), it polls all active collectors (head, gaze, hands, body, face expressions, custom transforms) and writes a row to `ContinuousData.csv`.
2. **Custom event logging** — whenever your experiment code decides something happened (a trial started, the participant chose an option, a stimulus appeared), you call a single reporter function and one row is appended to your custom CSV.

Your interaction points are:
- **Custom data classes** — define what experiment-specific data to log and in which format
- **Reporter functions** — thin wrappers in `ResXRDataManager.cs` that give your experiment scripts a clean, readable API
- **Custom transforms** — drag objects in the Inspector to record their position/rotation every frame alongside the rest of continuous data

Everything else — file creation, headers, flushing, crash safety, metadata — is handled for you.

---

## Inspector: Recording Options

Set on the `ResXR_DataManager_V2` prefab in your scene:

| Option | What it does |
|---|---|
| **Include Nodes** | Head position/rotation, controller positions |
| **Include Eyes** | Eye gaze angles, validity, confidence |
| **Include Gaze** | Combined (cyclopean) gaze hit point and focused object |
| **Include Separate Eyes Gaze** | Per-eye hit positions and focused objects. Enables 3 raycasts per frame (left, right, combined) in `ResXREyeTracker`. Turn off in heavy scenes — when disabled, only 1 combined raycast runs. |
| **Include Hands** | Hand tracking, bone positions, confidence |
| **Include Body** | Body joint positions and calibration |
| **Include Face** | Face expression weights and validity (`FaceExpressions.csv`) |
| **Include Performance** | Reserved for performance metrics (optional) |
| **Include System Status** | Recenter events, tracking origin changes, user presence, tracking loss |
| **Custom Transforms To Record** | Drag any scene object — its position/rotation appears as extra columns in `ContinuousData.csv` |

---

## 1. Log Custom Experiment Data — Custom Data Classes

### Overview

Research experiments generate data specific to the paradigm: which stimulus was shown, what choice the participant made, how long they took, which hand they used. This data is different for every experiment, so the DataManager cannot know its schema in advance.

The **Custom Data Class** system solves this. You define a plain C# class that describes the shape of one row of your data — what columns exist, what type they are. You write that class once. From then on, the DataManager handles everything: it creates the CSV file on the first write, generates the header from your field names, and appends rows whenever you call your reporter function. You never touch the file directly.

This gives you:
- **A clean, typed interface** — your experiment data is always a proper C# object, not an anonymous string
- **Automatic file management** — no `StreamWriter`, no `File.Open`, no flushing logic
- **Crash-safe writes** — every row is flushed to disk immediately
- **BIDS-compatible column documentation** — `[ColumnInfo]` attributes on each field document what each column means; these are compiled automatically into a sidecar JSON consumed by the ResXR Python pipeline for BIDS export
- **Compile-time safety** — missing annotations are flagged as hard errors in the Unity Editor after every compile, so gaps never slip into a build unnoticed

---

### Step 1: Define your data class

Create a class implementing `CustomDataClass`. The interface requires two auto-properties:
- `onset { get; }` — when the event happened. Always use `Time.realtimeSinceStartup` so it aligns with the continuous data clock.
- `duration { get; }` — how long the event lasted. Use `0f` for instantaneous events; for windowed events, emit the row at the END with `duration = endTime - startTime`.

All **public fields** (not properties) after onset/duration become CSV columns, in declaration order.

**The class name becomes the CSV filename**: a class called `ChoiceEvents` produces `{sessionTime}_ChoiceEvents.csv`. Name your class to describe what it records — no `TableName` property is needed.

```csharp
using ResXRData;
using UnityEngine;

public class ChoiceEvents : CustomDataClass
{
    // Required interface properties — always set in constructor
    public float onset    { get; }   // Time.realtimeSinceStartup at the moment of choice
    public float duration { get; }   // 0f — instantaneous event

    // Your experiment columns — these become CSV columns in declaration order
    [ColumnInfo("Task name or index", Format = "string")]
    public string Task;

    [ColumnInfo("Trial index within the task", Format = "integer", Minimum = 0)]
    public int Trial;

    [ColumnInfo("Name of the chosen image")]
    public string Choice;

    [ColumnInfo("Slot chosen by the participant", "A:Left slot", "B:Right slot")]
    public string ChosenOption;

    [ColumnInfo("Seconds from stimulus display to choice", Units = "s", Format = "number", Minimum = 0.0)]
    public float ReactionTime;

    // Constructor: set onset/duration first, then all your fields
    public ChoiceEvents(string task, int trial, string choice, string chosenOption, float reactionTime)
    {
        onset        = Time.realtimeSinceStartup;
        duration     = 0f;
        Task         = task;
        Trial        = trial;
        Choice       = choice;
        ChosenOption = chosenOption;
        ReactionTime = reactionTime;
    }
}
```

**Rules:**
- `onset` and `duration` must be **auto-properties** (`{ get; }`), not fields — they are the interface requirement and are always written as the first two columns
- Data columns must be **public fields**, not properties — properties are silently ignored
- Field declaration order = column order in the CSV
- Place your class either in the `#region custom data classes definitions` block in `ResXRDataManager.cs` (simplest), or in a dedicated file next to your experiment scripts (e.g. `BinaryChoiceDataClasses.cs`) for larger experiments

---

### Step 2: Annotate every field with `[ColumnInfo]`

Every public field **must** have a `[ColumnInfo]` annotation. This is enforced at two points:

- **Editor (after every compile)**: `CustomDataClassValidator.cs` (`[InitializeOnLoad]`) logs a hard error in the Unity Console for any unannotated field — `[ResXR: CustomDataClassValidator]`
- **Runtime (session end)**: `ValidateColumnAnnotations()` runs again and writes to both `Debug.LogError` and `ResXRLogs.csv`

If annotation is missing or description is empty, the field name is prettified as a placeholder (e.g. `ReactionTime` → `"Reaction Time"`) and written to the sidecar — but this is a fallback, not accurate documentation.

**Signature:**
```
[ColumnInfo(description, levels..., Units = "...", Format = "...", Minimum = N, Maximum = N)]
```

`description` is the only required argument. All others are optional named properties.

#### [ColumnInfo] examples

```csharp
// Description only (minimum valid annotation):
[ColumnInfo("Name of the chosen image")]
public string Choice;

// With physical units and BIDS format type:
[ColumnInfo("Seconds from stimulus display to choice", Units = "s", Format = "number", Minimum = 0.0)]
public float ReactionTime;

// Categorical — levels always as "value:description" (both parts required):
[ColumnInfo("Slot chosen by the participant", "A:Left slot", "B:Right slot")]
public string ChosenOption;

[ColumnInfo("Hand used to make the choice", "Left:Left hand", "Right:Right hand")]
public string HandUsed;

// Numeric with full range bounds:
[ColumnInfo("Confidence score", Format = "number", Minimum = 0.0, Maximum = 1.0)]
public float Confidence;
```

#### [ColumnInfo] parameter reference

| Parameter | Type | Required | Description |
|---|---|---|---|
| `description` | `string` | **Yes** | Human-readable description of the column. Empty or missing logs a hard error. |
| `levels` | `params string[]` | No | Categorical levels, each as `"value:description"`. Both value and description are required — value-only entries are not allowed. Omit entirely for non-categorical fields. See [BIDS spec](https://bids-specification.readthedocs.io/en/stable/common-principles.html#levels). |
| `Units` | named `string` | No | Physical units (e.g. `"s"`, `"m"`, `"degrees"`). Omit for dimensionless or categorical columns. |
| `Format` | named `string` | No | BIDS column format type. Must be one of 18 allowed values (see below). An unrecognised value logs a hard error at session end. |
| `Minimum` | named `double` | No | Minimum expected value for numeric columns. |
| `Maximum` | named `double` | No | Maximum expected value for numeric columns. |

**Allowed `Format` values** (from the BIDS specification):
```
string, number, integer, boolean, index, label, date, datetime, time, unit,
uri, rrid, bids_uri, dataset_relative, file_relative, participant_relative,
stimuli_relative, hed_version
```

#### What [ColumnInfo] produces at session end

At session end, C# reflection reads all `[ColumnInfo]` attributes across every custom table used in the session, and writes `{sessionTime}_custom_tables_sidecar.json`. The ResXR Python pipeline reads that file to:
- Auto-generate `*_events.json` BIDS sidecar files for each custom CSV
- Merge all custom tables into a single BIDS events file (all tables share the `onset`/`duration` clock from `ContinuousData.csv`)

---

### Step 3: Add a static reporter

The recommended pattern is to add a static `Log()` method directly on the data class itself. This keeps your experiment scripts clean — one readable line instead of `new ChoiceEvents(...)` scattered across your flow scripts — and keeps the reporter co-located with the class it serves.

```csharp
// In the same file as your data class — add this method:
public static void Log(string task, int trial, string choice, string chosenOption, float reactionTime)
{
    ResXRDataManager.Instance.LogCustom(new ChoiceEvents(task, trial, choice, chosenOption, reactionTime));
}
```

Then from your `TrialManager` or anywhere in your experiment:

```csharp
ChoiceEvents.Log(currentTask, trialIndex, chosenImage, chosenOption, rt);
```

You can also skip the reporter and call `LogCustom(new ChoiceEvents(...))` directly — fine for quick prototyping, but the static `Log()` is preferred for anything permanent.

---

### Built-in custom data classes

The template ships with several ready-to-use custom data classes in `ResXRDataManager.cs`. You do not need to modify them.

---

#### `events` — Quick event markers

**CSV:** `{sessionTime}_Events.csv` &nbsp;|&nbsp; **Columns:** `onset`, `duration`, `name`

Logs named experiment milestones — trial starts, stimulus onsets, participant responses, phase transitions — with a single line of code. No class setup needed. Because it uses the same `CustomDataClass` mechanism as all other tables, every row has an `onset` aligned to the continuous data clock, making it straightforward to align events with head/eye/hand data in analysis.

```csharp
// Point event (instantaneous — duration = 0):
ResXRDataManager.Instance.ReportEvent("trial_start", Time.realtimeSinceStartup, 0f);

// Windowed event (emit at the END with actual duration):
float stimStart = Time.realtimeSinceStartup;
// ... present stimulus, wait for response ...
ResXRDataManager.Instance.ReportEvent("stimulus", stimStart, Time.realtimeSinceStartup - stimStart);
```

---

#### `ResXRLogs` — On-device debug logging

**CSV:** `{sessionTime}_ResXRLogs.csv` &nbsp;|&nbsp; **Columns:** `onset`, `duration`, `message`

Writes timestamped text notes to a CSV file alongside all other session data. This is invaluable when debugging on a Quest headset where the Unity Console is not accessible. After the session, pull the files off the device and open `ResXRLogs.csv` to see exactly what happened and when.

The DataManager itself writes here when it detects `[ColumnInfo]` annotation errors — so validation problems will also appear in this file.

```csharp
ResXRDataManager.Instance.LogLineToFile("trial 3 started");
ResXRDataManager.Instance.LogLineToFile($"stimulus loaded: {stimulusName}");
```

> *The Unity Console can be accessed on-device via `adb logcat` — `ResXRLogs` is a simpler alternative that lives directly in your session data folder.*

---

#### `TrialsData` — Universal trial summary

**CSV:** `{sessionTime}_TrialsData.csv` &nbsp;|&nbsp; **Columns:** `onset`, `duration`, `Task`, `Trial`, `TrialName`

A shared trial summary row written by every demo's `TrialManager.EndTrial()`. Provides one consistent row per trial across all experiments, making it easy to merge data from multiple sessions or paradigms. Extend with a demo-specific subclass (e.g. `MazeTrialData`) when you need extra columns — do not modify this class directly. Call `TrialsData.Log(...)` to write a row.

---

## 2. Reporter Functions

### Overview

A reporter is a static `Log()` method defined directly on the data class. It constructs the data object and calls `LogCustom(...)`. You are not required to write one — you can call `LogCustom(new MyClass(...))` directly anywhere — but the pattern has a clear benefit: your experiment scripts (TrialManager, TaskManager, etc.) stay readable and free of data-class construction logic.

The reporter lives in the same file as the class it serves. That way the schema (fields + annotations) and the writer (Log method) are always in one place. See any of the demo data class files for working examples.

### How to Use

```csharp
// Pattern: add a static Log() method on your data class
public static void Log(string task, int trial, string choice, string chosenOption, float reactionTime)
{
    ResXRDataManager.Instance.LogCustom(new ChoiceEvents(task, trial, choice, chosenOption, reactionTime));
}

// Then call it from anywhere in your experiment:
ChoiceEvents.Log(taskName, trialIndex, imageName, slot, rt);
```

The `LogCustom` overloads:

```csharp
// Standard — pass a constructed instance:
LogCustom(new ChoiceEvents(...));

// Lazy — pass a factory lambda (avoids allocating if not always needed):
LogCustom(() => new ChoiceEvents(...));
```

---

## 3. Custom Transforms

### Overview

Sometimes you need to track the position and rotation of specific scene objects — a stimulus that moved, a target the participant reached for, an object they picked up. Rather than writing a custom data class for this, you can register any scene object in the Inspector and its transform data is automatically appended as extra columns to `ContinuousData.csv`, at the same 100 Hz rate as all other continuous data.

This is useful for simple positional tracking. For richer event-style data (e.g. "participant grabbed this object at time T"), a custom data class + reporter is the right approach.

### How to Use

1. Select the `ResXR_DataManager_V2` prefab in your scene
2. Find the **"Custom Transforms To Record"** list in the Inspector
3. Drag any scene object into the list

That is all. Position (X/Y/Z) and rotation (X/Y/Z) columns for each registered object will appear at the end of `ContinuousData.csv`. Column names are derived from the object's name in the scene hierarchy.

---

## 4. Metadata

### Overview

Every session produces two JSON files alongside the CSVs. These files are not intended for manual inspection — they are inputs for the ResXR Python pipeline that generates BIDS-compatible data packages from your raw session data. Together they give the pipeline everything it needs: device identity, build provenance, coordinate frame conventions, recording options, and the schema and documentation for every custom data table used in the session.

---

### `session_metadata.json`

Written **once at session start** by `SessionMetaWriter.cs`. Never modified again after that point.

| Field | Description |
|---|---|
| `session_id` | Session timestamp (e.g. `2026.06.03_14-22`) |
| `utc_start_iso8601` | Exact UTC start time in ISO 8601 format |
| `device_utc_offset` | DST-aware UTC offset at session start |
| `unity_version`, `platform` | Build environment details |
| `build_info_available` | `true` when `build_info.json` was loaded successfully. When `true`, `build_id`, `git_commit`, and `utc_build_iso8601` are populated. When `false`, those three fields are left empty — no placeholders — so the pipeline can reliably detect "not available". |
| `rotation_euler_order` | `"ZXY"` — Unity's `Transform.eulerAngles` convention |
| `tracking_origin_type` | Meta tracking origin: `EyeLevel`, `FloorLevel`, or `Stage` |
| `reference_frames` | `UnityWorld` and `HandLocal` coordinate frame descriptions; used by the pipeline to generate `*_channels.json` |
| `manufacturers_model_name_raw` | `SystemInfo.deviceModel` |
| `software_versions_raw` | `SystemInfo.operatingSystem` |
| `sampling_mode`, `fixedDeltaTime` | Documents the recording rate |
| `data_sources` | Compact map of which CSV files were written and their schema |

> There is also a `build_info.json` generated at build time and embedded in the APK by `AutoBuildInfo.cs`. When present and loaded, its values are written into `session_metadata.json`. When missing, `build_info_available` is `false` and the three build fields are left empty.

---

### `{sessionTime}_custom_tables_sidecar.json`

Written **at session end** by `CustomTablesSidecarWriter.cs`, before CSV files are closed. Contains one entry per custom data class actually used during the session.

Example structure:
```json
{
  "custom_tables": {
    "ChoiceEvents": {
      "file": "2026.06.03_14-22_ChoiceEvents.csv",
      "row_count": 48,
      "columns": {
        "onset":        { "description": "Time since app start when the event was logged", "units": "s", "Format": "number", "Minimum": 0 },
        "duration":     { "description": "Event duration in seconds; 0 for point events",  "units": "s", "Format": "number", "Minimum": 0 },
        "Choice":       { "description": "Name of the chosen image", "units": "n/a" },
        "ChosenOption": { "description": "Slot chosen by the participant", "units": "n/a", "Levels": { "A": "Left slot", "B": "Right slot" } },
        "ReactionTime": { "description": "Seconds from stimulus display to choice", "units": "s", "Format": "number", "Minimum": 0 }
      }
    }
  }
}
```

The pipeline reads this file to:
- Auto-generate `*_events.json` BIDS sidecar files for each custom CSV
- Merge all custom tables into a single BIDS events file (all tables share the same `onset`/`duration` clock as `ContinuousData.csv`)

Any validation errors (missing `[ColumnInfo]`, empty descriptions, unrecognised `Format` values) are written to both `Debug.LogError` and `ResXRLogs.csv` before this file is written.

> **Note:** Fields with no annotation or an empty description still appear in the sidecar, using the prettified field name as a placeholder (e.g. `ReactionTime` → `"Reaction Time"`). A hard error is logged — replace with a proper `[ColumnInfo("description")]` for accurate BIDS metadata.

---

## 5. How It Works (Under the Hood)

You do not need to read this section to use the DataManager. It is here for contributors and for researchers who want full transparency into what runs on their device.

### Architecture

The system is divided into four layers:

**A) Orchestrator** — `ResXRDataManager.cs`  
Sets up all schemas, opens all CSV writers, and drives collectors every `FixedUpdate`. On session end (`OnDestroy`), validates annotations, writes the custom tables sidecar, then closes all files in the correct order.

**B) Core Infrastructure** — `Core Infrastructure/`

| File | Role |
|---|---|
| `SchemaBuilder.cs` | Defines the column layout for each built-in CSV |
| `ColumnIndex.cs` | Ordered column name store; fast lookup by name or index |
| `RowBuffer.cs` | Staging area for one row of values before writing |
| `CsvRowWriter.cs` | Writes one CSV (header + rows); flushes every row immediately to disk |
| `CustomCsvFromDataClass.cs` | Automatic CSV creation and row writing for any `CustomDataClass` |
| `ColumnInfoAttribute.cs` | The `[ColumnInfo]` attribute definition and the 18 allowed BIDS format values |
| `Editor/CustomDataClassValidator.cs` | `[InitializeOnLoad]` Editor script; checks every `CustomDataClass` field for `[ColumnInfo]` after every compile; logs hard errors for missing annotations |

**C) Collectors** — `Collectors/`  
Each collector reads from one OVR subsystem and fills the row buffer. They run every physics tick.

| Collector | Data source |
|---|---|
| `OVRNodesCollector` | Head, hand controller positions/rotations |
| `OVREyesCollector` | Eye gaze angles, validity, confidence; combined hit point and focused object; per-eye when the recording option is enabled |
| `OVRHandsCollector` | Hand tracking, bone positions, hand confidence |
| `OVRBodyCollector` | Body joint positions and calibration state |
| `OVRFaceCollector` | 63 face expression weights and per-weight validity |
| `CustomTransformsCollector` | Position/rotation of objects registered in the Inspector |
| `SystemStatusCollector` | Recenter events, tracking origin changes, user presence, tracking loss |

**D) Metadata** — `Metadata/`

| File | Role |
|---|---|
| `AutoBuildInfo.cs` | Editor-only; runs at build time, writes `build_info.json`, stamps version fields |
| `BuildInfoLoader.cs` | Loads `build_info.json` at runtime |
| `SessionMetaWriter.cs` | Writes `session_metadata.json` once at session start |
| `CustomTablesSidecarWriter.cs` | Writes `{sessionTime}_custom_tables_sidecar.json` at session end |

### Data collection flow

```
FixedUpdate (100 Hz)
  └─ Each collector fills RowBuffer
       └─ CsvRowWriter.WriteRow → immediate disk flush (crash-safe)

OnDestroy (session end)
  └─ ValidateColumnAnnotations()       ← hard errors + ResXRLogs for annotation problems
  └─ BuildCustomTablesJson()
  └─ CustomTablesSidecarWriter.Write() ← writes sidecar JSON
  └─ All CsvRowWriters disposed
  └─ CustomCsvFromDataClass.CloseAll()
```

---

## 6. FAQ

**Q: Do I need to edit ResXRDataManager.cs or SchemaBuilder.cs?**  
A: Only `ResXRDataManager.cs`, and only to add your custom data classes and reporter functions. Never edit `SchemaBuilder.cs` or the collectors unless you have a specific research requirement.

**Q: Where is the column list for ContinuousData.csv and FaceExpressions.csv?**  
A: See `data_sources_README.txt`.

**Q: How do I add a new event table?**  
A:
1. Write a class implementing `CustomDataClass` with `onset`/`duration` properties and your public fields
2. Add `[ColumnInfo("description")]` to every public field
3. Add a `public static void Log(...)` method on the class that calls `LogCustom(new YourClass(...))`
4. Call `YourClass.Log(...)` from your experiment scripts

**Q: Will missing values appear as zeros?**  
A: No. Empty cells are left blank — blank means "no data this tick", not zero.

**Q: How often is continuous data logged?**  
A: Once per physics tick (`FixedUpdate`). The default is 100 Hz (`Fixed Timestep = 0.01 s` in Project Settings → Time). Change the timestep to adjust the sampling rate.

**Q: How often are custom event tables logged?**  
A: Whenever your reporter function is called. Custom events are not tied to the physics tick — they fire at the exact moment you call them.

**Q: What if Unity crashes — will I lose data?**  
A: No. `CsvRowWriter` flushes every row to disk immediately. Only the row actively being written at the crash moment is at risk.

**Q: Why must I call `Application.Quit()` at the end of the session?**  
A: All cleanup — flushing CSV rows and writing `{sessionTime}_custom_tables_sidecar.json` — runs in `ResXRDataManager.OnDestroy()`. This only triggers reliably when the app exits cleanly via `Application.Quit()`. If the OS kills the process instead (e.g. the user removes their headset), the sidecar JSON may be missing and the last CSV rows may not have been written. Always make `Application.Quit()` the last line of `EndSession()`.

**Q: How do I debug on device?**  
A: Use `ResXRDataManager.Instance.LogLineToFile("your message")` — it writes a timestamped row to `ResXRLogs.csv` alongside your other session files. Pull the files off the device after the session. *(adb logcat gives you raw Unity logs too — ResXRLogs is just more convenient and lives with your data.)*

**Note:** Enum and flag fields are written as strings (e.g. `"High"`, `"Calibrating"`, `"Tracked|OrientationValid"`) for readability in CSV tools.
