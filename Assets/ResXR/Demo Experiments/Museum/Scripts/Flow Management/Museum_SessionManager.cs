using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Museum_SessionManager : ResXRSingleton<Museum_SessionManager>
{
    [FormerlySerializedAs("_rounds")]
    [SerializeField] private Museum_Task[] _tasks;
    private int _currentTask;

    private void Start()
    {
        RunSessionFlow().Forget();
    }

    public async UniTask RunSessionFlow()
    {
        StartSession();

        await Museum_SceneReferencer.Instance.welcomeInstructions.ShowAndWaitForConfirmation(false);

        while (_currentTask < _tasks.Length)
        {
            await Museum_TaskManager.Instance.RunTaskFlow(_tasks[_currentTask]);
            await BetweenTasksFlow();
            _currentTask++;
        }
        PlaceEndInstructionsInFrontOfPlayer();
        await Museum_SceneReferencer.Instance.endInstructions.ShowAndWaitForConfirmation(false);

        EndSession();
    }


    private void StartSession()
    {   

    }


    private void EndSession()
    {  
        //exit
        Application.Quit();
    }

    private async UniTask BetweenTasksFlow()
    {
        await UniTask.Yield();

    }


    private void PlaceEndInstructionsInFrontOfPlayer()
    {
        PlaceInFrontOfPlayerHead placementHelper = Museum_SceneReferencer.Instance.endInstructions.GetComponent<PlaceInFrontOfPlayerHead>();
        if (placementHelper == null)
        {
            Debug.LogWarning("No PlaceInFrontOfPlayerHead component found on end instructions panel. Cannot place it in front of player.");
        }
        else
        {
            placementHelper.RepositionNow();
        }
    }
}
