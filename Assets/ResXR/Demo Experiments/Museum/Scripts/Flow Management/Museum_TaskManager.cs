using Cysharp.Threading.Tasks;
using ResXRData;
using UnityEngine;

public class Museum_TaskManager : ResXRSingleton<Museum_TaskManager>
{
    private Museum_Trial[] _trials;
    private int _currentTrial = 0;
    private Museum_Task _currentTask;
    private string _taskName;

    public async UniTask RunTaskFlow(Museum_Task task)
    {
        _currentTask = task;
        StartTask();

        if (_currentTask.taskType == Museum_TaskType.ImagesRating)
        {
            // Show specific instructions before starting image rating trials, in front of the player,
            // because they could be anywhere in the scene after free exploration.
            await PlaceInstructionsInFrontOfPlayer(Museum_SceneReferencer.Instance.endOfExplorationInstructions);
            await Museum_SceneReferencer.Instance.endOfExplorationInstructions.ShowAndWaitForConfirmation(false);

            // Move player to the designated position for the rating task, to ensure consistency.
            await Museum_SceneReferencer.Instance.ratingTaskPlayerPositionMark.WaitForPlayerAsync(false);

            await Museum_SceneReferencer.Instance.ratingInstructions.ShowAndWaitForConfirmation(false);
        }

        while (_currentTrial < _trials.Length)
        {
            await Museum_TrialManager.Instance.RunTrialFlow(_trials[_currentTrial], _taskName, _currentTrial);
            await BetweenTrialsFlow();
            _currentTrial++;
        }

        if (_currentTask.taskType == Museum_TaskType.ImagesRating)
        {
            Museum_SceneReferencer.Instance.imagesRating.gameObject.SetActive(false); // Ensure the rating component is deactivated after use.
        }
        EndTask();
    }

    private void StartTask()
    {
        // setup task initial conditions.
        _taskName = _currentTask.taskType.ToString();
        _currentTrial = 0;
        InitializeTrialsForThisTask();

        ResXRDataManager.Instance.ReportEvent($"task_start:{_taskName}", Time.realtimeSinceStartup, 0f);
    }

    private void InitializeTrialsForThisTask()
    {
        // Initialize trials based on the current task type
        switch (_currentTask.taskType)
        {
            case Museum_TaskType.FreeExploration:
                _trials = new Museum_Trial[]
                {
                    new Museum_Trial { trialType = Museum_TaskType.FreeExploration, _trialDurationInSeconds = _currentTask.durationInSeconds },
                };
                Debug.Log($"[TaskManager] Free Exploration task initialized with 1 trial of {_currentTask.durationInSeconds} seconds.");
                break;

            case Museum_TaskType.ImagesRating:
                int numOfRatingTrials = Museum_SceneReferencer.Instance.imagesRating.GetNumOfImagesToRate();

                _trials = new Museum_Trial[numOfRatingTrials];
                for (int i = 0; i < numOfRatingTrials; i++)
                {
                    _trials[i] = new Museum_Trial { trialType = Museum_TaskType.ImagesRating, _trialDurationInSeconds = 0f }; // Duration is not used for rating trials
                }
                Debug.Log($"[TaskManager] Images Rating task initialized with {numOfRatingTrials} rating trials.");
                break;

            default:
                _trials = new Museum_Trial[0];
                Debug.LogWarning("[TaskManager] Unknown task type. No trials initialized.");
                break;
        }
    }


    private void EndTask()
    {
        // setup end task conditions
        ResXRDataManager.Instance.ReportEvent($"task_end:{_taskName}", Time.realtimeSinceStartup, 0f);
    }

    private async UniTask BetweenTrialsFlow()
    {
        await UniTask.Yield();
    }

    private async UniTask PlaceInstructionsInFrontOfPlayer(InstructionsPanel panel)
    {
        PlaceInFrontOfPlayerHead placementHelper = panel.GetComponent<PlaceInFrontOfPlayerHead>();
        if (placementHelper == null)
        {
            Debug.LogWarning($"No PlaceInFrontOfPlayerHead component found on {panel.gameObject.name} instructions panel. Cannot place it in front of player.");
        }
        else
        {
            placementHelper.RepositionNow();
            Debug.Log($"[PlaceInFrontOfPlayerHead]Caller immediately after: {panel.transform.position}");
            await UniTask.Yield();
            Debug.Log($"[PlaceInFrontOfPlayerHead]Caller after 1 frame: {panel.transform.position}");
        }
    }
}
