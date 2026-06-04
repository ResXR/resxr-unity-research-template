// BinaryChoiceDataClasses.cs
// Custom data tables specific to the Binary Choice demo.
// Each class defines one CSV file; its static Log() method is the reporter —
// call it from your flow scripts instead of calling LogCustom() directly.
//
// ── For new developers ────────────────────────────────────────────────────────
// PATTERN: ClassName.Log(...) writes one row to that class's CSV.
// Pass plain values; the method constructs the data object and handles the rest.
// Example:
//   ChoiceEvents.Log(task, trial, optionA, optionB, choice, slot, hand, rt, t0, t1);
//
// To add a new table:
//   1. Add a class implementing CustomDataClass (onset, duration, public fields)
//   2. Annotate every public field with [ColumnInfo("description")]
//   3. Add a static Log() method at the bottom of the class
//   4. Call ClassName.Log(...) from your flow script
//
// For the full explanation of how data classes and reporters work, see
// ResXRDataManager_README.md or the "custom data classes" region in
// ResXRDataManager.cs.
// ─────────────────────────────────────────────────────────────────────────────

using ResXRData;
using UnityEngine;

namespace ResXRData
{
    /// <summary>
    /// One row per trial. Records which stimuli were shown, which was chosen, and timing.
    /// Written by <c>BinaryChoice_TrialManager</c> at the moment the participant's choice
    /// is registered.
    /// Call <c>ChoiceEvents.Log(...)</c> from your flow scripts to write a row.
    /// </summary>
    public class ChoiceEvents : CustomDataClass
    {
        public float onset    { get; }   // Time.realtimeSinceStartup when the choice was made
        public float duration { get; }   // 0f — instantaneous event

        [ColumnInfo("Task name or index within the session", Format = "string")]
        public string Task;
        [ColumnInfo("Trial index within the task", Format = "integer")]
        public int Trial;
        [ColumnInfo("Name of the image sprite shown in slot A")]
        public string OptionAName;
        [ColumnInfo("Name of the image sprite shown in slot B")]
        public string OptionBName;
        [ColumnInfo("Name of the chosen image")]
        public string Choice;
        [ColumnInfo("Slot chosen by the participant", "A:Left slot", "B:Right slot", Format = "string")]
        public string ChosenOption;
        [ColumnInfo("Hand used to touch the chosen stimulus", "Left:Left hand", "Right:Right hand", Format = "string")]
        public string HandUsed;
        [ColumnInfo("Seconds from stimulus display to touch", Units = "s", Format = "number", Minimum = 0.0)]
        public float ReactionTime;
        [ColumnInfo("Seconds since application start when the stimuli appeared on screen", Units = "s", Format = "number", Minimum = 0.0)]
        public float displayTime;
        [ColumnInfo("Seconds since application start when the choice touch was registered", Units = "s", Format = "number", Minimum = 0.0)]
        public float ChoiceTime;

        public ChoiceEvents(string task, int trial, string optionAName, string optionBName, string choice,
            string chosenOption, string handUsed, float reactionTime, float displayTime, float choiceTime)
        {
            this.onset = Time.realtimeSinceStartup;
            this.duration = 0f;
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

        /// <summary>
        /// Writes one ChoiceEvents row. Call immediately after the participant makes a choice.
        /// </summary>
        public static void Log(string task, int trial, string optionAName, string optionBName,
            string choice, string chosenOption, string handUsed,
            float reactionTime, float displayTime, float choiceTime)
        {
            ResXRDataManager.Instance.LogCustom(
                new ChoiceEvents(task, trial, optionAName, optionBName, choice,
                                 chosenOption, handUsed, reactionTime, displayTime, choiceTime));
        }
    }

