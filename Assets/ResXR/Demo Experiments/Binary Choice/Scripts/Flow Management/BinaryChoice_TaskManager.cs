using Cysharp.Threading.Tasks;
using ResXRData;
using System.Collections.Generic;
using UnityEngine;


public class BinaryChoice_TaskManager : ResXRSingleton<BinaryChoice_TaskManager>
{
    [SerializeField] private BinaryChoice_Trial[] _trials;
    private int _currentTrial = 0;
    private BinaryChoice_Task _currentTask;

    private StimuliPairsLoader _stimuliPairsLoader;
    private float _timeBetweenStimuli;
    private float _instructionsDisplayTime;

    public async UniTask RunTaskFlow(BinaryChoice_Task task)
    {
        _currentTask = task;
        StartTask();

        float instructionsStart = Time.realtimeSinceStartup;
        await _currentTask.taskInstructions.ShowForSeconds(_instructionsDisplayTime);
        ResXRDataManager_V2.Instance.ReportEvent(
            $"task_instructions:{_currentTask.taskName}",
            instructionsStart,
            Time.realtimeSinceStartup - instructionsStart);

        while (_currentTrial < _trials.Length)
        {
            await BinaryChoice_TrialManager.Instance.RunTrialFlow(_trials[_currentTrial], _currentTask.taskName, _currentTrial);
            await BetweenTrialsFlow();
            _currentTrial++;
        }

        EndTask();
    }

    private void StartTask()
    {
        // initialize variables
        _stimuliPairsLoader = new StimuliPairsLoader(_currentTask.stimuliFolderPath, _currentTask.stimuliOrder);
        _timeBetweenStimuli = BinaryChoice_SceneReferencer.Instance.SecondsBetweenStimuli;
        _instructionsDisplayTime = BinaryChoice_SceneReferencer.Instance.instructionsDisplayTime;

        _currentTrial = 0;
        CreateTrials();

        ResXRDataManager_V2.Instance.ReportEvent($"task_start:{_currentTask.taskName}", Time.realtimeSinceStartup, 0f);
        Debug.Log($"[TaskManager] Task {_currentTask.taskName} Started");
    }

    private void EndTask()
    {
        ResXRDataManager_V2.Instance.ReportEvent($"task_end:{_currentTask.taskName}", Time.realtimeSinceStartup, 0f);
        Debug.Log($"[TaskManager] Task {_currentTask.taskName} Ended");
    }

    private async UniTask BetweenTrialsFlow()
    {
        await BinaryChoice_SceneReferencer.Instance.fixationCross.ShowForSeconds(_timeBetweenStimuli);
    }

    private void CreateTrials()
    {
        // create trials for the task, using the stimuli pairs dispatcher

        if (_stimuliPairsLoader == null)
        {
            Debug.LogError("[TaskManager] CreateTrials: stimuliPairsDispatcher is null. Cannot create trials. Make sure it is initialized in StartTask.");
            return;
        }

        List<BinaryChoice_Trial> trialsList = new List<BinaryChoice_Trial>();

        while (_stimuliPairsLoader.HasMorePairs())
        {
            StimuliPair pair = _stimuliPairsLoader.GetNextPair();
            BinaryChoice_Trial trial = new BinaryChoice_Trial(pair);
            trialsList.Add(trial);
        }
        _trials = trialsList.ToArray();

        Debug.Log($"[TaskManager] Created {_trials.Length} trials for task {_currentTask.taskName}");
    }

}
