using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class PlayerPositionMark : MonoBehaviour
{
    [Header("references")]
    public GameObject visualMark;
    public InstructionsPanel instructionsPanel;

    [Header("settings")]
    public float fadeInDuration = 1f;
    public bool hideOnAwake = false;

    private PositionMarkTriggerZone triggerZone;
    private bool playerInside = false;
    
    

    private void Awake()
    {
        triggerZone = GetComponentInChildren<PositionMarkTriggerZone>();
        if (triggerZone == null)
        {
            Debug.LogError("PositionMarkTriggerZone not found in children.");
        }

        if (hideOnAwake)
        {
            Hide();
        }
    }

    private void OnEnable()
    {
        if (triggerZone != null)
        {
            triggerZone.OnPlayerEnter += HandlePlayerEnter;
        }
    }

    private void OnDisable()
    {
        if (triggerZone != null)
        {
            triggerZone.OnPlayerEnter -= HandlePlayerEnter;
        }
    }

    private void HandlePlayerEnter(GameObject player)
    {
        Debug.Log("[PlayerPositionMark] Player entered position marker");
        playerInside = true;



    }

    private void Show()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
        instructionsPanel.Show().Forget();
    }

    private void Hide()
    {
        instructionsPanel.Hide().Forget();
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }


    public async UniTask WaitForPlayerAsync(bool fadeToblack = true, CancellationToken cancellationToken = default)
    {
        Show();

        if (fadeToblack)
        {
            await ResXRPlayer.Instance.FadeViewToColor(Color.black, 0.0f);
        }

        Debug.Log("[PlayerPositionMark] Waiting for player to enter position marker...");

        await UniTask.WaitUntil(() => playerInside, cancellationToken: cancellationToken);

        if (fadeToblack)
        {
            await ResXRPlayer.Instance.FadeViewToColor(Color.clear, fadeInDuration);
        }
        
        Hide();

    }
}