using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Serialization;

public class SessionManager : ResXRSingleton<SessionManager>
{

    [FormerlySerializedAs("_rounds")]
    [SerializeField] private Task[] _tasks;
    private int _currentTask;

    //If there is a higher level flow manager, remove this and use his start method
    private void Start()
    {
        RunSessionFlow().Forget();
    }

    public async UniTask RunSessionFlow()
    {
        StartSession();

        while (_currentTask < _tasks.Length)
        {
            await TaskManager.Instance.RunTaskFlow(_tasks[_currentTask]);
            await BetweenTasksFlow();
            _currentTask++;
        }

        EndSession();
    }


    private void StartSession()
    {
        // setup session initial conditions.
    }


    private void EndSession()
    {
        // setup end session conditions
    }

    private async UniTask BetweenTasksFlow()
    {
        await UniTask.Yield();

        throw new NotImplementedException();
    }
}