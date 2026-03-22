using Cysharp.Threading.Tasks;

public class TrialManager : ResXRSingleton<TrialManager>
{
    private Trial _currentTrial;

    public async UniTask RunTrialFlow(Trial trial)
    {
        _currentTrial = trial;
        StartTrial();

        // all trial flow. Activating and waiting for project specific functionalities.
        await UniTask.Yield();

        EndTrial();
    }

    private void StartTrial()
    {
        // setup trial initial conditions.
    }


    private void EndTrial()
    {
        // setup trial end conditions.
    }
}