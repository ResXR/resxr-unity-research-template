using Cysharp.Threading.Tasks;
using ResXRData;
using Unity.VisualScripting;
using UnityEngine;

public class ImagesRating : MonoBehaviour
{
    [SerializeField] private InstructionsPanel ratingPanel;
    [SerializeField] private Slider ratingSlider;
    [SerializeField] SpriteRenderer[] imagesToRate;
    private int currentImageIndex = 0;

    private void Awake()
    {
        foreach (SpriteRenderer image in imagesToRate)
        {
            image.GameObject().SetActive(false);
        }
    }

    /// <summary>
    /// Writes one SliderConfig row to CSV. Call once at session start, before any rating trials.
    /// </summary>
    public void LogSliderConfig()
    {
        ResXRDataManager_V2.Instance.LogCustom(new SliderConfigRow(
            ratingSlider.MinValue,
            ratingSlider.MaxValue,
            ratingSlider.NumOfIntervals,
            ratingSlider.AllowContinuousValues
        ));
    }

    /// <summary>
    /// Shows the next image, waits for the participant to confirm a rating, logs the result, and returns.
    /// Returns (imageName, rating) so the caller can use the values if needed.
    /// </summary>
    public async UniTask<(string, float)> ShowNextImageAndWaitForRank(string taskName = "", int trialIndex = 0)
    {
        if (currentImageIndex >= imagesToRate.Length)
        {
            Debug.LogWarning("[ImagesRating] No more images to rate.");
            return (string.Empty, 0f);
        }

        // Show rating panel, image and wait for user input
        ratingSlider.ResetValue();
        ratingPanel.Show(false).Forget();
        SpriteRenderer currentImage = imagesToRate[currentImageIndex];
        currentImage.GameObject().SetActive(true);
        ratingSlider.gameObject.SetActive(true);

        float presentationStart = Time.realtimeSinceStartup;

        float rating = await ratingSlider.WaitForConfirm();
        float confirmTime = Time.realtimeSinceStartup;

        string imageName = currentImage.sprite.name;

        // hide image and rating panel and slider
        currentImage.GameObject().SetActive(false);
        ratingPanel.Hide().Forget();
        ratingSlider.gameObject.SetActive(false);

        // image_displayed: single event with duration = deliberation time
        ResXRDataManager_V2.Instance.ReportEvent(
            $"image_displayed:{imageName}",
            presentationStart,
            confirmTime - presentationStart);

        // Per-image rating row
        ResXRDataManager_V2.Instance.LogCustom(new ImageRatingRow(
            taskName,
            trialIndex,
            imageName,
            rating,
            ratingSlider.MinValue,
            ratingSlider.MaxValue,
            presentationStart,
            confirmTime
        ));

        currentImageIndex++;
        return (imageName, rating);
    }

    // Run through all images and collect ratings. we're actually running them one by one in the trial manager.
    public async UniTask RunRatingImages()
    {
        currentImageIndex = 0;
        while (currentImageIndex < imagesToRate.Length)
        {
            (string imageName, float rating) = await ShowNextImageAndWaitForRank();
            Debug.Log($"[ImagesRating] Image: {imageName}, Rating: {rating}");
        }
        Debug.Log("[ImagesRating] All images rated.");
    }

    public int GetNumOfImagesToRate()
    {
        return imagesToRate.Length;
    }
}
