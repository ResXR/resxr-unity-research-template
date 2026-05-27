using Cysharp.Threading.Tasks;
using ResXRData;
using UnityEngine;
using UnityEngine.Serialization;

public class Maze_SessionManager : ResXRSingleton<Maze_SessionManager>
{
    [FormerlySerializedAs("_rounds")]
    [SerializeField] private Maze_Task[] _tasks;
    private int _currentTask;

    private PlayerPositionMark _startingPositionMark;
    private InstructionsPanelWithConfirmation _generalInstructions;
    private InstructionsPanel _endInstructions;

    private float endInstructionsDuration = 5f;


    //If there is a higher level flow manager, remove this and use his start method
    private void Start()
    {
        RunSessionFlow().Forget();
    }

    public async UniTask RunSessionFlow()
    {
        await UniTask.Yield(); // let the scene fully settle before starting flow

        StartSession();

        await _startingPositionMark.WaitForPlayerAsync();
        ResXRDataManager.Instance.ReportEvent("player_at_start_zone", Time.realtimeSinceStartup, 0f);

        await _generalInstructions.ShowAndWaitForConfirmation();

        while (_currentTask < _tasks.Length)
        {
            await Maze_TaskManager.Instance.RunTaskFlow(_tasks[_currentTask], _currentTask);
            await BetweenTasksFlow();
            _currentTask++;
        }

        await _endInstructions.ShowForSeconds(endInstructionsDuration);

        EndSession();
    }


    private void StartSession()
    {
        // setup session initial conditions.
        InitReferences();
        ResXRDataManager.Instance.ReportEvent("session_start", Time.realtimeSinceStartup, 0f);
    }


    private void EndSession()
    {
        // setup end session conditions
        ResXRDataManager.Instance.ReportEvent("session_end", Time.realtimeSinceStartup, 0f);
    }

    private async UniTask BetweenTasksFlow()
    {
        await UniTask.Yield();
    }

    private void InitReferences()
    {
        _startingPositionMark = Maze_SceneReferencer.Instance.startingPositionMark;
        _generalInstructions = Maze_SceneReferencer.Instance.generalInstructions;
        _endInstructions = Maze_SceneReferencer.Instance.endInstructions;
    }

}
