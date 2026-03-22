using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

//TODO: create interface (to see the methods)
public abstract class AInputManager
{
    public bool IsInputPressedThisFrame(HandType handType)
    {
        bool isLeftHeld = IsLeftHeld();
        bool isRightHeld = IsRightHeld();

        switch (handType)
        {
            case HandType.Left:
                return isLeftHeld;
            case HandType.Right:
                return isRightHeld;
            case HandType.Any:
                return isLeftHeld || isRightHeld;
            case HandType.None:
                return false;
            default: return false;
        }
    }

    public abstract bool IsLeftHeld();
    public abstract bool IsRightHeld();

    public async UniTask WaitForPress(HandType handType, Action callback = default, CancellationToken cancellationToken = default)
    {
        await WaitForReleaseIfHolding(handType,
            cancellationToken); //If the player is already holding when the method starts, we wait for release and a new hold.
        await UniTask.WaitUntil(() => IsInputPressedThisFrame(handType), cancellationToken: cancellationToken);
        callback?.Invoke();
        DoOnPress();
    }

    protected virtual void DoOnPress()
    {
    }

    private async UniTask WaitForReleaseIfHolding(HandType handType, CancellationToken cancellationToken = default)
    {
        if (IsInputPressedThisFrame(handType))
        {
            await UniTask.WaitUntil(() => !IsInputPressedThisFrame(handType), cancellationToken: cancellationToken);
        }
    }

    public async UniTask WaitForHold(HandType handType, float duration, Action callback = default,
        CancellationToken cancellationToken = default)
    {
        //If the player is already holding when the method starts, we wait for release and a new hold
        await WaitForReleaseIfHolding(handType, cancellationToken);

        float holdingTime = 0;
        while (holdingTime < duration)
        {
            holdingTime = IsInputPressedThisFrame(handType) ? holdingTime + Time.deltaTime : 0;

            DoWhileWaitingForHold(handType, holdingTime, duration);

            await UniTask.Yield(cancellationToken: cancellationToken);
        }

        callback?.Invoke();

        DoOnHoldFinished(handType);
    }

    protected virtual void DoWhileWaitingForHold(HandType handType, float holdingDuration, float duration)
    {
    }

    protected virtual void DoOnHoldFinished(HandType handType)
    {
    }


    public async UniTask WaitForHoldAndRelease(HandType handType, float duration, Action callback = default,
        CancellationToken cancellationToken = default)
    {
        await WaitForHold(handType, duration, null, cancellationToken);
        await WaitForReleaseIfHolding(handType, cancellationToken);
        callback?.Invoke();
    }

    public async UniTask WaitForPressesInARow(int numberOfInputsInARow, float maxTimeBetweenInputs,
        Action successCallback = default, Action timeOutCallback = default,
        bool alternateHands = false, HandType startingHand = HandType.Right, CancellationToken cancellationToken = default)
    {
        int currentNumberOfInputs = 1;
        float currentTime = 0;
        HandType nextHandType = startingHand;
        while (currentNumberOfInputs < numberOfInputsInARow)
        {
            bool inputReceived = alternateHands
                ? IsInputPressedThisFrame(nextHandType)
                : IsInputPressedThisFrame(HandType.Any);
            if (inputReceived)
            {
                currentNumberOfInputs++;
                currentTime = 0;
                nextHandType = nextHandType == HandType.Left ? HandType.Right : HandType.Left;
            }

            await UniTask.Yield(cancellationToken: cancellationToken);
            currentTime += Time.deltaTime;

            if (currentTime > maxTimeBetweenInputs)
            {
                timeOutCallback?.Invoke();
                return;
            }
        }

        successCallback?.Invoke();
    }
}