using Cysharp.Threading.Tasks;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using UnityEngine;

public class Maze_TaskManager : ResXRSingleton<Maze_TaskManager>
{
    [SerializeField, ReadOnly] private Maze_Trial[] _trials;
    private int _currentTrial = 0;
    private Maze_Task _currentTask;
    private Maze _maze;
    private float fadeToBlackDuration = 1.0f;
    private InstructionsPanel _trialStartPanel;
    private float _trialNumberPanelVisibleDuration;

    public async UniTask RunTaskFlow(Maze_Task task)
    {
        _currentTask = task;
        StartTask();

        while (_currentTrial < _trials.Length)
        {
            await SetTrialNumberAndShowTrialStartInstructions(); 

            await Maze_TrialManager.Instance.RunTrialFlow(_trials[_currentTrial]);
            await BetweenTrialsFlow();
            _currentTrial++;
        }

        EndTask();
    }

    private void StartTask()
    {
        // setup task initial conditions.
        InitReferences();

        _trials = new Maze_Trial[_currentTask.numOfTrials];
    }


    private void EndTask()
    {
        // setup end task conditions
    }

    private async UniTask BetweenTrialsFlow()
    {
        await RotateMaze();
    }

    private async UniTask SetTrialNumberAndShowTrialStartInstructions()
    {
        _trialStartPanel.SetTitle("Trial #" + (_currentTrial + 1).ToString());
        await _trialStartPanel.ShowForSeconds(_trialNumberPanelVisibleDuration, true);
    }


    private async UniTask RotateMaze()
    {
        await ResXRPlayer.Instance.FadeViewToColor(Color.black, fadeToBlackDuration);
        _maze.Rotate180Degrees();
        await ResXRPlayer.Instance.FadeViewToColor(Color.clear, fadeToBlackDuration);
    }

    private void InitReferences()
    {
        _maze = Maze_SceneReferencer.Instance.maze;
        _trialStartPanel = Maze_SceneReferencer.Instance.trialStartPanel;
        _trialNumberPanelVisibleDuration = Maze_SceneReferencer.Instance.trialNumberPanelVisibleDuration;
    }

}
