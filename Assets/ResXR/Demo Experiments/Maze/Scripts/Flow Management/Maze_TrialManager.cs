using Cysharp.Threading.Tasks;
using UnityEngine;

public class Maze_TrialManager : ResXRSingleton<Maze_TrialManager>
{
    private Maze_Trial _currentTrial;
    private Coin _coin;

    public async UniTask RunTrialFlow(Maze_Trial trial)
    {
        _currentTrial = trial;
        StartTrial();


        await _coin.WaitForCoinPickup();


        // all trial flow. Activating and waiting for project specific functionalities.
        await UniTask.Yield();

        EndTrial();
    }

    private void StartTrial()
    {
        // setup trial initial conditions.
        InitReferences();
    }


    private void EndTrial()
    {
        // setup trial end conditions.
    }



    private void InitReferences()
    {
        _coin = Maze_SceneReferencer.Instance.coin;
    }
}
