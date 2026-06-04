=====================================================
          DATA MANAGER V2 — README
=====================================================
What is the ResXR DataManager?
A data collection & export package that can be easily added to
any project to save relevant data to a local device.

It handles continuous logging of VR data
(head, gaze, hands, body, face expressions, etc.) 
and lets you add your own custom event tables.

You do NOT need to edit the core manager or schemas.  
Your interaction points are limited to:
- creating your own "custom data classes"
- adding custom "reporter" functions
- adding custom transforms to track in the inspector

For the full list of fields logged automatically by the template
(head/eyes/hands/body/face), see:
   data_sources_README.txt

Recording options (inspector):
- "Include Gaze" adds combined gaze columns (FocusedObject, EyeGazeHitPosition_X/Y/Z).
- "Include Separate Eyes Gaze" adds per-eye columns (Left/RightEyeGazeHitPosition,
  Left/RightFocusedObject, HasLeftEyeHit, HasRightEyeHit) and tells ResXREyeTracker
  to run 3 raycasts per frame (left, right, combined). When this option is OFF,
  only the combined ray runs (1 raycast), which can help performance in heavy scenes.
  The combined ray always runs when both eyes are confident; the recording option
  only controls per-eye raycasts and CSV columns.
=====================================================


1. HOW TO USE ResXRDataManager
-----------------------------------------------------

- **Custom data classes (events)**
  Implement `CustomDataClass` with `onset { get; }` and `duration { get; }` properties plus public
  fields for your CSV columns (see CustomCsvFromDataClass.cs). Add a constructor that sets all values.
  The CSV is named after the C# class — a class called `ChoiceEvents` produces
  `{sessionTime}_ChoiceEvents.csv`. Name your class to describe what it records; no `TableName`
  property is needed.
  Always use `Time.realtimeSinceStartup` for `onset` so it aligns with ContinuousData.

  Annotate all public fields with [ColumnInfo] for BIDS sidecar metadata.
  Signature: [ColumnInfo(description, levels..., Units = "...", Format = "...", Minimum = N, Maximum = N)]
  description is the only required argument; all others are optional.
  Missing [ColumnInfo] entirely or leaving description empty both log a hard error +
  ResXRDebugLogs entry at session end; the field name is used as a placeholder description.

      // Non-categorical (no levels):
      [ColumnInfo("Name of the chosen image")]
      public string Choice;

      // Non-categorical, with units and format:
      [ColumnInfo("Seconds from stimulus display to choice", Units = "s", Format = "number", Minimum = 0.0)]
      public float ReactionTime;

      // Categorical — one string per level, always "value:description":
      [ColumnInfo("Slot chosen by the participant", "A:Left slot", "B:Right slot")]
      public string ChosenOption;

      [ColumnInfo("Hand used to make the choice", "Left:Left hand", "Right:Right hand")]
      public string HandUsed;

      // Numeric with range bounds:
      [ColumnInfo("Confidence score from 0 to 1", Format = "number", Minimum = 0.0, Maximum = 1.0)]
      public float Confidence;

  levels are positional params (one string per level); omit entirely for non-categorical fields.
  Each level string MUST be "value:description" — value-only entries (e.g. "Left") are not
  allowed because BIDS requires a description for every level. Both the value and description
  must be non-empty. See BIDS spec:
    https://bids-specification.readthedocs.io/en/stable/common-principles.html#levels
  Units is an optional named property; omit for dimensionless or categorical columns.
  Format must be one of the 18 BIDS-allowed values (an unrecognised value logs an error at session end):
    string, number, integer, boolean, index, label, date, datetime, time, unit,
    uri, rrid, bids_uri, dataset_relative, file_relative, participant_relative,
    stimuli_relative, hed_version
  Minimum and Maximum are optional double bounds; omit if not applicable.

  At session end, C# reflection reads these attributes and writes them into
  {sessionTime}_custom_tables_sidecar.json. The Python pipeline then reads that JSON to
  generate *_events.json BIDS sidecar files.
  Undecorated fields appear in the CSV and in the sidecar with an auto-generated placeholder
  description (field name prettified, e.g. "ReactionTime" → "Reaction Time"). A hard error
  is logged at session end — add [ColumnInfo("description")] to replace the placeholder.

  Template-provided **Events** table (pipeline-friendly markers):
  - CSV: `<sessionTime>_Events.csv` with columns `name`, `onset`, `duration`.
  - Onset is expected to be `Time.realtimeSinceStartup` (seconds since app start), same clock as continuous CSVs;
    duration is in seconds (use 0 for point events). Used for downstream output standardization.
  - Log from code:
      ResXRDataManager.Instance.ReportEvent("trial_start", Time.realtimeSinceStartup, 0f);

  Example (matches the template’s ChoiceEvents shape):
      public class ChoiceEvents : CustomDataClass
      {
          public float onset    { get; }   // Time.realtimeSinceStartup at choice
          public float duration { get; }   // 0f — instantaneous event
          public string Task;
          public int Trial;
          public string OptionAName;
          public string OptionBName;
          public string Choice;
          // ... other fields ...

          public ChoiceEvents(...)
          {
              onset = Time.realtimeSinceStartup;
              duration = 0f;
              // ...
          }
      }

