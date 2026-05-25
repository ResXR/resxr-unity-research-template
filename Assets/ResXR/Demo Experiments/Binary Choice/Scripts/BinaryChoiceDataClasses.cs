// BinaryChoiceDataClasses.cs
// Data tables specific to the Binary Choice demo experiment.
//
// ─────────────────────────────────────────────────────────────────────────────
// WHERE TO PUT YOUR DATA CLASSES
// ─────────────────────────────────────────────────────────────────────────────
//
// For how data classes work, see the "custom data classes" region in
// ResXRDataManager_V2.cs — it has the full explanation and examples.
//
// Option A — Dedicated file like this one.
//            Recommended when your experiment has multiple tables, or when
//            the same table is used from several scripts.
//            Keep classes in the ResXRData namespace so any script can reach
//            them with a single "using ResXRData;" line.
//
// Option B — The "custom data classes" region inside ResXRDataManager_V2.cs.
//            Good for tables that are truly universal across experiments
//            (like TrialsData and ReportEvent, which are already there).
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
    /// The convenience method ResXRDataManager_V2.Instance.LogChoice(...) creates this row for you.
    /// </summary>
    public class ChoiceEvent : CustomDataClass
    {
        public string TableName => "ChoiceEvents";

        public float TimeSinceStart;  // Time.realtimeSinceStartup at the moment the choice was made
        public string Task;
        public int Trial;
        public string OptionAName;    // Name of the sprite shown on slot A
        public string OptionBName;    // Name of the sprite shown on slot B
        public string Choice;         // Name of the chosen image
        public string ChosenOption;   // "A" or "B"
        public string HandUsed;       // Which hand touched the stimulus
        public float ReactionTime;    // Seconds from stimulus display to choice
        public float displayTime;     // Time.realtimeSinceStartup when stimuli appeared
        public float ChoiceTime;      // Time.realtimeSinceStartup when choice was registered

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

    /// <summary>
    /// World-space bounding boxes of the two choice slots (A and B).
    /// Written ONCE at session start by ChoicesManager.LogStimulusBoundsOnce().
    /// Use these to resolve whether and where the participant's gaze landed on
    /// each stimulus from the continuous gaze data in ContinuousData.csv.
    /// </summary>
    public class StimulusBounds : CustomDataClass
    {
        public string TableName => "StimulusBounds";

        public float TimeSinceStart;
        public string ChoiceId;          // "A" or "B" — matches ChosenOption in ChoiceEvents
        // Renderer bounds (visual area of the stimulus quad)
        public float RendererCenterX;
        public float RendererCenterY;
        public float RendererCenterZ;
        public float RendererSizeX;
        public float RendererSizeY;
        public float RendererSizeZ;
        // Collider bounds (interaction volume — used by the touch detection)
        public float ColliderCenterX;
        public float ColliderCenterY;
        public float ColliderCenterZ;
        public float ColliderSizeX;
        public float ColliderSizeY;
        public float ColliderSizeZ;

        public StimulusBounds(float timeSinceStart, string choiceId,
            float rendererCenterX, float rendererCenterY, float rendererCenterZ,
            float rendererSizeX, float rendererSizeY, float rendererSizeZ,
            float colliderCenterX, float colliderCenterY, float colliderCenterZ,
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
}
