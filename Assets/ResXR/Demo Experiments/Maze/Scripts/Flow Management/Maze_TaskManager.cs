using Cysharp.Threading.Tasks;
using ResXRData;
using UnityEngine;

public class Maze_TaskManager : ResXRSingleton<Maze_TaskManager>
{
    [SerializeField, ReadOnly] private Maze_Trial[] _trials;
    private int _currentTrial = 0;
    private Maze_Task _currentTask;
    private string _taskName;
    private Maze _maze;
    private float fadeToBlackDuration = 1.0f;
    private InstructionsPanel _trialStartPanel;
    private float _trialNumberPanelVisibleDuration;

    public async UniTask RunTaskFlow(Maze_Task task, int taskIndex)
    {
        _currentTask = task;
        StartTask(taskIndex);

        while (_currentTrial < _trials.Length)
        {
            await SetTrialNumberAndShowTrialStartInstructions();

            await Maze_TrialManager.Instance.RunTrialFlow(_trials[_currentTrial], _taskName, _currentTrial);
            if (_currentTrial < _trials.Length - 1) // don't rotate maze after the final trial
            {
                await BetweenTrialsFlow();
            }
            _currentTrial++;
        }

        EndTask();
    }

    private void StartTask(int taskIndex)
    {
        // setup task initial conditions.
        InitReferences();

        _taskName = $"maze_t{taskIndex}";
        _trials = new Maze_Trial[_currentTask.numOfTrials];
        _currentTrial = 0;

        ResXRDataManager_V2.Instance.ReportEvent($"task_start:{_taskName}", Time.realtimeSinceStartup, 0f);
    }


    private void EndTask()
    {
        // setup end task conditions
        ResXRDataManager_V2.Instance.ReportEvent($"task_end:{_taskName}", Time.realtimeSinceStartup, 0f);
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
        ResXRDataManager_V2.Instance.ReportEvent(
            $"maze_rotation:state={(_maze.IsRotated ? "rotated" : "original")}",
            Time.realtimeSinceStartup, 0f);
        await ResXRPlayer.Instance.FadeViewToColor(Color.clear, fadeToBlackDuration);
    }

    private void InitReferences()
    {
        _maze = Maze_SceneReferencer.Instance.maze;
        _trialStartPanel = Maze_SceneReferencer.Instance.trialStartPanel;
        _trialNumberPanelVisibleDuration = Maze_SceneReferencer.Instance.trialNumberPanelVisibleDuration;
    }

}
