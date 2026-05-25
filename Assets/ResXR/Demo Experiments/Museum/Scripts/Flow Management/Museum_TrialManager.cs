using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using ResXRData;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Museum_TrialManager : ResXRSingleton<Museum_TrialManager>
{
    private Museum_Trial _currentTrial;
    private string _currentTaskName;
    private int _currentTrialIndex;
    private float _trialStartTime;

    [ReadOnly]
    [SerializeField] private float elapsedTime = 0f;
    private bool isTrialRunning = false;

    public async UniTask RunTrialFlow(Museum_Trial trial, string taskName, int trialIndex)
    {
        _currentTrial = trial;
        _currentTaskName = taskName;
        _currentTrialIndex = trialIndex;

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

        _trialStartTime = Time.realtimeSinceStartup;
        ResXRDataManager_V2.Instance.ReportEvent(
            $"trial_start:{_currentTaskName}_t{_currentTrialIndex}",
            _trialStartTime, 0f);
    }



    private void EndTrial()
    {
        // setup trial end conditions.
        isTrialRunning = false;

        float endTime = Time.realtimeSinceStartup;
        string trialName = $"{_currentTaskName}_t{_currentTrialIndex}";

        ResXRDataManager_V2.Instance.ReportEvent(
            $"trial_end:{trialName}",
            endTime, 0f);

        ResXRDataManager_V2.Instance.LogCustom(new TrialsData(
            ResXRDataManager_V2.Instance.SessionTime,
            _currentTaskName,
            _currentTrialIndex.ToString(),
            trialName,
            _trialStartTime,
            endTime
        ));
    }

    private void Update()
    {
        if (!isTrialRunning)
            return;
        elapsedTime += Time.deltaTime;
    }

    private async UniTask RunFreeExploration()
    {
        // Free exploration: one continuous window. Gaze data is captured automatically by the data manager.
        // Single event logged at the end with the actual measured duration (not just the configured duration).
        float explorationStart = Time.realtimeSinceStartup;
        await UniTask.Delay(System.TimeSpan.FromSeconds(_currentTrial._trialDurationInSeconds));
        ResXRDataManager_V2.Instance.ReportEvent(
            $"free_exploration:{_currentTaskName}",
            explorationStart,
            Time.realtimeSinceStartup - explorationStart);
    }

    private async UniTask RunImagesRating()
    {
        // Show image and collect rating. Task name and trial index are passed through so
        // ImagesRating can write the ImageRatingRow with full context.
        await Museum_SceneReferencer.Instance.imagesRating
            .ShowNextImageAndWaitForRank(_currentTaskName, _currentTrialIndex);

        await UniTask.Delay(System.TimeSpan.FromSeconds(1));
    }

}
