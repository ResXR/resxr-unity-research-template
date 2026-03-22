using Cysharp.Threading.Tasks;
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

    public async UniTask<(string,float)> ShowNextImageAndWaitForRank()
    {
        if (currentImageIndex >= imagesToRate.Length)
        {
            Debug.LogWarning("[ImagesRating] No more images to rate.");
            return (string.Empty, 0f);
        }


        // Show rating panel, image and wait for user input
        ratingPanel.Show(false).Forget();
        SpriteRenderer currentImage = imagesToRate[currentImageIndex];
        currentImage.GameObject().SetActive(true);
        ratingSlider.gameObject.SetActive(true);

        float rating = await ratingSlider.WaitForConfirm();
        string imageName = currentImage.sprite.name;

        // hide image and rating panel and slider
        currentImage.GameObject().SetActive(false);
        ratingPanel.Hide().Forget();
        ratingSlider.ResetValue();
        ratingSlider.gameObject.SetActive(false);

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
            // Here you can store the rating as needed
        }
        Debug.Log("[ImagesRating] All images rated.");
    }

    public int GetNumOfImagesToRate()
    {
        return imagesToRate.Length;
    }


}
