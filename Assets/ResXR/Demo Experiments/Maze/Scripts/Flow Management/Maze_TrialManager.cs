using Cysharp.Threading.Tasks;
using ResXRData;
using UnityEngine;

public class Maze_TrialManager : ResXRSingleton<Maze_TrialManager>
{
    private Maze_Trial _currentTrial;
    private string _currentTaskName;
    private int _currentTrialIndex;
    private float _trialStartTime;
    private Coin _coin;

    public async UniTask RunTrialFlow(Maze_Trial trial, string taskName, int trialIndex)
    {
        _currentTrial = trial;
        _currentTaskName = taskName;
        _currentTrialIndex = trialIndex;

        StartTrial();

        await _coin.WaitForCoinPickup();

        float coinPickupTime = Time.realtimeSinceStartup;
        ResXRDataManager_V2.Instance.ReportEvent(
            $"coin_pickup:{_currentTaskName}_t{_currentTrialIndex}",
            coinPickupTime, 0f);

        await UniTask.Yield();

        EndTrial(coinPickupTime);
    }

    private void StartTrial()
    {
        // setup trial initial conditions.
        InitReferences();

        _trialStartTime = Time.realtimeSinceStartup;
        ResXRDataManager_V2.Instance.ReportEvent(
            $"trial_start:{_currentTaskName}_t{_currentTrialIndex}",
            _trialStartTime, 0f);
    }


    private void EndTrial(float endTime)
    {
        string trialName = $"{_currentTaskName}_t{_currentTrialIndex}";

        ResXRDataManager_V2.Instance.ReportEvent(
            $"trial_end:{trialName}",
            endTime, 0f);

        // Generic trial structure row (shared schema across all demos)
        ResXRDataManager_V2.Instance.LogCustom(new TrialsData(
            ResXRDataManager_V2.Instance.SessionTime,
            _currentTaskName,
            _currentTrialIndex.ToString(),
            trialName,
            _trialStartTime,
            endTime
        ));

        // Maze-specific row: rotation state + coin/start positions recorded fresh each trial
        bool rotatedAtStart = Maze_SceneReferencer.Instance.maze.IsRotated;
        Vector3 coinPos = Maze_SceneReferencer.Instance.coin.transform.position;
        Vector3 startZonePos = Maze_SceneReferencer.Instance.startingPositionMark.transform.position;

        ResXRDataManager_V2.Instance.LogCustom(new MazeTrialData(
            ResXRDataManager_V2.Instance.SessionTime,
            _currentTaskName,
            _currentTrialIndex,
            trialName,
            _trialStartTime,
            endTime,
            rotatedAtStart,
            coinPos,
            startZonePos
        ));
    }


    private void InitReferences()
    {
        _coin = Maze_SceneReferencer.Instance.coin;
    }
}