- **Reporter functions**
  Add a static Log() method directly on your data class (or call LogCustom(...) yourself).
  The reporter lives in the same file as the class it serves — schema and writer in one place.

  Example:
      public static void Log(string task, int trial, ...)
      {
          ResXRDataManager.Instance.LogCustom(new ChoiceEvents(task, trial, ...));
      }

  Call it from your flow scripts:
      ChoiceEvents.Log(taskName, trialIndex, ...);

- **Custom transforms**
  In the Unity inspector, assign transforms (objects) 
  you want to record positions/rotations for.
  Example: drag "Stimuli_A" into "Custom Transforms To Record" list.
  They will automatically appear as extra columns in
  ContinuousData.csv.

That’s it. ContinuousData.csv and FaceExpressions.csv 
are always recorded with the built-in schemas, you 
don’t need to edit them. For details of those columns, 
refer to data_sources_README.txt.


2. METADATA
-----------------------------------------------------

Each session is accompanied by two JSON files:

- session_metadata.json
  Written once at session start by SessionMetaWriter.cs; never modified again.
  Designed to support later Motion-BIDS export (a Python pipeline
  generates the actual BIDS files; no *_scans.tsv or *_channels.json
  are written at runtime).

  Contains:
    * session_id, utc_start_iso8601
    * device_utc_offset (real offset at session start, DST-aware)
    * device/platform info (unity_version, platform)
    * rotation_euler_order = "ZXY" (Unity Transform.eulerAngles default)
    * enabled features (eyes, face, hands, controllers)
    * schema revision, compact map of data sources
    * manufacturers_model_name_raw (SystemInfo.deviceModel)
    * software_versions_raw (SystemInfo.operatingSystem)
    * device_serial_number (empty) and device_serial_number_note
      (explains that Quest serial number cannot be read from Unity/Android 10+)
    * tracking_origin_type (Meta: EyeLevel / FloorLevel / Stage)
    * reference_frames (UnityWorld + HandLocal: RotationRule, RotationOrder,
      SpatialAxes for pipeline to generate *_channels.json later)
    * build_info_available (true if build_info.json was loaded successfully; false otherwise)
    * build_id, git_commit, utc_build_iso8601 — only set when build_info_available
      is true. When false, these are left empty (no placeholders) so the pipeline
      can treat them as "not available".

- {sessionTime}_custom_tables_sidecar.json
  Written at session end by CustomTablesSidecarWriter.cs (before CSV files are closed).
  Contains one entry per custom data class used during the session (e.g. ChoiceEvents,
  TrialsData, events), listing the CSV filename, row count, and per-column metadata
  sourced from [ColumnInfo] annotations (description, units, levels, format, min, max).
  Consumed by the ResXR Python pipeline post-experiment to auto-generate
  *_events.json BIDS sidecar files for each custom CSV. Because all custom tables
  share the same onset/duration clock as ContinuousData.csv, the pipeline can also
  merge them into a single BIDS events file.
  Any validation errors (empty descriptions, unrecognised Format values) are written
  to both Debug.LogError and ResXRDebugLogs.csv before this file is written.

Together, this guarantees reproducibility: you know 
exactly which build and session produced the data and under which 
settings.

(There is also a file called build_info.json generated at build time and embadded in the apk;
when present and loaded, its values are written into session_metadata.
When missing or not yet loaded, build_info_available is false and the
three build fields above are left empty.)


3. HOW IT WORKS (UNDER THE HOOD)
-----------------------------------------------------

The system is made of small scripts grouped by role.

A) Orchestrator
---------------
- ResXRDataManager.cs  
  The conductor. Sets up schemas, opens CSV files,
  and calls collectors every physics tick. 
  owns all the CsvRowWriters and calls them each frame.
  On quit, closes files and writes metadata.

