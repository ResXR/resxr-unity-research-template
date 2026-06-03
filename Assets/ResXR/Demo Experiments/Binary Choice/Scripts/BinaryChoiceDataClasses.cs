// BinaryChoiceDataClasses.cs
// Data tables specific to the Binary Choice demo experiment.
//
// ─────────────────────────────────────────────────────────────────────────────
// WHERE TO PUT YOUR DATA CLASSES
// ─────────────────────────────────────────────────────────────────────────────
//
// For how data classes work, see the "custom data classes" region in
// ResXRDataManager.cs — it has the full explanation and examples.
//
// Option A — Dedicated file like this one.
//            Recommended when your experiment has multiple tables, or when
//            the same table is used from several scripts.
//            Keep classes in the ResXRData namespace so any script can reach
//            them with a single "using ResXRData;" line.
//
// Option B — The "custom data classes" region inside ResXRDataManager.cs.
//            Good for tables that are truly universal across experiments
//            (like TrialsData and events, which are already there).
//
// The demo experiments use dedicated files (Option A) so definitions are easy
// to find and not buried inside flow managers.
// ─────────────────────────────────────────────────────────────────────────────

using ResXRData;
using UnityEngine;

namespace ResXRData
{
    /// <summary>
    /// One row per trial. Written by BinaryChoice_TrialManager.EndTrial() after LogChoice().
    /// Records which stimuli were shown, which was chosen, and timing information.
    /// The convenience method ResXRDataManager.Instance.LogChoice(...) creates this row for you.
    /// </summary>
    public class ChoiceEvents : CustomDataClass
    {
        public float onset    { get; }   // Time.realtimeSinceStartup at the moment the choice was made
        public float duration { get; }   // 0f — instantaneous event

        [ColumnInfo("Task name or index")]
        public string Task;
        [ColumnInfo("Trial index within the task", Format = "integer")]
        public int Trial;
        [ColumnInfo("Name of the image sprite shown in slot A")]
        public string OptionAName;    // Name of the sprite shown on slot A
        [ColumnInfo("Name of the image sprite shown in slot B")]
        public string OptionBName;    // Name of the sprite shown on slot B
        [ColumnInfo("Name of the chosen image")]
        public string Choice;         // Name of the chosen image
        [ColumnInfo("Slot chosen by the participant", "A:Left slot", "B:Right slot")]
        public string ChosenOption;   // "A" or "B"
        [ColumnInfo("Hand used to touch the chosen stimulus", "Left", "Right")]
        public string HandUsed;       // Which hand touched the stimulus
        [ColumnInfo("Seconds from stimulus display to touch", Units = "s", Format = "number", Minimum = 0.0)]
        public float ReactionTime;    // Seconds from stimulus display to choice
        [ColumnInfo("Time.realtimeSinceStartup when the stimuli appeared on screen", Units = "s", Format = "number", Minimum = 0.0)]
        public float displayTime;     // Time.realtimeSinceStartup when stimuli appeared
        [ColumnInfo("Time.realtimeSinceStartup when the choice touch was registered", Units = "s", Format = "number", Minimum = 0.0)]
        public float ChoiceTime;      // Time.realtimeSinceStartup when choice was registered

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
    }

    /// <summary>
    /// World-space bounding boxes of the two choice slots (A and B).
    /// Written ONCE at session start by ChoicesManager.LogStimulusBoundsOnce().
    /// Use these to resolve whether and where the participant's gaze landed on
    /// each stimulus from the continuous gaze data in ContinuousData.csv.
    /// </summary>
    public class StimulusBounds : CustomDataClass
    {
        public float onset    { get; }   // Time.realtimeSinceStartup when bounds were logged
        public float duration { get; }   // 0f — configuration snapshot, not a timed event

        [ColumnInfo("Slot identifier matching ChosenOption in ChoiceEvents", "A:Left slot", "B:Right slot")]
        public string ChoiceId;          // "A" or "B" — matches ChosenOption in ChoiceEvents
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
    }
}
