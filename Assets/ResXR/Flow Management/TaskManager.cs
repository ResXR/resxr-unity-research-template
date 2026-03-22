using Cysharp.Threading.Tasks;
using UnityEngine;

public class TaskManager : ResXRSingleton<TaskManager>
{

    [SerializeField] private Trial[] _trials;
    private int _currentTrial = 0;
    private Task _currentTask;

    public async UniTask RunTaskFlow(Task task)
    {
        _currentTask = task;
        StartTask();

        while (_currentTrial < _trials.Length)
        {
            await TrialManager.Instance.RunTrialFlow(_trials[_currentTrial]);
            await BetweenTrialsFlow();
            _currentTrial++;
        }

        EndTask();
    }

    private void StartTask()
    {
        // setup task initial conditions.
    }


    private void EndTask()
    {
        // setup end task conditions
    }

    private async UniTask BetweenTrialsFlow()
    {
        await UniTask.Yield();

    }
}