    /// <summary>
    /// World-space bounding boxes of the two choice slots (A and B).
    /// Written once at session start by <c>ChoicesManager.RecordStimulusBounds()</c>.
    /// Use these to resolve whether and where the participant's gaze landed on
    /// each stimulus from the continuous gaze data in ContinuousData.csv.
    /// Call <c>StimulusBounds.Log(...)</c> to write a row (via the ChoicesManager wrapper).
    /// </summary>
    public class StimulusBounds : CustomDataClass
    {
        public float onset    { get; }   // Time.realtimeSinceStartup when bounds were logged
        public float duration { get; }   // 0f — configuration snapshot, not a timed event

        [ColumnInfo("Slot identifier matching ChosenOption in ChoiceEvents", "A:Left slot", "B:Right slot", Format = "string")]
        public string ChoiceId;
        // Renderer bounds (visual area of the stimulus quad)
        [ColumnInfo("World-space X component of the renderer bounding box centre", Units = "m", Format = "number")]
        public float RendererCenterX;
        [ColumnInfo("World-space Y component of the renderer bounding box centre", Units = "m", Format = "number")]
        public float RendererCenterY;
        [ColumnInfo("World-space Z component of the renderer bounding box centre", Units = "m", Format = "number")]
        public float RendererCenterZ;
        [ColumnInfo("World-space X extent of the renderer bounding box", Units = "m", Format = "number", Minimum = 0.0)]
        public float RendererSizeX;
        [ColumnInfo("World-space Y extent of the renderer bounding box", Units = "m", Format = "number", Minimum = 0.0)]
        public float RendererSizeY;
        [ColumnInfo("World-space Z extent of the renderer bounding box", Units = "m", Format = "number", Minimum = 0.0)]
        public float RendererSizeZ;
        // Collider bounds (interaction volume — used by the touch detection)
        [ColumnInfo("World-space X component of the collider bounding box centre", Units = "m", Format = "number")]
        public float ColliderCenterX;
        [ColumnInfo("World-space Y component of the collider bounding box centre", Units = "m", Format = "number")]
        public float ColliderCenterY;
        [ColumnInfo("World-space Z component of the collider bounding box centre", Units = "m", Format = "number")]
        public float ColliderCenterZ;
        [ColumnInfo("World-space X extent of the collider bounding box", Units = "m", Format = "number", Minimum = 0.0)]
        public float ColliderSizeX;
        [ColumnInfo("World-space Y extent of the collider bounding box", Units = "m", Format = "number", Minimum = 0.0)]
        public float ColliderSizeY;
        [ColumnInfo("World-space Z extent of the collider bounding box", Units = "m", Format = "number", Minimum = 0.0)]
        public float ColliderSizeZ;

        public StimulusBounds(float timeSinceStart, string choiceId,
            float rendererCenterX, float rendererCenterY, float rendererCenterZ,
            float rendererSizeX, float rendererSizeY, float rendererSizeZ,
            float colliderCenterX, float colliderCenterY, float colliderCenterZ,
            float colliderSizeX, float colliderSizeY, float colliderSizeZ)
        {
            this.onset = timeSinceStart;
            this.duration = 0f;
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

        /// <summary>
        /// Writes one StimulusBounds row. Called once per slot at session start
        /// via <c>ChoicesManager.RecordStimulusBounds()</c>.
        /// </summary>
        public static void Log(float timeSinceStart, string choiceId,
            float rendererCenterX, float rendererCenterY, float rendererCenterZ,
            float rendererSizeX, float rendererSizeY, float rendererSizeZ,
            float colliderCenterX, float colliderCenterY, float colliderCenterZ,
            float colliderSizeX, float colliderSizeY, float colliderSizeZ)
        {
            ResXRDataManager.Instance.LogCustom(
                new StimulusBounds(timeSinceStart, choiceId,
                    rendererCenterX, rendererCenterY, rendererCenterZ,
                    rendererSizeX, rendererSizeY, rendererSizeZ,
                    colliderCenterX, colliderCenterY, colliderCenterZ,
                    colliderSizeX, colliderSizeY, colliderSizeZ));
        }
    }
}
