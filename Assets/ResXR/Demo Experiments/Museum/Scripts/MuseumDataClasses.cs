// MuseumDataClasses.cs
// Custom data tables specific to the Museum demo.
// Each class defines one CSV file; its static Log() method is the reporter —
// call it from your flow scripts instead of calling LogCustom() directly.
//
// ── For new developers ────────────────────────────────────────────────────────
// PATTERN: ClassName.Log(...) writes one row to that class's CSV.
// Pass plain values; the method constructs the data object and handles the rest.
// Example:
//   ImageRatings.Log(task, trial, imageName, rawRating, min, max, start, confirm);
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
    /// One row per rated image. Written immediately after the participant confirms their rating.
    /// NormalizedRating is (RawRating - Min) / (Max - Min), i.e. 0–1.
    /// Call <c>ImageRatings.Log(...)</c> to write a row (called inside ImagesRating component).
    /// </summary>
    public class ImageRatings : CustomDataClass
    {
        public float onset    { get; }   // Time.realtimeSinceStartup when the image appeared (presentation start)
        public float duration { get; }   // Seconds from image appearance to confirm (deliberation time)

        [ColumnInfo("Task name or index within the session", Format = "string")]
        public string Task;
        [ColumnInfo("Trial index within the task", Format = "integer")]
        public int Trial;
        [ColumnInfo("Name of the image that was rated")]
        public string ImageName;
        [ColumnInfo("Slider value as selected by the participant; see SliderConfig table for scale boundaries", Format = "number")]
        public float RawRating;
        [ColumnInfo("Slider value normalised to 0-1 range: (raw - min) / (max - min)", Format = "number", Minimum = 0.0, Maximum = 1.0)]
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

        /// <summary>
        /// Writes one ImageRatings row. Called inside <c>ImagesRating.ShowNextImageAndWaitForRank()</c>
        /// after the participant confirms their rating.
        /// </summary>
        public static void Log(string task, int trial, string imageName,
            float rawRating, float minValue, float maxValue,
            float presentationStart, float confirmTime)
        {
            ResXRDataManager.Instance.LogCustom(
                new ImageRatings(task, trial, imageName, rawRating, minValue, maxValue,
                                 presentationStart, confirmTime));
        }
    }

    /// <summary>
    /// Written once per session (at session start) before any trials run.
    /// Records the slider configuration so per-image rows stay compact.
    /// Call <c>SliderConfig.Log(...)</c> to write a row (via <c>ImagesRating.LogSliderConfig()</c>).
    /// </summary>
    public class SliderConfig : CustomDataClass
    {
        public float onset    { get; }   // Time.realtimeSinceStartup when config was logged
        public float duration { get; }   // 0f — configuration snapshot, not a timed event

        [ColumnInfo("Minimum value of the rating slider", Format = "number")]
        public float MinValue;
        [ColumnInfo("Maximum value of the rating slider", Format = "number")]
        public float MaxValue;
        [ColumnInfo("Number of discrete steps on the slider; 0 means continuous", Format = "integer", Minimum = 0.0)]
        public int NumOfIntervals;
        [ColumnInfo("Whether the slider allows non-integer (continuous) positions", Format = "boolean")]
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

        /// <summary>
        /// Writes one SliderConfig row. Call once at session start via
        /// <c>ImagesRating.LogSliderConfig()</c>.
        /// </summary>
        public static void Log(float minValue, float maxValue, int numOfIntervals, bool allowContinuous)
        {
            ResXRDataManager.Instance.LogCustom(
                new SliderConfig(minValue, maxValue, numOfIntervals, allowContinuous));
        }
    }

    /// <summary>
    /// One row per artwork, written once at session start.
    /// World-space renderer bounds + artwork orientation let the analysis pipeline
    /// compute where on the artwork canvas (in UV / pixel space) the gaze hit point landed.
    /// Call <c>ArtworkBounds.Log(...)</c> to write a row (via <c>Museum_SessionManager.RecordArtworkBounds()</c>).
    /// </summary>
    public class ArtworkBounds : CustomDataClass
    {
        public float onset    { get; }   // Time.realtimeSinceStartup when bounds were logged
        public float duration { get; }   // 0f — configuration snapshot, not a timed event

        [ColumnInfo("Name of the artwork as displayed in the scene", Format = "string")]
        public string ArtworkName;
        // Renderer world-space bounds (visual area)
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
        // Artwork orientation (ZXY Euler, degrees — matches Unity Transform convention)
        [ColumnInfo("ZXY Euler rotation of the artwork around the X axis", Units = "deg", Format = "number", Minimum = 0.0, Maximum = 360.0)]
        public float RotationEulerX;
        [ColumnInfo("ZXY Euler rotation of the artwork around the Y axis", Units = "deg", Format = "number", Minimum = 0.0, Maximum = 360.0)]
        public float RotationEulerY;
        [ColumnInfo("ZXY Euler rotation of the artwork around the Z axis", Units = "deg", Format = "number", Minimum = 0.0, Maximum = 360.0)]
        public float RotationEulerZ;
        // Collider world-space bounds (interaction area — on a dedicated collider GameObject)
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

        /// <summary>
        /// Writes one ArtworkBounds row. Called once per artwork at session start
        /// via <c>Museum_SessionManager.RecordArtworkBounds()</c>.
        /// </summary>
        public static void Log(float timeSinceStart, Renderer artwork, Collider col)
        {
            ResXRDataManager.Instance.LogCustom(new ArtworkBounds(timeSinceStart, artwork, col));
        }
    }
}
