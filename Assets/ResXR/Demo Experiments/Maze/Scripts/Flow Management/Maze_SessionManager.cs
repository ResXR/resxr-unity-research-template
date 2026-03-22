using Cysharp.Threading.Tasks;
using Meta.WitAi;
using System;
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
        StartSession();
        await _startingPositionMark.WaitForPlayerAsync();
        await _generalInstructions.ShowAndWaitForConfirmation();

        while (_currentTask < _tasks.Length)
        {
            await Maze_TaskManager.Instance.RunTaskFlow(_tasks[_currentTask]);
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
    }


    private void EndSession()
    {
        // setup end session conditions
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
