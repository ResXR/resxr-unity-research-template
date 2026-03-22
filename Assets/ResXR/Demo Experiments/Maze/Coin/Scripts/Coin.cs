using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class Coin : MonoBehaviour
{

    public Animator Animator => _animator;
    private Animator _animator;

    public bool facingForward => _facingForward;
    [SerializeField] private bool _facingForward;


    private bool _acceptPickUps = true;
    private TaskCompletionSource<bool> _coinPickupTcs;

    private AudioSource _coinPickUpAudio;



    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _coinPickUpAudio = GetComponent<AudioSource>();
    }

    private void Start()
    {
        _animator.SetTrigger("Hide");
    }

    public void PickUp()
    {
        //Preventing multiple fingers from colliding with coin in the same frame
        if (!_acceptPickUps)
        {
            return;
        }
        Debug.Log($"[COIN] Pickup triggered at Time.time = {Time.time:F3}. right ResXRHand pos = {ResXRPlayer.Instance.RightHand.position}");

        //coin picked up indications
        _acceptPickUps = false;
        _animator.SetTrigger("Coin pressed");
        _coinPickUpAudio.Play();

        UpdateAcceptPickUpsState().Forget();

        // Complete the task when the coin is picked up
        if (_coinPickupTcs != null && !_coinPickupTcs.Task.IsCompleted)
        {
            _coinPickupTcs.SetResult(true);
        }
    }



    private async UniTask UpdateAcceptPickUpsState()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        _acceptPickUps = true;
    }



    public System.Threading.Tasks.Task<bool> WaitForCoinPickup()
    {
        _animator.SetTrigger("Activated");
        _coinPickupTcs = new TaskCompletionSource<bool>();
        return _coinPickupTcs.Task;
    }

}
