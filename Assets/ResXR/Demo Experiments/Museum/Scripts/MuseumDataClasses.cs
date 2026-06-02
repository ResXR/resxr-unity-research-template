// MuseumDataClasses.cs
// Custom data tables specific to the Museum demo.
//
// ── For new developers ────────────────────────────────────────────────────────
// Each class here becomes one CSV file named after the class — e.g. ImageRatings
// produces {sessionTime}_ImageRatings.csv. Each public field = one column.
// Every row automatically starts with onset and duration columns (required by the
// CustomDataClass interface) before any of your own fields.
// To add a new table, add a new class that implements CustomDataClass.
// To log data, add a reporter method to ResXRDataManager (see LogChoice or
// LogLineToFile there for examples), then call it from your flow scripts.
// For the full explanation of how data classes work, see the
// "custom data classes" region at the top of ResXRDataManager.cs.
// ─────────────────────────────────────────────────────────────────────────────

using ResXRData;
using UnityEngine;

namespace ResXRData
{
    /// <summary>
    /// One row per rated image. Written immediately after the participant confirms their rating.
    /// NormalizedRating is (RawRating - Min) / (Max - Min), i.e. 0–1.
    /// </summary>
    public class ImageRatings : CustomDataClass
    {
        public float onset    { get; }   // Time.realtimeSinceStartup when the image appeared (presentation start)
        public float duration { get; }   // Seconds from image appearance to confirm (deliberation time)

        public string Task;
        public int Trial;
        public string ImageName;
        public float RawRating;
        public float NormalizedRating; // (raw - min) / (max - min)

        public ImageRatings(string task, int trial, string imageName,
            float rawRating, float minValue, float maxValue,
            float presentationStart, float confirmTime)
        {
            onset = presentationStart;
            duration = confirmTime - presentationStart;
            Task = task;
            Trial = trial;
            ImageName = imageName;
            RawRating = rawRating;
            float range = maxValue - minValue;
            NormalizedRating = range > 1e-6f ? (rawRating - minValue) / range : 0f;
        }
    }

    /// <summary>
    /// Written once per session (at session start) before any trials run.
    /// Records the slider configuration so per-image rows stay compact.
    /// </summary>
    public class SliderConfig : CustomDataClass
    {
        public float onset    { get; }   // Time.realtimeSinceStartup when config was logged
        public float duration { get; }   // 0f — configuration snapshot, not a timed event

        public float MinValue;
        public float MaxValue;
        public int NumOfIntervals;
        public bool AllowContinuousValues;

        public SliderConfig(float minValue, float maxValue, int numOfIntervals, bool allowContinuous)
        {
            onset = Time.realtimeSinceStartup;
            duration = 0f;
            MinValue = minValue;
            MaxValue = maxValue;
            NumOfIntervals = numOfIntervals;
            AllowContinuousValues = allowContinuous;
        }
    }

    /// <summary>
    /// One row per artwork, written once at session start.
    /// World-space renderer bounds + artwork orientation let the analysis pipeline
    /// compute where on the artwork canvas (in UV / pixel space) the gaze hit point landed.
    /// </summary>
    public class ArtworkBounds : CustomDataClass
    {
        public float onset    { get; }   // Time.realtimeSinceStartup when bounds were logged
        public float duration { get; }   // 0f — configuration snapshot, not a timed event

        public string ArtworkName;
        // Renderer world-space bounds (visual area)
        public float RendererCenterX;
        public float RendererCenterY;
        public float RendererCenterZ;
        public float RendererSizeX;
        public float RendererSizeY;
        public float RendererSizeZ;
        // Artwork orientation (ZXY Euler, degrees — matches Unity Transform convention)
        public float RotationEulerX;
        public float RotationEulerY;
        public float RotationEulerZ;
        // Collider world-space bounds (interaction area — on a dedicated collider GameObject)
        public float ColliderCenterX;
        public float ColliderCenterY;
        public float ColliderCenterZ;
        public float ColliderSizeX;
        public float ColliderSizeY;
        public float ColliderSizeZ;

        public ArtworkBounds(float timeSinceStart, Renderer artwork, Collider col)
        {
            onset = timeSinceStart;
            duration = 0f;
            ArtworkName = artwork.gameObject.name;

            var rb = artwork.bounds;
            RendererCenterX = rb.center.x;
            RendererCenterY = rb.center.y;
            RendererCenterZ = rb.center.z;
            RendererSizeX = rb.size.x;
            RendererSizeY = rb.size.y;
            RendererSizeZ = rb.size.z;

            var euler = artwork.transform.eulerAngles;
            RotationEulerX = euler.x;
            RotationEulerY = euler.y;
            RotationEulerZ = euler.z;

            var cb = col.bounds;
            ColliderCenterX = cb.center.x;
            ColliderCenterY = cb.center.y;
            ColliderCenterZ = cb.center.z;
            ColliderSizeX = cb.size.x;
            ColliderSizeY = cb.size.y;
            ColliderSizeZ = cb.size.z;
        }
    }
}
