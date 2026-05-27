using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using ResXRData;
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
        ResXRDataManager_V2.Instance.ReportEvent("session_start", Time.realtimeSinceStartup, 0f);

        // Slider config: written once per session so per-image rating rows stay compact
        Museum_SceneReferencer.Instance.imagesRating.LogSliderConfig();

        // Artwork bounds: written once per session (world-space bounds + orientation per artwork)
        LogArtworkBounds();
    }


    private void EndSession()
    {
        ResXRDataManager_V2.Instance.ReportEvent("session_end", Time.realtimeSinceStartup, 0f);
        Application.Quit();
    }

    private async UniTask BetweenTasksFlow()
    {
        await UniTask.Yield();
    }

    /// <summary>
    /// Writes one ArtworkBoundsRow per artwork assigned in Museum_SceneReferencer.
    /// artworks and artworkColliders must be the same length and in the same order.
    /// Call once at session start. Wire up both arrays in the Inspector before building.
    /// </summary>
    private void LogArtworkBounds()
    {
        Renderer[] artworks = Museum_SceneReferencer.Instance.artworks;
        Collider[] colliders = Museum_SceneReferencer.Instance.artworkColliders;

        if (artworks == null || artworks.Length == 0)
        {
            Debug.LogWarning("[Museum_SessionManager] No artworks assigned in Museum_SceneReferencer. ArtworkBounds.csv will be empty.");
            return;
        }

        if (colliders == null || colliders.Length != artworks.Length)
        {
            Debug.LogError($"[Museum_SessionManager] artworkColliders length ({colliders?.Length ?? 0}) does not match artworks length ({artworks.Length}). ArtworkBounds.csv will be empty.");
            return;
        }

        float t = Time.realtimeSinceStartup;
        int logged = 0;
        for (int i = 0; i < artworks.Length; i++)
        {
            if (artworks[i] == null) { Debug.LogError($"[Museum_SessionManager] artworks[{i}] is null — skipping."); continue; }
            if (colliders[i] == null) { Debug.LogError($"[Museum_SessionManager] artworkColliders[{i}] is null — skipping."); continue; }
            ResXRDataManager_V2.Instance.LogCustom(new ArtworkBoundsRow(t, artworks[i], colliders[i]));
            logged++;
        }

        Debug.Log($"[Museum_SessionManager] Logged bounds for {logged} artworks.");
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
