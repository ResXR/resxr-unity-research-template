using Cysharp.Threading.Tasks;
using ResXRData;
using UnityEngine;

public class BinaryChoice_TrialManager : ResXRSingleton<BinaryChoice_TrialManager>
{
    private BinaryChoice_Trial _currentTrial;
    private ChoicesManager _choicesManager;
    private string _currentTaskName;
    private int _currentTrialIndex;

    public async UniTask RunTrialFlow(BinaryChoice_Trial trial, string taskName, int trialIndex)
    {
        _currentTrial = trial;
        _currentTaskName = taskName;
        _currentTrialIndex = trialIndex;

        StartTrial();

        ChoiceResult result = await _choicesManager.SetImagesAndWaitForChoice(_currentTrial.StimuliPair);

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

        Debug.Log("[TrialManager] Trial Started with Stimuli Pair: " +
                   $"{_currentTrial.StimuliPair.stimulusASprite.name} and " +
                   $"{_currentTrial.StimuliPair.stimulusBSprite.name}");
    }


    private void EndTrial()
    {
        Debug.Log("[TrialManager] Trial Ended");
    }

}
