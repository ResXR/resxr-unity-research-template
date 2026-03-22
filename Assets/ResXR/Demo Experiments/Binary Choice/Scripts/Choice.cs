using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// this class represents a choice object that can detect when it is touched by another object with the "Toucher" tag.
public class Choice : MonoBehaviour
{
    private Collider coll;
    private SpriteRenderer sprite;
    private bool isWaitingForTrigger = false;
    private UniTaskCompletionSource<Collider> trigger_tcs;


    private void Awake()
    {
        // Get the Collider and SpriteRenderer components attached to this choice GameObject
        coll = GetComponent<Collider>();
        if (coll == null)
        {
            Debug.LogError($"[Choice] No Collider found on this choice GameObject({gameObject.name}). Please add a Collider component.");
        }
        sprite = GetComponent<SpriteRenderer>();
        if (sprite == null)
        {
            Debug.LogError($"[Choice] No SpriteRenderer found on this choice GameObject({gameObject.name}). Please add a SpriteRenderer component.");
        }
    }

    public string GetCurrentImageName()
    {
        return sprite.sprite.name;
    }

    /// <summary>
    /// Waits for the choice to be touched by an object with the "Toucher" tag.
    /// Returns the collider that triggered the choice (for HandUsed resolution).
    /// </summary>
    public async UniTask<Collider> WaitForTouch()
    {
        if (!coll.isTrigger)
        {
            Debug.LogError($"[Choice] WaitForTouch is called but the collider on {gameObject.name} is not set to trigger");
        }

        isWaitingForTrigger = true;
        trigger_tcs = new UniTaskCompletionSource<Collider>();

        return await trigger_tcs.Task;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isWaitingForTrigger && other.CompareTag("Toucher"))
        {
            isWaitingForTrigger = false;
            trigger_tcs?.TrySetResult(other);
        }
    }

    public void SetImage(Sprite newImage)
    {
        sprite.sprite = newImage;
    }
}
