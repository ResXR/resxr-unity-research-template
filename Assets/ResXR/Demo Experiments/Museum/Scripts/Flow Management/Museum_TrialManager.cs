using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Museum_TrialManager : ResXRSingleton<Museum_TrialManager>
{
    private Museum_Trial _currentTrial;
    [ReadOnly]
    [SerializeField] private float elapsedTime = 0f;
    private bool isTrialRunning = false;

    public async UniTask RunTrialFlow(Museum_Trial trial)
    {
        _currentTrial = trial;
        StartTrial();

        // Run trial based on its type.
        switch (_currentTrial.trialType)
        {
            case Museum_TaskType.FreeExploration:
                await RunFreeExploration();
                break;

            case Museum_TaskType.ImagesRating:
                await RunImagesRating();
                break;

            default:
                Debug.LogWarning("[TrialManager] Unknown trial type. Skipping trial.");
                break;
        }


        EndTrial();
    }

    private void StartTrial()
    {
        // setup trial initial conditions.
        elapsedTime = 0f;
        isTrialRunning = true;
    }



    private void EndTrial()
    {
        // setup trial end conditions.
        isTrialRunning = false;
    }

    private void Update()
    {
        if (!isTrialRunning)
            return;
        elapsedTime += Time.deltaTime;
    }

    private async UniTask RunFreeExploration()
    {
        // free exploration is basically one continuous trial, so we just wait for the trial duration here.
        // eye gaze data is automatically recorded by the ResXR data manager during this time.
        await UniTask.Delay(System.TimeSpan.FromSeconds(_currentTrial._trialDurationInSeconds));

    }

    private async UniTask RunImagesRating()
    {
        // Show images one by one and collect ratings.
        await Museum_SceneReferencer.Instance.imagesRating.ShowNextImageAndWaitForRank();
        // delay one second
        await UniTask.Delay(System.TimeSpan.FromSeconds(1));
    }

}