B) Core Infrastructure
-----------------------
These are the building blocks that make CSV writing
work. You don’t normally edit them.
- SchemaBuilder.cs: defines column names for each CSV.
- ColumnIndex.cs: stores ordered column names and allows
  quick lookup by name or index.
- RowBuffer.cs: staging area for one row of data.
- CsvRowWriter.cs: writes one CSV (header + rows).
- CustomCsvFromDataClass.cs: lets you write a custom
  data class straight to CSV automatically.
- ColumnInfoAttribute.cs: optional [ColumnInfo] attribute
  for BIDS sidecar metadata on custom data class fields.
- Editor/CustomDataClassValidator.cs: [InitializeOnLoad]
  editor script; logs a hard error in the Unity console
  after every compile for any public field missing a
  [ColumnInfo] annotation. Editor-only — the equivalent
  runtime check runs in ResXRDataManager at session end.

C) Collectors
-------------
Collectors pull data from the VR system and fill rows.
- OVRNodesCollector.cs: device nodes (head, hands, etc.)
- OVREyesCollector.cs: eye gazes (angles, valid, confidence, shared time); combined focused object & hit point via ResXRPlayer; when "Include Separate Eyes Gaze" is on, per-eye hit points and focused objects from ResXRPlayer.EyeTracker
- OVRHandsCollector.cs: hand tracking, bones, confidence
- OVRBodyCollector.cs: body joints and calibration
- OVRFaceCollector.cs: face expression weights + validity
- CustomTransformsCollector.cs: positions/rotations of
  experiment-specific objects you register in inspector
- SystemStatusCollector.cs: recenter, tracking origin, user present, tracking loss

D) Metadata
-----------
- AutoBuildInfo.cs: runs in Unity Editor at build time,
  writes build_info.json, appends build id to version.
- BuildInfoLoader.cs: loads build_info.json at runtime.
- SessionMetaWriter.cs: writes session_metadata.json
  once at session start (never modified again).
- CustomTablesSidecarWriter.cs: writes
  {sessionTime}_custom_tables_sidecar.json at session end
  with per-table and per-column metadata for the pipeline.

-----------------------------------------------------

Data collection flow:
- Collectors fill a RowBuffer with values for that tick.
- RowBuffer flushes to CsvRowWriter.
- CsvRowWriter writes it to disk (CSV file).
- Metadata scripts run in parallel, writing JSONs.

So the chain is:
Collectors → RowBuffer → CsvRowWriter → CSV files


4. FAQ
-----------------------------------------------------

Q: Do I need to edit DataManager_V2 or SchemaBuilder?  
A: No. They are prebuilt. You only add custom classes
   and reporter functions.

Q: Where do I find which columns are in the CSV?  
A: See data_sources_README.txt for ContinuousData and 
   FaceExpressions. Custom tables use your class fields.

Q: How do I add a new event table?
A: Create a new class that implements CustomDataClass with onset and duration
   properties plus your public fields. The CSV file is named after the class
   automatically. Add a reporter function that instantiates it and calls
   LogCustom(...) (or CustomCsvFromDataClass.Write(...)).

Q: Will missing values appear as zeros?  
A: No. Empty cells are left blank in CSV (meaning:
   “no data this frame”).

Q: How often is data logged?  
A: ContinuousData.csv and FaceExpressions.csv are logged 
   once per physics tick (Unity’s FixedUpdate). The tick
   rate is set in Project Settings → Time → Fixed Timestep 
   (default is 0.01 seconds = 100 Hz).
   You can change this setting if you want a different
   logging frequency for continuous data.

   Custom data classes are different: they are logged
   **whenever your reporter function is called**. This means 
   you can log events at arbitrary times, regardless of the
   physics tick.

Q: What if Unity crashes—will I lose data?
A: No, CsvRowWriter flushes each line to disk so files
   stay consistent.

Q: Why must I call Application.Quit() at the end of the session?
A: All cleanup — flushing CSV rows and writing the session sidecar JSON —
   runs in ResXRDataManager.OnDestroy(). This only triggers reliably when
   the app exits cleanly via Application.Quit(). If the OS kills the
   process instead (e.g. the user removes their headset), the sidecar
   JSON may be missing and the last CSV rows may not have been written.
   Always make Application.Quit() the last line of EndSession().

Note: Enum/flag fields are written as strings (e.g., "High", "Calibrating", "Tracked|OrientationValid") for readability.

=====================================================
