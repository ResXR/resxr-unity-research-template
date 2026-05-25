using Cysharp.Threading.Tasks;
using ResXRData;
using UnityEngine;

public class BinaryChoice_TrialManager : ResXRSingleton<BinaryChoice_TrialManager>
{
    private BinaryChoice_Trial _currentTrial;
    private ChoicesManager _choicesManager;
    private string _currentTaskName;
    private int _currentTrialIndex;
    private float _trialStartTime;

    public async UniTask RunTrialFlow(BinaryChoice_Trial trial, string taskName, int trialIndex)
    {
        _currentTrial = trial;
        _currentTaskName = taskName;
        _currentTrialIndex = trialIndex;

        StartTrial();

        ChoiceResult result = await _choicesManager.SetImagesAndWaitForChoice(_currentTrial.StimuliPair);

        // Stimulus window: single event with duration = time from display to choice
        ResXRDataManager_V2.Instance.ReportEvent(
            $"stimulus:{_currentTaskName}_t{_currentTrialIndex}",
            result.displayTime,
            result.reactionTime);

        // Choice made: point event
        ResXRDataManager_V2.Instance.ReportEvent(
            $"choice_made:option={result.chosenOption}:image={result.chosenImageName}",
            result.choiceTime, 0f);

        ResXRDataManager_V2.Instance.LogChoice(
            _currentTaskName,
            _currentTrialIndex,
            _currentTrial.StimuliPair.stimulusASprite.name,
            _currentTrial.StimuliPair.stimulusBSprite.name,
            result.chosenImageName,
            result.chosenOption,
            result.handUsed,
            result.reactionTime,
            result.displayTime,
            result.choiceTime
        );

        EndTrial();
    }

    private void StartTrial()
    {
        if (_currentTrial == null || _currentTrial.StimuliPair == null)
        {
            Debug.LogError("[TrialManager] StartTrial: currentTrial is null or wasn't initialized with a stimuli pair. Cannot start trial.");
            return;
        }

        // initialize variables
        _choicesManager = BinaryChoice_SceneReferencer.Instance.choicesManager;

        _trialStartTime = Time.realtimeSinceStartup;
        ResXRDataManager_V2.Instance.ReportEvent(
            $"trial_start:{_currentTaskName}_t{_currentTrialIndex}",
            _trialStartTime, 0f);

        Debug.Log("[TrialManager] Trial Started with Stimuli Pair: " +
                   $"{_currentTrial.StimuliPair.stimulusASprite.name} and " +
                   $"{_currentTrial.StimuliPair.stimulusBSprite.name}");
    }


    private void EndTrial()
    {
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

        Debug.Log("[TrialManager] Trial Ended");
    }

}
