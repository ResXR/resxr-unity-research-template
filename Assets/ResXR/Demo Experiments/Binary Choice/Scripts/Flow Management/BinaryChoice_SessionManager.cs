using Cysharp.Threading.Tasks;
using ResXRData;
using UnityEngine;
using UnityEngine.Serialization;


public class BinaryChoice_SessionManager : ResXRSingleton<BinaryChoice_SessionManager>
{
    [FormerlySerializedAs("_rounds")]
    private BinaryChoice_Task[] _tasks;
    private int _currentTask;

    private InstructionsPanelWithConfirmation generalInstructions;
    private InstructionsPanel endInstructions;
    private float instructionsDisplayTime;



    //If there is a higher level flow manager, remove this and use his start method
    private void Start()
    {
        RunSessionFlow().Forget();
    }

    public async UniTask RunSessionFlow()
    {
        StartSession();

        await generalInstructions.ShowAndWaitForConfirmation();

        while (_currentTask < _tasks.Length)
        {

            await BinaryChoice_TaskManager.Instance.RunTaskFlow(_tasks[_currentTask]);
            await BetweenTasksFlow();
            _currentTask++;
        }

        await endInstructions.ShowForSeconds(instructionsDisplayTime);
        EndSession();
    }


    private void StartSession()
    {
        // setup session initial conditions.
        LoadTasksFromConfig();
        InitializeReferences();

        BinaryChoice_SceneReferencer.Instance.choicesManager.LogStimulusBoundsOnce();

        ResXRDataManager_V2.Instance.ReportEvent("session_start", Time.realtimeSinceStartup, 0f);

        Debug.Log($"Total Tasks: {_tasks.Length}\n" +
                  $"Experiment settings:\n" +
                  $"Time between stimuli: {BinaryChoice_SceneReferencer.Instance.SecondsBetweenStimuli}");

        Debug.Log("Session Started");
    }

    private void InitializeReferences()
    {
        generalInstructions = BinaryChoice_SceneReferencer.Instance.generalInstructions;
        instructionsDisplayTime = BinaryChoice_SceneReferencer.Instance.instructionsDisplayTime;
        endInstructions = BinaryChoice_SceneReferencer.Instance.endInstructions;
    }


    private void EndSession()
    {
        // setup end session conditions
        ResXRDataManager_V2.Instance.ReportEvent("session_end", Time.realtimeSinceStartup, 0f);
        Debug.Log("Session Ended");
    }

    private async UniTask BetweenTasksFlow()
    {
        await UniTask.Yield();
    }

    private void LoadTasksFromConfig()
    {
        _tasks = BinaryChoice_TasksConfiguration.Instance.tasks;
    }

}
